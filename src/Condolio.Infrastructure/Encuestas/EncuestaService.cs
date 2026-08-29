using Condolio.Application.Common;
using Condolio.Application.Encuestas;
using Condolio.Domain.Encuestas;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Encuestas;

public class EncuestaService : IEncuestaService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;

    public EncuestaService(CondolioDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<EncuestaListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<EncuestaListaDto>.Fail("Consorcio no encontrado.");

        var encuestas = await _db.Encuestas
            .Where(e => e.ConsorcioId == consorcioId)
            .Include(e => e.Opciones)
            .Include(e => e.Votos)
            .OrderByDescending(e => e.Estado == EstadoEncuesta.Activa)
            .ThenByDescending(e => e.PublicadaUtc ?? e.CreadoUtc)
            .ToListAsync(ct);

        var yo = _tenant.UsuarioId ?? string.Empty;
        var lista = encuestas.Select(e => Mapear(e, yo)).ToList();

        var stats = new EstadisticasEncuestasDto(
            encuestas.Count,
            encuestas.Count(e => e.Estado == EstadoEncuesta.Activa),
            encuestas.Count(e => e.Estado == EstadoEncuesta.Borrador),
            encuestas.Count(e => e.Estado == EstadoEncuesta.Cerrada),
            encuestas.Sum(e => e.Votos.Count));

        return Result<EncuestaListaDto>.Ok(new EncuestaListaDto(
            lista, stats,
            encuestas.Count(e => e.Categoria == CategoriaEncuesta.General),
            encuestas.Count(e => e.Categoria == CategoriaEncuesta.Mantenimiento),
            encuestas.Count(e => e.Categoria == CategoriaEncuesta.Evento)));
    }

    public async Task<Result<EncuestaDetalleDto>> ObtenerAsync(Guid consorcioId, Guid encuestaId, CancellationToken ct = default)
    {
        var e = await _db.Encuestas
            .Include(x => x.Opciones)
            .Include(x => x.Votos)
            .FirstOrDefaultAsync(x => x.Id == encuestaId && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result<EncuestaDetalleDto>.Fail("Encuesta no encontrada.");

        var yo = _tenant.UsuarioId ?? string.Empty;
        var dto = Mapear(e, yo);

        var votantes = e.Anonima
            ? new List<VotanteDto>()
            : e.Votos.OrderBy(v => v.CreadoUtc)
                .Select(v => new VotanteDto(
                    string.IsNullOrWhiteSpace(v.UsuarioNombre) ? "—" : v.UsuarioNombre,
                    e.Opciones.FirstOrDefault(o => o.Id == v.OpcionId)?.Texto ?? "—",
                    v.CreadoUtc))
                .ToList();

        return Result<EncuestaDetalleDto>.Ok(new EncuestaDetalleDto(dto, votantes));
    }

    public async Task<Result<EncuestaDto>> CrearAsync(Guid consorcioId, GuardarEncuestaDto dto, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<EncuestaDto>.Fail("Consorcio no encontrado.");

        var val = Validar(dto);
        if (val is not null) return Result<EncuestaDto>.Fail(val);

        var e = new Encuesta
        {
            ConsorcioId = consorcioId,
            AutorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            AutorNombre = await NombreUsuario(_tenant.UsuarioId, ct) ?? "Administración",
        };
        Aplicar(e, dto);
        _db.Encuestas.Add(e);
        await _db.SaveChangesAsync(ct);
        return Result<EncuestaDto>.Ok(Mapear(e, _tenant.UsuarioId ?? string.Empty));
    }

    public async Task<Result<EncuestaDto>> ActualizarAsync(Guid consorcioId, Guid encuestaId, GuardarEncuestaDto dto, CancellationToken ct = default)
    {
        var e = await _db.Encuestas
            .Include(x => x.Opciones)
            .Include(x => x.Votos)
            .FirstOrDefaultAsync(x => x.Id == encuestaId && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result<EncuestaDto>.Fail("Encuesta no encontrada.");
        if (e.Votos.Count > 0) return Result<EncuestaDto>.Fail("No se puede editar una encuesta con votos.");

        var val = Validar(dto);
        if (val is not null) return Result<EncuestaDto>.Fail(val);

        _db.OpcionesEncuesta.RemoveRange(e.Opciones);
        e.Opciones.Clear();
        Aplicar(e, dto);
        await _db.SaveChangesAsync(ct);
        return Result<EncuestaDto>.Ok(Mapear(e, _tenant.UsuarioId ?? string.Empty));
    }

    public async Task<Result<EncuestaDto>> CambiarEstadoAsync(Guid consorcioId, Guid encuestaId, EstadoEncuesta estado, CancellationToken ct = default)
    {
        var e = await _db.Encuestas
            .Include(x => x.Opciones)
            .Include(x => x.Votos)
            .FirstOrDefaultAsync(x => x.Id == encuestaId && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result<EncuestaDto>.Fail("Encuesta no encontrada.");

        e.Estado = estado;
        if (estado == EstadoEncuesta.Activa && e.PublicadaUtc is null)
            e.PublicadaUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result<EncuestaDto>.Ok(Mapear(e, _tenant.UsuarioId ?? string.Empty));
    }

    public async Task<Result<EncuestaDto>> VotarAsync(Guid consorcioId, Guid encuestaId, IReadOnlyList<Guid> opcionesIds, CancellationToken ct = default)
    {
        var e = await _db.Encuestas.AsNoTracking()
            .Include(x => x.Opciones)
            .FirstOrDefaultAsync(x => x.Id == encuestaId && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result<EncuestaDto>.Fail("Encuesta no encontrada.");
        if (e.Estado != EstadoEncuesta.Activa) return Result<EncuestaDto>.Fail("La encuesta no está abierta a votación.");
        if (e.CierreUtc is { } cierre && cierre < DateTime.UtcNow) return Result<EncuestaDto>.Fail("La encuesta ya finalizó.");

        var validas = opcionesIds.Where(id => e.Opciones.Any(o => o.Id == id)).Distinct().ToList();
        if (validas.Count == 0) return Result<EncuestaDto>.Fail("Elegí al menos una opción.");
        if (!e.MultiplesOpciones && validas.Count > 1) return Result<EncuestaDto>.Fail("Solo podés elegir una opción.");

        var yo = _tenant.UsuarioId ?? string.Empty;
        var previos = await _db.VotosEncuesta.Where(v => v.EncuestaId == encuestaId && v.UsuarioId == yo).ToListAsync(ct);
        if (previos.Count > 0) _db.VotosEncuesta.RemoveRange(previos);

        var nombre = await NombreUsuario(yo, ct) ?? "—";
        foreach (var opcionId in validas)
        {
            _db.VotosEncuesta.Add(new VotoEncuesta
            {
                EncuestaId = encuestaId,
                AdministradorId = e.AdministradorId,
                OpcionId = opcionId,
                UsuarioId = yo,
                UsuarioNombre = nombre,
            });
        }
        await _db.SaveChangesAsync(ct);

        var actualizada = await _db.Encuestas
            .Include(x => x.Opciones)
            .Include(x => x.Votos)
            .AsNoTracking()
            .FirstAsync(x => x.Id == encuestaId, ct);
        return Result<EncuestaDto>.Ok(Mapear(actualizada, yo));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid encuestaId, CancellationToken ct = default)
    {
        var e = await _db.Encuestas
            .Include(x => x.Opciones)
            .Include(x => x.Votos)
            .FirstOrDefaultAsync(x => x.Id == encuestaId && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result.Fail("Encuesta no encontrada.");

        _db.VotosEncuesta.RemoveRange(e.Votos);
        _db.OpcionesEncuesta.RemoveRange(e.Opciones);
        _db.Encuestas.Remove(e);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ---- helpers ----

    private static string? Validar(GuardarEncuestaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return "El título es obligatorio.";
        var opciones = dto.Opciones.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();
        if (opciones.Count < 2) return "Se necesitan al menos dos opciones.";
        return null;
    }

    private static void Aplicar(Encuesta e, GuardarEncuestaDto dto)
    {
        e.Titulo = dto.Titulo.Trim();
        e.Descripcion = dto.Descripcion?.Trim() ?? string.Empty;
        e.Categoria = dto.Categoria;
        e.ModoVotacion = dto.ModoVotacion;
        e.MultiplesOpciones = dto.MultiplesOpciones;
        e.Anonima = dto.Anonima;
        e.CierreUtc = dto.CierreUtc;
        e.Estado = dto.Publicar ? EstadoEncuesta.Activa : EstadoEncuesta.Borrador;
        if (e.Estado == EstadoEncuesta.Activa && e.PublicadaUtc is null)
            e.PublicadaUtc = DateTime.UtcNow;

        var orden = 0;
        foreach (var texto in dto.Opciones.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()))
        {
            e.Opciones.Add(new OpcionEncuesta
            {
                EncuestaId = e.Id,
                AdministradorId = e.AdministradorId,
                Texto = texto,
                Orden = orden++,
            });
        }
    }

    private async Task<string?> NombreUsuario(string? id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return await _db.Users.Where(u => u.Id == id)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync(ct);
    }

    private static EncuestaDto Mapear(Encuesta e, string yo)
    {
        var totalVotos = e.Votos.Count;
        var votantes = e.Votos.Select(v => v.UsuarioId).Distinct().Count();
        var opciones = e.Opciones.OrderBy(o => o.Orden).Select(o =>
        {
            var votos = e.Votos.Count(v => v.OpcionId == o.Id);
            return new OpcionResultadoDto(
                o.Id, o.Texto, votos,
                totalVotos == 0 ? 0 : Math.Round(votos * 100.0 / totalVotos, 1),
                !string.IsNullOrEmpty(yo) && e.Votos.Any(v => v.OpcionId == o.Id && v.UsuarioId == yo));
        }).ToList();

        return new EncuestaDto(
            e.Id, e.Titulo, e.Descripcion, e.Categoria, e.Estado, e.ModoVotacion,
            e.MultiplesOpciones, e.Anonima, e.PublicadaUtc, e.CierreUtc,
            string.IsNullOrWhiteSpace(e.AutorNombre) ? "Administración" : e.AutorNombre,
            totalVotos, votantes,
            !string.IsNullOrEmpty(yo) && e.Votos.Any(v => v.UsuarioId == yo),
            opciones);
    }
}
