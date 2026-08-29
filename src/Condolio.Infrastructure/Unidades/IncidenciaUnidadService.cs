using Condolio.Application.Common;
using Condolio.Application.Tickets;
using Condolio.Application.Unidades;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Unidades;

public class IncidenciaUnidadService : IIncidenciaUnidadService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IActividadUnidadService _actividad;
    private readonly ITicketService _tickets;

    public IncidenciaUnidadService(CondolioDbContext db, ITenantContext tenant, IActividadUnidadService actividad, ITicketService tickets)
    {
        _db = db;
        _tenant = tenant;
        _actividad = actividad;
        _tickets = tickets;
    }

    public async Task<Result<IReadOnlyList<IncidenciaUnidadDto>>> ListarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default)
    {
        if (!await UnidadExiste(consorcioId, unidadId, ct))
            return Result<IReadOnlyList<IncidenciaUnidadDto>>.Fail("Unidad no encontrada.");

        var raw = await (
            from i in _db.UnidadIncidencias.Where(i => i.UnidadId == unidadId)
            join u in _db.Users on i.AutorUsuarioId equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            orderby i.FechaEvento descending
            select new { i, autor = u != null ? (u.Nombre + " " + u.Apellido).Trim() : "—" })
            .ToListAsync(ct);

        var items = raw.Select(r => Mapear(r.i, r.autor)).ToList();
        return Result<IReadOnlyList<IncidenciaUnidadDto>>.Ok(items);
    }

    public async Task<Result<IncidenciaDetalleDto>> ObtenerAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct = default)
    {
        var incidencia = await _db.UnidadIncidencias
            .Include(i => i.Unidad)
            .Include(i => i.Comentarios)
            .FirstOrDefaultAsync(i => i.Id == incidenciaId && i.UnidadId == unidadId
                && i.Unidad.ConsorcioId == consorcioId, ct);
        if (incidencia is null) return Result<IncidenciaDetalleDto>.Fail("Incidencia no encontrada.");

        var nombres = await NombresPorUsuario(
            incidencia.Comentarios.Select(c => c.AutorUsuarioId).Append(incidencia.AutorUsuarioId), ct);

        string Nombre(string? id) => id != null && nombres.TryGetValue(id, out var n) ? n : "—";

        var historial = new List<IncidenciaHistorialDto>
        {
            new("creada", "creó la incidencia", Nombre(incidencia.AutorUsuarioId), incidencia.CreadoUtc),
        };
        if (incidencia.EscaladaUtc is { } escUtc)
            historial.Add(new("escalada", "escaló la incidencia a ticket", Nombre(incidencia.AutorUsuarioId), escUtc));
        // El escalado también toca ActualizadoUtc; solo mostramos "editó" si fue una edición real.
        if (incidencia.ActualizadoUtc is { } editUtc
            && (incidencia.EscaladaUtc is null || Math.Abs((editUtc - incidencia.EscaladaUtc.Value).TotalSeconds) > 5))
            historial.Add(new("editada", "editó la incidencia", Nombre(incidencia.AutorUsuarioId), editUtc));
        historial.AddRange(incidencia.Comentarios.Select(c =>
            new IncidenciaHistorialDto("comentario", c.Texto, Nombre(c.AutorUsuarioId), c.CreadoUtc)));

        var ordenado = historial.OrderBy(h => h.FechaUtc).ToList();
        return Result<IncidenciaDetalleDto>.Ok(new IncidenciaDetalleDto(
            Mapear(incidencia, Nombre(incidencia.AutorUsuarioId)), ordenado));
    }

    public async Task<Result<IncidenciaUnidadDto>> RegistrarAsync(Guid consorcioId, Guid unidadId, GuardarIncidenciaDto dto, CancellationToken ct = default)
    {
        if (!await UnidadExiste(consorcioId, unidadId, ct))
            return Result<IncidenciaUnidadDto>.Fail("Unidad no encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return Result<IncidenciaUnidadDto>.Fail("La descripción es obligatoria.");

        var incidencia = new UnidadIncidencia
        {
            UnidadId = unidadId,
            Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? null : dto.Titulo.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Categoria = dto.Categoria,
            Severidad = dto.Severidad,
            FechaEvento = dto.FechaEvento ?? DateTime.UtcNow,
            Etiquetas = JoinEtiquetas(dto.Etiquetas),
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
        };
        _db.UnidadIncidencias.Add(incidencia);
        await _db.SaveChangesAsync(ct);

        await _actividad.RegistrarAsync(unidadId, TipoActividad.IncidenciaRegistrada,
            "Incidencia registrada", incidencia.Titulo ?? incidencia.Categoria.ToString(), ct);

        return await Devolver(incidencia, ct);
    }

    public async Task<Result<IncidenciaUnidadDto>> EditarAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, GuardarIncidenciaDto dto, CancellationToken ct = default)
    {
        var incidencia = await _db.UnidadIncidencias
            .Include(i => i.Unidad)
            .FirstOrDefaultAsync(i => i.Id == incidenciaId && i.UnidadId == unidadId
                && i.Unidad.ConsorcioId == consorcioId, ct);
        if (incidencia is null) return Result<IncidenciaUnidadDto>.Fail("Incidencia no encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Descripcion))
            return Result<IncidenciaUnidadDto>.Fail("La descripción es obligatoria.");

        incidencia.Titulo = string.IsNullOrWhiteSpace(dto.Titulo) ? null : dto.Titulo.Trim();
        incidencia.Descripcion = dto.Descripcion.Trim();
        incidencia.Categoria = dto.Categoria;
        incidencia.Severidad = dto.Severidad;
        if (dto.FechaEvento is { } f) incidencia.FechaEvento = f;
        incidencia.Etiquetas = JoinEtiquetas(dto.Etiquetas);
        await _db.SaveChangesAsync(ct);

        await _actividad.RegistrarAsync(unidadId, TipoActividad.IncidenciaEditada,
            "Incidencia editada", incidencia.Titulo ?? incidencia.Categoria.ToString(), ct);

        return await Devolver(incidencia, ct);
    }

    public async Task<Result> AgregarComentarioAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, string texto, CancellationToken ct = default)
    {
        var incidencia = await _db.UnidadIncidencias
            .Include(i => i.Unidad)
            .FirstOrDefaultAsync(i => i.Id == incidenciaId && i.UnidadId == unidadId
                && i.Unidad.ConsorcioId == consorcioId, ct);
        if (incidencia is null) return Result.Fail("Incidencia no encontrada.");
        if (string.IsNullOrWhiteSpace(texto)) return Result.Fail("La nota no puede estar vacía.");

        _db.IncidenciaComentarios.Add(new IncidenciaComentario
        {
            IncidenciaId = incidenciaId,
            Texto = texto.Trim(),
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
        });
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> EscalarAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct = default)
    {
        var incidencia = await _db.UnidadIncidencias
            .Include(i => i.Unidad)
            .FirstOrDefaultAsync(i => i.Id == incidenciaId && i.UnidadId == unidadId
                && i.Unidad.ConsorcioId == consorcioId, ct);
        if (incidencia is null) return Result.Fail("Incidencia no encontrada.");
        if (incidencia.EscaladaUtc is not null) return Result.Fail("La incidencia ya fue escalada a ticket.");

        incidencia.EscaladaUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var ticket = await _tickets.CrearDesdeIncidenciaAsync(
            consorcioId, unidadId, incidenciaId,
            incidencia.Titulo, incidencia.Descripcion, incidencia.Categoria, incidencia.Severidad, ct);

        var detalle = ticket.Exito ? $"Ticket #{ticket.Valor}" : "Incidencia escalada a ticket";
        await _actividad.RegistrarAsync(unidadId, TipoActividad.IncidenciaEditada,
            detalle, incidencia.Titulo ?? incidencia.Categoria.ToString(), ct);
        return Result.Ok();
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct = default)
    {
        var incidencia = await _db.UnidadIncidencias
            .Include(i => i.Unidad)
            .FirstOrDefaultAsync(i => i.Id == incidenciaId && i.UnidadId == unidadId
                && i.Unidad.ConsorcioId == consorcioId, ct);
        if (incidencia is null) return Result.Fail("Incidencia no encontrada.");

        _db.UnidadIncidencias.Remove(incidencia);
        await _db.SaveChangesAsync(ct);

        await _actividad.RegistrarAsync(unidadId, TipoActividad.IncidenciaEliminada,
            "Incidencia eliminada", incidencia.Titulo ?? incidencia.Categoria.ToString(), ct);

        return Result.Ok();
    }

    private async Task<Dictionary<string, string>> NombresPorUsuario(IEnumerable<string?> ids, CancellationToken ct)
    {
        var distintos = ids.Where(id => !string.IsNullOrEmpty(id)).Cast<string>().Distinct().ToList();
        return await _db.Users.Where(u => distintos.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => (u.Nombre + " " + u.Apellido).Trim(), ct);
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

    private static IncidenciaUnidadDto Mapear(UnidadIncidencia i, string autor) => new(
        i.Id, i.Titulo, i.Descripcion, i.Categoria, i.Severidad, i.FechaEvento,
        SplitEtiquetas(i.Etiquetas), autor, i.CreadoUtc, i.ActualizadoUtc, i.EscaladaUtc);

    private Task<bool> UnidadExiste(Guid consorcioId, Guid unidadId, CancellationToken ct) =>
        _db.Unidades.AnyAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);

    private async Task<Result<IncidenciaUnidadDto>> Devolver(UnidadIncidencia i, CancellationToken ct)
    {
        var autor = await _db.Users.Where(u => u.Id == i.AutorUsuarioId)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim())
            .FirstOrDefaultAsync(ct);
        return Result<IncidenciaUnidadDto>.Ok(Mapear(i, autor ?? "—"));
    }
}
