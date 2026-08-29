using Condolio.Application.Calendario;
using Condolio.Application.Comunicaciones;
using Condolio.Application.Common;
using Condolio.Domain.Calendario;
using Condolio.Domain.Comunicaciones;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Calendario;

public class EventoService : IEventoService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAnuncioService _anuncios;

    public EventoService(CondolioDbContext db, ITenantContext tenant, IAnuncioService anuncios)
    {
        _db = db;
        _tenant = tenant;
        _anuncios = anuncios;
    }

    public async Task<Result<IReadOnlyList<EventoDto>>> ListarAsync(Guid consorcioId, DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<IReadOnlyList<EventoDto>>.Fail("Consorcio no encontrado.");

        var eventos = await _db.EventosCalendario
            .Where(e => e.ConsorcioId == consorcioId && e.InicioUtc < hasta && e.FinUtc > desde)
            .OrderBy(e => e.InicioUtc)
            .Select(e => Mapear(e))
            .ToListAsync(ct);
        return Result<IReadOnlyList<EventoDto>>.Ok(eventos);
    }

    public async Task<Result<EventoDto>> ObtenerAsync(Guid consorcioId, Guid eventoId, CancellationToken ct = default)
    {
        var e = await _db.EventosCalendario.FirstOrDefaultAsync(x => x.Id == eventoId && x.ConsorcioId == consorcioId, ct);
        return e is null ? Result<EventoDto>.Fail("Evento no encontrado.") : Result<EventoDto>.Ok(Mapear(e));
    }

    public async Task<Result<EventoDto>> CrearAsync(Guid consorcioId, GuardarEventoDto dto, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<EventoDto>.Fail("Consorcio no encontrado.");
        var val = Validar(dto);
        if (val is not null) return Result<EventoDto>.Fail(val);

        var autor = _tenant.UsuarioId is { } uid
            ? await _db.Users.Where(u => u.Id == uid).Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync(ct)
            : null;

        var e = new EventoCalendario
        {
            ConsorcioId = consorcioId,
            CreadoPorUsuarioId = _tenant.UsuarioId ?? string.Empty,
            CreadoPorNombre = autor ?? "Administración",
        };
        Aplicar(e, dto);
        e.NotificoComunidad = dto.NotificarComunidad;
        _db.EventosCalendario.Add(e);
        await _db.SaveChangesAsync(ct);

        if (dto.NotificarComunidad)
            await PublicarAnuncio(consorcioId, e, ct);

        return Result<EventoDto>.Ok(Mapear(e));
    }

    public async Task<Result<EventoDto>> ActualizarAsync(Guid consorcioId, Guid eventoId, GuardarEventoDto dto, CancellationToken ct = default)
    {
        var e = await _db.EventosCalendario.FirstOrDefaultAsync(x => x.Id == eventoId && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result<EventoDto>.Fail("Evento no encontrado.");
        var val = Validar(dto);
        if (val is not null) return Result<EventoDto>.Fail(val);

        var notificarAhora = dto.NotificarComunidad && !e.NotificoComunidad;
        Aplicar(e, dto);
        if (dto.NotificarComunidad) e.NotificoComunidad = true;
        await _db.SaveChangesAsync(ct);

        if (notificarAhora)
            await PublicarAnuncio(consorcioId, e, ct);

        return Result<EventoDto>.Ok(Mapear(e));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid eventoId, CancellationToken ct = default)
    {
        var n = await _db.EventosCalendario.Where(x => x.Id == eventoId && x.ConsorcioId == consorcioId)
            .ExecuteDeleteAsync(ct);
        return n == 0 ? Result.Fail("Evento no encontrado.") : Result.Ok();
    }

    // ---- helpers ----

    private static string? Validar(GuardarEventoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return "El título es obligatorio.";
        if (dto.FinUtc < dto.InicioUtc) return "El fin no puede ser anterior al inicio.";
        return null;
    }

    private static void Aplicar(EventoCalendario e, GuardarEventoDto dto)
    {
        e.Titulo = dto.Titulo.Trim();
        e.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        e.Ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim();
        e.Categoria = dto.Categoria;
        e.InicioUtc = dto.InicioUtc;
        e.FinUtc = dto.FinUtc < dto.InicioUtc ? dto.InicioUtc : dto.FinUtc;
        e.TodoElDia = dto.TodoElDia;
    }

    private async Task PublicarAnuncio(Guid consorcioId, EventoCalendario e, CancellationToken ct)
    {
        var cuerpo = $"{e.Titulo}\n\n{e.Descripcion}".Trim();
        if (!string.IsNullOrWhiteSpace(e.Ubicacion)) cuerpo += $"\n\nLugar: {e.Ubicacion}";
        await _anuncios.CrearAsync(consorcioId, new GuardarAnuncioDto(
            e.Titulo, cuerpo, CategoriaAnuncio.Evento, false, null, e.InicioUtc, null), ct);
    }

    private static EventoDto Mapear(EventoCalendario e) => new(
        e.Id, e.Titulo, e.Descripcion, e.Ubicacion, e.Categoria,
        e.InicioUtc, e.FinUtc, e.TodoElDia, e.NotificoComunidad,
        string.IsNullOrWhiteSpace(e.CreadoPorNombre) ? "Administración" : e.CreadoPorNombre);
}
