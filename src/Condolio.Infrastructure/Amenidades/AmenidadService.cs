using Condolio.Application.Amenidades;
using Condolio.Application.Common;
using Condolio.Domain.Amenidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Amenidades;

public class AmenidadService : IAmenidadService
{
    private readonly CondolioDbContext _db;

    public AmenidadService(CondolioDbContext db) => _db = db;

    public async Task<Result<AmenidadListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<AmenidadListaDto>.Fail("Consorcio no encontrado.");

        var amenidades = await _db.Amenidades
            .Include(a => a.Horarios)
            .Where(a => a.ConsorcioId == consorcioId)
            .OrderBy(a => a.Nombre)
            .ToListAsync(ct);

        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var reservas = await _db.Reservas
            .Where(r => r.ConsorcioId == consorcioId)
            .Select(r => new { r.AmenidadId, r.Inicio, r.Estado, r.Importe })
            .ToListAsync(ct);

        var proximasPorAmenidad = reservas
            .Where(r => r.Estado == EstadoReserva.Confirmada && r.Inicio >= ahora)
            .GroupBy(r => r.AmenidadId)
            .ToDictionary(g => g.Key, g => g.Count());

        var lista = amenidades
            .Select(a => Mapear(a, proximasPorAmenidad.GetValueOrDefault(a.Id)))
            .ToList();

        var dto = new AmenidadListaDto(
            lista,
            amenidades.Count,
            amenidades.Count(a => a.Reservable),
            reservas.Count(r => r.Inicio >= inicioMes && r.Estado != EstadoReserva.Cancelada && r.Estado != EstadoReserva.Rechazada),
            reservas.Count(r => r.Estado == EstadoReserva.Pendiente),
            reservas.Where(r => r.Estado == EstadoReserva.Confirmada).Sum(r => r.Importe ?? 0));

        return Result<AmenidadListaDto>.Ok(dto);
    }

    public async Task<Result<AmenidadDto>> ObtenerAsync(Guid consorcioId, Guid amenidadId, CancellationToken ct = default)
    {
        var a = await _db.Amenidades.Include(x => x.Horarios)
            .FirstOrDefaultAsync(x => x.Id == amenidadId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result<AmenidadDto>.Fail("Amenidad no encontrada.");

        var proximas = await _db.Reservas.CountAsync(
            r => r.AmenidadId == a.Id && r.Estado == EstadoReserva.Confirmada && r.Inicio >= DateTime.UtcNow, ct);
        return Result<AmenidadDto>.Ok(Mapear(a, proximas));
    }

    public async Task<Result<AmenidadDto>> CrearAsync(Guid consorcioId, GuardarAmenidadDto dto, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<AmenidadDto>.Fail("Consorcio no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<AmenidadDto>.Fail("El nombre es obligatorio.");

        var a = new Amenidad { ConsorcioId = consorcioId };
        AplicarEscalares(a, dto);
        a.Horarios = ConstruirHorarios(dto);
        _db.Amenidades.Add(a);
        await _db.SaveChangesAsync(ct);
        return Result<AmenidadDto>.Ok(Mapear(a, 0));
    }

    public async Task<Result<AmenidadDto>> ActualizarAsync(Guid consorcioId, Guid amenidadId, GuardarAmenidadDto dto, CancellationToken ct = default)
    {
        var a = await _db.Amenidades
            .FirstOrDefaultAsync(x => x.Id == amenidadId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result<AmenidadDto>.Fail("Amenidad no encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<AmenidadDto>.Fail("El nombre es obligatorio.");

        var viejos = await _db.AmenidadHorarios.Where(h => h.AmenidadId == a.Id).ToListAsync(ct);
        _db.AmenidadHorarios.RemoveRange(viejos);

        AplicarEscalares(a, dto);
        foreach (var h in ConstruirHorarios(dto))
        {
            h.AmenidadId = a.Id;
            _db.AmenidadHorarios.Add(h);
        }
        await _db.SaveChangesAsync(ct);

        a.Horarios = await _db.AmenidadHorarios.Where(h => h.AmenidadId == a.Id).ToListAsync(ct);
        var proximas = await _db.Reservas.CountAsync(
            r => r.AmenidadId == a.Id && r.Estado == EstadoReserva.Confirmada && r.Inicio >= DateTime.UtcNow, ct);
        return Result<AmenidadDto>.Ok(Mapear(a, proximas));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid amenidadId, CancellationToken ct = default)
    {
        var a = await _db.Amenidades.Include(x => x.Horarios)
            .FirstOrDefaultAsync(x => x.Id == amenidadId && x.ConsorcioId == consorcioId, ct);
        if (a is null) return Result.Fail("Amenidad no encontrada.");

        if (await _db.Reservas.AnyAsync(r => r.AmenidadId == a.Id
            && r.Estado == EstadoReserva.Confirmada && r.Inicio >= DateTime.UtcNow, ct))
            return Result.Fail("No se puede eliminar: tiene reservas confirmadas a futuro.");

        _db.AmenidadHorarios.RemoveRange(a.Horarios);
        _db.Amenidades.Remove(a);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ---- helpers ----

    private static void AplicarEscalares(Amenidad a, GuardarAmenidadDto dto)
    {
        a.Nombre = dto.Nombre.Trim();
        a.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        a.ImagenesIds = dto.ImagenesIds is { Count: > 0 }
            ? string.Join(",", dto.ImagenesIds)
            : null;
        a.Reservable = dto.Reservable;
        a.IntervaloMinutos = dto.IntervaloMinutos < 0 ? 60 : dto.IntervaloMinutos; // 0 = flexible, 1440 = día completo
        a.LimiteMensual = dto.LimiteMensual;
        a.MaxReservasPorUnidad = Math.Max(0, dto.MaxReservasPorUnidad); // 0 = ilimitadas
        a.TieneCosto = dto.TieneCosto;
        a.Tarifa = dto.TieneCosto ? dto.Tarifa : null;
        a.RequiereAprobacion = dto.RequiereAprobacion;
        a.ReservableDesde = dto.ReservableDesde;
        a.DiasBloqueados = dto.DiasBloqueados is { Count: > 0 }
            ? string.Join(",", dto.DiasBloqueados.Select(d => d.Trim()).Where(d => d.Length > 0))
            : null;
        a.MensajeReserva = string.IsNullOrWhiteSpace(dto.MensajeReserva) ? null : dto.MensajeReserva.Trim();
    }

    private static List<AmenidadHorario> ConstruirHorarios(GuardarAmenidadDto dto) =>
        (dto.Horarios ?? new()).Select(h => new AmenidadHorario
        {
            Dia = (DayOfWeek)h.Dia,
            Cerrado = h.Cerrado,
            AbreMin = h.AbreMin,
            CierraMin = h.CierraMin,
        }).ToList();

    private static AmenidadDto Mapear(Amenidad a, int reservasProximas) => new(
        a.Id, a.Nombre, a.Descripcion, ParseIds(a.ImagenesIds),
        a.Reservable, a.IntervaloMinutos, a.LimiteMensual, a.MaxReservasPorUnidad,
        a.TieneCosto, a.Tarifa, a.RequiereAprobacion,
        a.ReservableDesde, ParseCsv(a.DiasBloqueados), a.MensajeReserva,
        a.Horarios.OrderBy(h => (int)h.Dia)
            .Select(h => new AmenidadHorarioDto((int)h.Dia, h.Cerrado, h.AbreMin, h.CierraMin)).ToList(),
        reservasProximas, a.CreadoUtc, a.ActualizadoUtc);

    private static IReadOnlyList<string> ParseCsv(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IReadOnlyList<Guid> ParseIds(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<Guid>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                 .Where(g => g != Guid.Empty)
                 .ToList();
}
