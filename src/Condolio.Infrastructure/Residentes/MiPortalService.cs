using Condolio.Application.Amenidades;
using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Domain.Amenidades;
using Condolio.Domain.Encuestas;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Residentes;

public class MiPortalService : IMiPortalService
{
    /// <summary>Los datetime del sistema se guardan en hora local de Argentina (UTC-3, sin DST).</summary>
    private static DateTime AhoraLocal => DateTime.UtcNow.AddHours(-3);

    private readonly CondolioDbContext _db;
    private readonly IAmenidadService _amenidades;

    public MiPortalService(CondolioDbContext db, IAmenidadService amenidades)
    {
        _db = db;
        _amenidades = amenidades;
    }

    private async Task<Origen?> OrigenAsync(string usuarioId, CancellationToken ct) =>
        await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new Origen(
                p.AdministradorId,
                p.UnidadId,
                p.Unidad.Nombre,
                p.Unidad.ConsorcioId,
                p.Unidad.Consorcio.Nombre,
                p.Unidad.Consorcio.Localidad,
                (p.Nombre + " " + p.Apellido).Trim()))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<PortalCasaDto>> CasaAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<PortalCasaDto>.Fail("Todavía no tenés una unidad asignada.");

        var ahora = AhoraLocal;

        var votadas = await _db.VotosEncuesta.IgnoreQueryFilters()
            .Where(v => v.UsuarioId == usuarioId).Select(v => v.EncuestaId).Distinct().ToListAsync(ct);

        var pendientes = await _db.Encuestas.IgnoreQueryFilters()
            .Where(e => e.ConsorcioId == o.ConsorcioId && e.Estado == EstadoEncuesta.Activa && !votadas.Contains(e.Id))
            .OrderBy(e => e.CierreUtc ?? DateTime.MaxValue)
            .Select(e => new { e.Id, e.Titulo, e.CierreUtc })
            .ToListAsync(ct);

        var encuestasPendientes = pendientes
            .Select(e => new EncuestaPendienteDto(e.Id, e.Titulo,
                e.CierreUtc is { } c ? Math.Max(0, (int)Math.Ceiling((c - ahora).TotalDays)) : (int?)null))
            .ToList();

        var reservas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SolicitanteUsuarioId == usuarioId && r.Fin >= ahora
                && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .OrderBy(r => r.Inicio).Take(5)
            .Select(r => new ReservaResumenDto(r.Id, r.Amenidad.Nombre, r.Inicio, r.Fin, r.Estado.ToString()))
            .ToListAsync(ct);

        return Result<PortalCasaDto>.Ok(new PortalCasaDto(
            o.ConsorcioId, o.ConsorcioNombre, o.Localidad, o.UnidadNombre,
            encuestasPendientes, reservas, 0, 0));
    }

    public async Task<Result<IReadOnlyList<AmenidadDto>>> AmenidadesAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<AmenidadDto>>.Fail("No tenés una unidad asignada.");

        var res = await _amenidades.ListarAsync(o.ConsorcioId, ct);
        if (!res.Exito) return Result<IReadOnlyList<AmenidadDto>>.Fail(res.Error!);

        var reservables = res.Valor!.Amenidades.Where(a => a.Reservable).ToList();
        return Result<IReadOnlyList<AmenidadDto>>.Ok(reservables);
    }

    public async Task<Result<AmenidadDto>> AmenidadAsync(string usuarioId, Guid amenidadId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<AmenidadDto>.Fail("No tenés una unidad asignada.");
        return await _amenidades.ObtenerAsync(o.ConsorcioId, amenidadId, ct);
    }

    public async Task<Result<IReadOnlyList<SlotDto>>> SlotsAsync(string usuarioId, Guid amenidadId, DateOnly fecha, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<SlotDto>>.Fail("No tenés una unidad asignada.");

        var amenidad = await _db.Amenidades.IgnoreQueryFilters()
            .Include(a => a.Horarios)
            .FirstOrDefaultAsync(a => a.Id == amenidadId && a.ConsorcioId == o.ConsorcioId, ct);
        if (amenidad is null) return Result<IReadOnlyList<SlotDto>>.Fail("Amenidad no encontrada.");

        var dia = fecha.ToDateTime(TimeOnly.MinValue).DayOfWeek;
        var horario = amenidad.Horarios.FirstOrDefault(h => h.Dia == dia);
        if (horario is null || horario.Cerrado)
            return Result<IReadOnlyList<SlotDto>>.Ok(Array.Empty<SlotDto>());

        var dur = amenidad.IntervaloMinutos <= 0 ? 60 : amenidad.IntervaloMinutos;
        // La grilla de horarios y las reservas se manejan en hora local (naive).
        var medianoche = fecha.ToDateTime(TimeOnly.MinValue);
        var ahora = AhoraLocal;

        var ocupadas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.AmenidadId == amenidadId
                && r.Estado != EstadoReserva.Rechazada && r.Estado != EstadoReserva.Cancelada
                && r.Fin > medianoche && r.Inicio < medianoche.AddDays(1))
            .Select(r => new { r.Inicio, r.Fin })
            .ToListAsync(ct);

        var slots = new List<SlotDto>();
        for (var m = horario.AbreMin; m + dur <= horario.CierraMin; m += dur)
        {
            var inicio = medianoche.AddMinutes(m);
            var fin = inicio.AddMinutes(dur);
            if (fin <= ahora) continue;
            if (ocupadas.Any(x => x.Inicio < fin && x.Fin > inicio)) continue;
            slots.Add(new SlotDto(inicio, fin));
        }
        return Result<IReadOnlyList<SlotDto>>.Ok(slots);
    }

    public async Task<Result<MiReservaDto>> SolicitarReservaAsync(
        string usuarioId, Guid amenidadId, DateTime inicio, DateTime fin, string? nota, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<MiReservaDto>.Fail("No tenés una unidad asignada.");
        if (fin <= inicio) return Result<MiReservaDto>.Fail("El horario no es válido.");

        var amenidad = await _db.Amenidades.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == amenidadId && a.ConsorcioId == o.ConsorcioId, ct);
        if (amenidad is null) return Result<MiReservaDto>.Fail("Amenidad no encontrada.");
        if (!amenidad.Reservable) return Result<MiReservaDto>.Fail("Esta amenidad no admite reservas.");

        var activasPropias = await _db.Reservas.IgnoreQueryFilters().CountAsync(r =>
            r.AmenidadId == amenidadId && r.SolicitanteUsuarioId == usuarioId
            && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada)
            && r.Fin >= AhoraLocal, ct);
        if (amenidad.MaxReservasPorUnidad > 0 && activasPropias >= amenidad.MaxReservasPorUnidad && !amenidad.LimiteMensual)
            return Result<MiReservaDto>.Fail($"Alcanzaste el máximo de {amenidad.MaxReservasPorUnidad} reserva(s) activa(s).");

        var solapa = await _db.Reservas.IgnoreQueryFilters().AnyAsync(r => r.AmenidadId == amenidadId
            && r.Estado != EstadoReserva.Rechazada && r.Estado != EstadoReserva.Cancelada
            && r.Inicio < fin && r.Fin > inicio, ct);
        if (solapa) return Result<MiReservaDto>.Fail("Ese horario ya está reservado.");

        var horas = (decimal)(fin - inicio).TotalHours;
        var reserva = new Reserva
        {
            AdministradorId = o.AdministradorId,
            ConsorcioId = o.ConsorcioId,
            AmenidadId = amenidadId,
            UnidadId = o.UnidadId,
            SolicitanteUsuarioId = usuarioId,
            SolicitanteNombre = string.IsNullOrWhiteSpace(o.Nombre) ? "Residente" : o.Nombre,
            Inicio = inicio,
            Fin = fin,
            Estado = amenidad.RequiereAprobacion ? EstadoReserva.Pendiente : EstadoReserva.Confirmada,
            Importe = amenidad.TieneCosto ? amenidad.Tarifa * Math.Max(1, Math.Ceiling(horas)) : null,
            Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
        };
        if (reserva.Estado == EstadoReserva.Confirmada) reserva.ResueltaUtc = DateTime.UtcNow;

        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync(ct);

        return Result<MiReservaDto>.Ok(new MiReservaDto(
            reserva.Id, amenidadId, amenidad.Nombre, Adjuntos(amenidad.ImagenesIds),
            reserva.Inicio, reserva.Fin, reserva.Estado.ToString(), reserva.Nota, reserva.CreadoUtc));
    }

    public async Task<Result<MisReservasDto>> MisReservasAsync(string usuarioId, CancellationToken ct = default)
    {
        var reservas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SolicitanteUsuarioId == usuarioId)
            .OrderByDescending(r => r.Inicio)
            .Select(r => new
            {
                r.Id, r.AmenidadId, Amenidad = r.Amenidad.Nombre, r.Amenidad.ImagenesIds,
                r.Inicio, r.Fin, r.Estado, r.Nota, r.CreadoUtc,
            })
            .ToListAsync(ct);

        var ahora = AhoraLocal;
        var mapped = reservas.Select(r => new
        {
            Activa = r.Fin >= ahora && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada),
            r.Inicio,
            Dto = new MiReservaDto(r.Id, r.AmenidadId, r.Amenidad, Adjuntos(r.ImagenesIds),
                r.Inicio, r.Fin, r.Estado.ToString(), r.Nota, r.CreadoUtc),
        }).ToList();

        var activas = mapped.Where(x => x.Activa).OrderBy(x => x.Inicio).Select(x => x.Dto).ToList();
        var previas = mapped.Where(x => !x.Activa).Select(x => x.Dto).ToList();

        return Result<MisReservasDto>.Ok(new MisReservasDto(activas, previas));
    }

    public async Task<Result> CancelarReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default)
    {
        var n = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.Id == reservaId && r.SolicitanteUsuarioId == usuarioId
                && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Estado, EstadoReserva.Cancelada)
                .SetProperty(r => r.ResueltaUtc, DateTime.UtcNow), ct);
        return n == 0 ? Result.Fail("Reserva no encontrada.") : Result.Ok();
    }

    private static IReadOnlyList<Guid> Adjuntos(string? ids) =>
        string.IsNullOrWhiteSpace(ids)
            ? Array.Empty<Guid>()
            : ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToArray();

    private sealed record Origen(
        Guid AdministradorId, Guid UnidadId, string UnidadNombre,
        Guid ConsorcioId, string ConsorcioNombre, string? Localidad, string Nombre);
}
