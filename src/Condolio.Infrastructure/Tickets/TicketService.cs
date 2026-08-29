using Condolio.Application.Common;
using Condolio.Application.Tickets;
using Condolio.Domain.Tickets;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Tickets;

public class TicketService : ITicketService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;

    public TicketService(CondolioDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<TicketListaDto>> ListarAsync(Guid consorcioId, bool archivados, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<TicketListaDto>.Fail("Consorcio no encontrado.");

        var todos = await _db.Tickets
            .Where(t => t.ConsorcioId == consorcioId)
            .Select(t => new
            {
                t,
                unidad = t.UnidadId == null ? null : _db.Unidades.Where(u => u.Id == t.UnidadId).Select(u => u.Nombre).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var activos = todos.Count(x => x.t.ArchivadoUtc == null);
        var archivadosCount = todos.Count - activos;

        var filtrados = todos
            .Where(x => archivados ? x.t.ArchivadoUtc != null : x.t.ArchivadoUtc == null)
            .OrderByDescending(x => x.t.UltimaActividadUtc)
            .Select(x => Mapear(x.t, x.unidad))
            .ToList();

        return Result<TicketListaDto>.Ok(new TicketListaDto(filtrados, activos, archivadosCount));
    }

    public async Task<Result<IReadOnlyList<UsuarioAsignableDto>>> AsignablesAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var tenantId = _tenant.AdministradorId;

        var query =
            from u in _db.Users
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == "Administrador" && (tenantId == null || u.AdministradorId == tenantId)
            orderby u.Nombre
            select new UsuarioAsignableDto(u.Id, (u.Nombre + " " + u.Apellido).Trim());

        var lista = await query.ToListAsync(ct);
        return Result<IReadOnlyList<UsuarioAsignableDto>>.Ok(lista);
    }

    public async Task<Result<TicketDetalleDto>> ObtenerAsync(Guid consorcioId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Comentarios)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.ConsorcioId == consorcioId, ct);
        if (ticket is null) return Result<TicketDetalleDto>.Fail("Ticket no encontrado.");

        var unidad = ticket.UnidadId is null ? null
            : await _db.Unidades.Where(u => u.Id == ticket.UnidadId).Select(u => u.Nombre).FirstOrDefaultAsync(ct);

        var nombres = await NombresPorUsuario(ticket.Comentarios.Select(c => c.AutorUsuarioId), ct);
        var comentarios = ticket.Comentarios
            .OrderBy(c => c.CreadoUtc)
            .Select(c => new TicketComentarioDto(
                c.Texto,
                nombres.TryGetValue(c.AutorUsuarioId, out var n) ? n : "—",
                c.CreadoUtc,
                c.EsInterna))
            .ToList();

        return Result<TicketDetalleDto>.Ok(new TicketDetalleDto(Mapear(ticket, unidad), comentarios));
    }

    public async Task<Result<TicketDto>> CrearAsync(Guid consorcioId, CrearTicketDto dto, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<TicketDto>.Fail("Consorcio no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return Result<TicketDto>.Fail("La descripción es obligatoria.");

        string? unidadNombre = null;
        if (dto.UnidadId is { } uid)
        {
            unidadNombre = await _db.Unidades.Where(u => u.Id == uid && u.ConsorcioId == consorcioId)
                .Select(u => u.Nombre).FirstOrDefaultAsync(ct);
            if (unidadNombre is null) return Result<TicketDto>.Fail("Unidad no encontrada.");
        }

        var ahora = DateTime.UtcNow;
        var ticket = new Ticket
        {
            ConsorcioId = consorcioId,
            Numero = await ProximoNumero(consorcioId, ct),
            Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? null : dto.Titulo.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Categoria = dto.Categoria,
            Prioridad = dto.Prioridad,
            Estado = EstadoTicket.Nuevo,
            UnidadId = dto.UnidadId,
            Etiquetas = JoinEtiquetas(dto.Etiquetas),
            Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim(),
            FechaLimite = dto.FechaLimite,
            ReportadoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            ReportadoPorNombre = await NombreUsuario(_tenant.UsuarioId, ct) ?? "Administración",
            ReportadoUtc = ahora,
            EstadoDesdeUtc = ahora,
            UltimaActividadUtc = ahora,
        };

        if (!string.IsNullOrWhiteSpace(dto.AsignadoAUsuarioId))
        {
            ticket.AsignadoAUsuarioId = dto.AsignadoAUsuarioId;
            ticket.AsignadoANombre = await NombreUsuario(dto.AsignadoAUsuarioId, ct);
        }

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return Result<TicketDto>.Ok(Mapear(ticket, unidadNombre));
    }

    public async Task<Result<TicketDto>> ActualizarAsync(Guid consorcioId, Guid ticketId, ActualizarTicketDto dto, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.ConsorcioId == consorcioId, ct);
        if (ticket is null) return Result<TicketDto>.Fail("Ticket no encontrado.");

        var ahora = DateTime.UtcNow;
        if (ticket.Estado != dto.Estado)
        {
            ticket.Estado = dto.Estado;
            ticket.EstadoDesdeUtc = ahora;
        }
        ticket.Prioridad = dto.Prioridad;

        if (ticket.AsignadoAUsuarioId != dto.AsignadoAUsuarioId)
        {
            ticket.AsignadoAUsuarioId = string.IsNullOrWhiteSpace(dto.AsignadoAUsuarioId) ? null : dto.AsignadoAUsuarioId;
            ticket.AsignadoANombre = ticket.AsignadoAUsuarioId is null ? null : await NombreUsuario(ticket.AsignadoAUsuarioId, ct);
        }

        ticket.UltimaActividadUtc = ahora;
        await _db.SaveChangesAsync(ct);

        var unidad = ticket.UnidadId is null ? null
            : await _db.Unidades.Where(u => u.Id == ticket.UnidadId).Select(u => u.Nombre).FirstOrDefaultAsync(ct);
        return Result<TicketDto>.Ok(Mapear(ticket, unidad));
    }

    public async Task<Result> ComentarAsync(Guid consorcioId, Guid ticketId, string texto, bool esInterna, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto)) return Result.Fail("El comentario no puede estar vacío.");
        var ticket = await _db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.ConsorcioId == consorcioId, ct);
        if (ticket is null) return Result.Fail("Ticket no encontrado.");

        _db.TicketComentarios.Add(new TicketComentario
        {
            TicketId = ticketId,
            AdministradorId = ticket.AdministradorId,
            Texto = texto.Trim(),
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            EsInterna = esInterna,
        });
        await _db.SaveChangesAsync(ct);

        await _db.Tickets.Where(t => t.Id == ticketId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UltimaActividadUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task<Result> ArchivarAsync(Guid consorcioId, Guid ticketId, bool archivar, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.ConsorcioId == consorcioId, ct);
        if (ticket is null) return Result.Fail("Ticket no encontrado.");

        ticket.ArchivadoUtc = archivar ? DateTime.UtcNow : null;
        ticket.UltimaActividadUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.Include(t => t.Comentarios)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.ConsorcioId == consorcioId, ct);
        if (ticket is null) return Result.Fail("Ticket no encontrado.");

        _db.TicketComentarios.RemoveRange(ticket.Comentarios);
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<int>> CrearDesdeIncidenciaAsync(
        Guid consorcioId, Guid unidadId, Guid incidenciaId,
        string? titulo, string descripcion, CategoriaIncidencia categoria, SeveridadIncidencia severidad,
        CancellationToken ct = default)
    {
        var ahora = DateTime.UtcNow;
        var ticket = new Ticket
        {
            ConsorcioId = consorcioId,
            Numero = await ProximoNumero(consorcioId, ct),
            Titulo = titulo,
            Descripcion = descripcion,
            Categoria = MapCategoria(categoria),
            Prioridad = MapPrioridad(severidad),
            Estado = EstadoTicket.Nuevo,
            UnidadId = unidadId,
            IncidenciaId = incidenciaId,
            ReportadoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            ReportadoPorNombre = await NombreUsuario(_tenant.UsuarioId, ct) ?? "Administración",
            ReportadoUtc = ahora,
            EstadoDesdeUtc = ahora,
            UltimaActividadUtc = ahora,
        };
        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);
        return Result<int>.Ok(ticket.Numero);
    }

    // ---- helpers ----

    private async Task<int> ProximoNumero(Guid consorcioId, CancellationToken ct) =>
        (await _db.Tickets.Where(t => t.ConsorcioId == consorcioId)
            .Select(t => (int?)t.Numero).MaxAsync(ct) ?? 0) + 1;

    private async Task<Dictionary<string, string>> NombresPorUsuario(IEnumerable<string?> ids, CancellationToken ct)
    {
        var distintos = ids.Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        return await _db.Users.Where(u => distintos.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.Nombre + " " + u.Apellido).Trim(), ct);
    }

    private async Task<string?> NombreUsuario(string? id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return await _db.Users.Where(u => u.Id == id)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync(ct);
    }

    private static string? JoinEtiquetas(IReadOnlyList<string>? tags)
    {
        if (tags is null) return null;
        var limpio = tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct().ToList();
        return limpio.Count == 0 ? null : string.Join(",", limpio);
    }

    private static IReadOnlyList<string> SplitEtiquetas(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static CategoriaTicket MapCategoria(CategoriaIncidencia c) => c switch
    {
        CategoriaIncidencia.Seguridad => CategoriaTicket.Seguridad,
        CategoriaIncidencia.Mantenimiento => CategoriaTicket.Mantenimiento,
        CategoriaIncidencia.Mascotas => CategoriaTicket.Mascotas,
        CategoriaIncidencia.Ruido => CategoriaTicket.Ruido,
        CategoriaIncidencia.Vecinos or CategoriaIncidencia.Disputa => CategoriaTicket.Vecinos,
        CategoriaIncidencia.DanoPropiedad => CategoriaTicket.Mantenimiento,
        _ => CategoriaTicket.Otro,
    };

    private static PrioridadTicket MapPrioridad(SeveridadIncidencia s) => s switch
    {
        SeveridadIncidencia.Baja => PrioridadTicket.Baja,
        SeveridadIncidencia.Media => PrioridadTicket.Media,
        SeveridadIncidencia.Alta => PrioridadTicket.Alta,
        SeveridadIncidencia.Critica => PrioridadTicket.Critica,
        _ => PrioridadTicket.Media,
    };

    private static TicketDto Mapear(Ticket t, string? unidadNombre) => new(
        t.Id, t.Numero, t.Titulo, t.Descripcion, t.Categoria, t.Estado, t.Prioridad,
        t.UnidadId, unidadNombre, SplitEtiquetas(t.Etiquetas), t.Ubicacion, t.FechaLimite,
        string.IsNullOrWhiteSpace(t.ReportadoPorNombre) ? "—" : t.ReportadoPorNombre,
        t.ReportadoUtc, t.AsignadoANombre, t.EstadoDesdeUtc, t.UltimaActividadUtc, t.ArchivadoUtc != null);
}
