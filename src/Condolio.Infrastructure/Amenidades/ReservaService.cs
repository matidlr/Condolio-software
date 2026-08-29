using Condolio.Application.Amenidades;
using Condolio.Application.Common;
using Condolio.Domain.Amenidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Amenidades;

public class ReservaService : IReservaService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;

    public ReservaService(CondolioDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Result<ReservaListaDto>> ListarAsync(Guid consorcioId, DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<ReservaListaDto>.Fail("Consorcio no encontrado.");

        var todas = await _db.Reservas
            .Where(r => r.ConsorcioId == consorcioId)
            .Select(r => new
            {
                r,
                amenidad = _db.Amenidades.Where(a => a.Id == r.AmenidadId).Select(a => a.Nombre).FirstOrDefault(),
                unidad = r.UnidadId == null ? null : _db.Unidades.Where(u => u.Id == r.UnidadId).Select(u => u.Nombre).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var hoy = DateTime.UtcNow.Date;

        var enRango = todas
            .Where(x => x.r.Inicio < hasta && x.r.Fin > desde)
            .OrderBy(x => x.r.Inicio)
            .Select(x => Mapear(x.r, x.amenidad ?? "—", x.unidad))
            .ToList();

        var dto = new ReservaListaDto(
            enRango,
            todas.Count(x => x.r.Estado == EstadoReserva.Pendiente),
            todas.Count(x => x.r.Estado == EstadoReserva.Confirmada),
            todas.Count(x => x.r.Estado == EstadoReserva.Rechazada),
            todas.Count(x => x.r.Estado == EstadoReserva.Confirmada && x.r.Inicio.Date == hoy));

        return Result<ReservaListaDto>.Ok(dto);
    }

    public async Task<Result<ReservaDto>> CrearAsync(Guid consorcioId, CrearReservaDto dto, CancellationToken ct = default)
    {
        var amenidad = await _db.Amenidades
            .FirstOrDefaultAsync(a => a.Id == dto.AmenidadId && a.ConsorcioId == consorcioId, ct);
        if (amenidad is null) return Result<ReservaDto>.Fail("Amenidad no encontrada.");
        if (!amenidad.Reservable) return Result<ReservaDto>.Fail("Esta amenidad no admite reservas.");
        if (dto.Fin <= dto.Inicio) return Result<ReservaDto>.Fail("El horario de fin debe ser posterior al de inicio.");

        var solapa = await _db.Reservas.AnyAsync(r => r.AmenidadId == dto.AmenidadId
            && r.Estado != EstadoReserva.Rechazada && r.Estado != EstadoReserva.Cancelada
            && r.Inicio < dto.Fin && r.Fin > dto.Inicio, ct);
        if (solapa) return Result<ReservaDto>.Fail("Ya hay una reserva en ese horario.");

        string? unidadNombre = null;
        if (dto.UnidadId is { } uid)
        {
            unidadNombre = await _db.Unidades.Where(u => u.Id == uid && u.ConsorcioId == consorcioId)
                .Select(u => u.Nombre).FirstOrDefaultAsync(ct);
            if (unidadNombre is null) return Result<ReservaDto>.Fail("Unidad no encontrada.");
        }

        var horas = (decimal)(dto.Fin - dto.Inicio).TotalHours;
        var reserva = new Reserva
        {
            ConsorcioId = consorcioId,
            AmenidadId = dto.AmenidadId,
            UnidadId = dto.UnidadId,
            SolicitanteUsuarioId = _tenant.UsuarioId ?? string.Empty,
            SolicitanteNombre = await NombreUsuario(_tenant.UsuarioId, ct) ?? "Administración",
            Inicio = dto.Inicio,
            Fin = dto.Fin,
            Estado = amenidad.RequiereAprobacion ? EstadoReserva.Pendiente : EstadoReserva.Confirmada,
            Importe = amenidad.TieneCosto ? amenidad.Tarifa * Math.Max(1, Math.Ceiling(horas)) : null,
            Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim(),
        };
        if (reserva.Estado == EstadoReserva.Confirmada) reserva.ResueltaUtc = DateTime.UtcNow;

        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync(ct);
        return Result<ReservaDto>.Ok(Mapear(reserva, amenidad.Nombre, unidadNombre));
    }

    public async Task<Result<ReservaDto>> CambiarEstadoAsync(Guid consorcioId, Guid reservaId, EstadoReserva estado, CancellationToken ct = default)
    {
        var r = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == reservaId && x.ConsorcioId == consorcioId, ct);
        if (r is null) return Result<ReservaDto>.Fail("Reserva no encontrada.");

        r.Estado = estado;
        r.ResueltaUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var amenidad = await _db.Amenidades.Where(a => a.Id == r.AmenidadId).Select(a => a.Nombre).FirstOrDefaultAsync(ct);
        var unidad = r.UnidadId is null ? null
            : await _db.Unidades.Where(u => u.Id == r.UnidadId).Select(u => u.Nombre).FirstOrDefaultAsync(ct);
        return Result<ReservaDto>.Ok(Mapear(r, amenidad ?? "—", unidad));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid reservaId, CancellationToken ct = default)
    {
        var r = await _db.Reservas.FirstOrDefaultAsync(x => x.Id == reservaId && x.ConsorcioId == consorcioId, ct);
        if (r is null) return Result.Fail("Reserva no encontrada.");
        _db.Reservas.Remove(r);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<string?> NombreUsuario(string? id, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return await _db.Users.Where(u => u.Id == id)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim()).FirstOrDefaultAsync(ct);
    }

    private static ReservaDto Mapear(Reserva r, string amenidadNombre, string? unidadNombre) => new(
        r.Id, r.AmenidadId, amenidadNombre, r.UnidadId, unidadNombre,
        string.IsNullOrWhiteSpace(r.SolicitanteNombre) ? "—" : r.SolicitanteNombre,
        r.Inicio, r.Fin, r.Estado, r.Importe, r.Nota, r.CreadoUtc);
}
