using Condolio.Application.Accesos;
using Condolio.Application.Common;
using Condolio.Domain.Accesos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QRCoder;

namespace Condolio.Infrastructure.Accesos;

public class AccesoAdminService : IAccesoAdminService
{
    private readonly CondolioDbContext _db;
    private readonly string _frontendUrl;

    public AccesoAdminService(CondolioDbContext db, IConfiguration config)
    {
        _db = db;
        _frontendUrl = (config["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
    }

    // ================= QR / Pases =================

    public async Task<Result<PasesAdminListaDto>> ListarPasesAsync(
        Guid consorcioId, int anio, int mes, string? busqueda, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<PasesAdminListaDto>.Fail("Consorcio no encontrado.");

        var desde = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var hasta = desde.AddMonths(1);
        var hoy = DateTime.UtcNow.Date;

        var pases = await _db.PasesAcceso.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId)
            .Select(p => new
            {
                p,
                unidad = p.UnidadId == Guid.Empty ? null
                    : _db.Unidades.IgnoreQueryFilters().Where(u => u.Id == p.UnidadId).Select(u => u.Nombre).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var activos = pases.Count(x => EstadoReal(x.p) == EstadoPase.Activo);
        var enRango = pases.Count(x => x.p.CreadoUtc >= desde && x.p.CreadoUtc < hasta);
        var escaneadosHoy = await _db.RegistrosVisita.IgnoreQueryFilters()
            .CountAsync(v => v.ConsorcioId == consorcioId && v.PaseAccesoId != null && v.IngresoUtc.Date == hoy, ct);

        var q = (busqueda ?? "").Trim().ToLowerInvariant();
        var lista = pases
            .Where(x => x.p.CreadoUtc >= desde && x.p.CreadoUtc < hasta)
            .Where(x => q.Length == 0
                || x.p.VisitanteNombre.ToLowerInvariant().Contains(q)
                || (x.unidad ?? "").ToLowerInvariant().Contains(q)
                || x.p.CreadoPorNombre.ToLowerInvariant().Contains(q))
            .OrderByDescending(x => x.p.CreadoUtc)
            .Select(x => new PaseAdminDto(
                x.p.Id,
                "#" + x.p.Token[..6].ToUpperInvariant(),
                x.p.TipoPase, x.p.TipoVisita, x.p.Vehiculo, x.p.VisitanteNombre, x.p.Patente,
                x.p.FechaEntrada, x.p.ValidoHastaUtc, EstadoReal(x.p),
                x.p.CreadoPorNombre, x.p.CreadoUtc, x.p.UsosCount, x.p.UsosMax,
                string.IsNullOrEmpty(x.unidad) ? "Administración" : x.unidad!))
            .ToList();

        return Result<PasesAdminListaDto>.Ok(new PasesAdminListaDto(lista, activos, enRango, escaneadosHoy));
    }

    public async Task<Result<PaseAccesoDto>> ObtenerPaseAsync(Guid consorcioId, Guid paseId, CancellationToken ct = default)
    {
        var p = await _db.PasesAcceso.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == paseId && x.ConsorcioId == consorcioId, ct);
        if (p is null) return Result<PaseAccesoDto>.Fail("Pase no encontrado.");

        var datos = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => c.Nombre).FirstAsync(ct);
        var unidad = p.UnidadId == Guid.Empty ? "Administración"
            : await _db.Unidades.IgnoreQueryFilters().Where(u => u.Id == p.UnidadId).Select(u => u.Nombre).FirstOrDefaultAsync(ct) ?? "—";

        return Result<PaseAccesoDto>.Ok(new PaseAccesoDto(
            p.Id, p.TipoPase, p.TipoVisita, p.Vehiculo, p.VisitanteNombre, p.Patente,
            p.FechaEntrada, p.ValidoHastaUtc, EstadoReal(p), p.Token, p.CreadoPorNombre, p.CreadoUtc,
            p.UsosCount, p.UsosMax, datos, unidad, GenerarQr(p.Token)));
    }

    public async Task<Result<PaseAccesoDto>> CrearPaseAsync(
        Guid consorcioId, string usuarioId, string usuarioNombre, CrearPaseAdminDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.VisitanteNombre))
            return Result<PaseAccesoDto>.Fail("El nombre del visitante es obligatorio.");

        var consorcio = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => new { c.Nombre, c.AdministradorId }).FirstOrDefaultAsync(ct);
        if (consorcio is null) return Result<PaseAccesoDto>.Fail("Consorcio no encontrado.");

        string destino = "Administración";
        var unidadId = Guid.Empty;
        if (dto.UnidadId is { } uid)
        {
            var u = await _db.Unidades.IgnoreQueryFilters()
                .Where(x => x.Id == uid && x.ConsorcioId == consorcioId).Select(x => x.Nombre).FirstOrDefaultAsync(ct);
            if (u is null) return Result<PaseAccesoDto>.Fail("Unidad no encontrada.");
            unidadId = uid;
            destino = u;
        }

        var conVehiculo = dto.Vehiculo != TipoVehiculo.SinVehiculo;
        var p = new PaseAcceso
        {
            AdministradorId = consorcio.AdministradorId,
            ConsorcioId = consorcioId,
            UnidadId = unidadId,
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "Administración" : usuarioNombre,
            TipoPase = dto.TipoPase,
            TipoVisita = dto.TipoVisita,
            Vehiculo = dto.Vehiculo,
            VisitanteNombre = dto.VisitanteNombre.Trim(),
            Patente = conVehiculo && !string.IsNullOrWhiteSpace(dto.Patente) ? dto.Patente.Trim().ToUpperInvariant() : null,
            FechaEntrada = dto.FechaEntrada.Date,
            ValidoHastaUtc = dto.TipoPase == TipoPase.UnaEntrada ? dto.FechaEntrada.Date.AddDays(1) : dto.ValidoHasta,
            UsosMax = dto.TipoPase == TipoPase.PaseFiesta ? 50 : dto.TipoPase == TipoPase.Temporal ? 20 : 1,
        };
        _db.PasesAcceso.Add(p);
        await _db.SaveChangesAsync(ct);

        return Result<PaseAccesoDto>.Ok(new PaseAccesoDto(
            p.Id, p.TipoPase, p.TipoVisita, p.Vehiculo, p.VisitanteNombre, p.Patente,
            p.FechaEntrada, p.ValidoHastaUtc, p.Estado, p.Token, p.CreadoPorNombre, p.CreadoUtc,
            p.UsosCount, p.UsosMax, consorcio.Nombre, destino, GenerarQr(p.Token)));
    }

    public async Task<Result> RevocarPaseAsync(Guid consorcioId, Guid paseId, CancellationToken ct = default)
    {
        var n = await _db.PasesAcceso.IgnoreQueryFilters()
            .Where(p => p.Id == paseId && p.ConsorcioId == consorcioId && p.Estado == EstadoPase.Activo)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPase.Revocado), ct);
        return n == 0 ? Result.Fail("Pase no encontrado o ya no está activo.") : Result.Ok();
    }

    // ================= Bitácora =================

    public async Task<Result<BitacoraDto>> BitacoraAsync(
        Guid consorcioId, DateOnly fecha, int dias, string? filtro, string? busqueda, CancellationToken ct = default)
    {
        if (!await _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct))
            return Result<BitacoraDto>.Fail("Consorcio no encontrado.");

        var hasta = fecha.ToDateTime(TimeOnly.MaxValue);
        var desde = fecha.AddDays(-(Math.Max(1, dias) - 1)).ToDateTime(TimeOnly.MinValue);

        var registros = await _db.RegistrosVisita.IgnoreQueryFilters()
            .Where(v => v.ConsorcioId == consorcioId && v.IngresoUtc >= desde && v.IngresoUtc <= hasta)
            .Select(v => new
            {
                v,
                unidad = v.UnidadId == Guid.Empty ? null
                    : _db.Unidades.IgnoreQueryFilters().Where(u => u.Id == v.UnidadId).Select(u => u.Nombre).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var adentroAhora = await _db.RegistrosVisita.IgnoreQueryFilters()
            .CountAsync(v => v.ConsorcioId == consorcioId && v.EgresoUtc == null, ct);

        var q = (busqueda ?? "").Trim().ToLowerInvariant();
        var lista = registros
            .Where(x => filtro switch
            {
                "adentro" => x.v.EgresoUtc == null,
                "salio" => x.v.EgresoUtc != null,
                _ => true,
            })
            .Where(x => q.Length == 0
                || x.v.VisitanteNombre.ToLowerInvariant().Contains(q)
                || (x.v.Patente ?? "").ToLowerInvariant().Contains(q)
                || (x.unidad ?? "").ToLowerInvariant().Contains(q))
            .OrderByDescending(x => x.v.IngresoUtc)
            .Select(x => new RegistroBitacoraDto(
                x.v.Id, x.v.VisitanteNombre, x.v.TipoVisita, x.v.Vehiculo, x.v.Patente, x.v.DocumentoVisitante,
                string.IsNullOrEmpty(x.unidad) ? "Administración" : x.unidad!,
                x.v.IngresoUtc, x.v.EgresoUtc, x.v.PaseAccesoId != null,
                string.IsNullOrWhiteSpace(x.v.RegistradoPorNombre) ? "Portería" : x.v.RegistradoPorNombre,
                x.v.Nota))
            .ToList();

        return Result<BitacoraDto>.Ok(new BitacoraDto(lista, adentroAhora));
    }

    public async Task<Result> RegistrarEgresoAsync(Guid consorcioId, Guid registroId, CancellationToken ct = default)
    {
        var n = await _db.RegistrosVisita.IgnoreQueryFilters()
            .Where(v => v.Id == registroId && v.ConsorcioId == consorcioId && v.EgresoUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.EgresoUtc, DateTime.UtcNow), ct);
        return n == 0 ? Result.Fail("Registro no encontrado o ya tiene egreso.") : Result.Ok();
    }

    public async Task<Result<ResumenAccesoDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow.Date;
        var q = _db.RegistrosVisita.IgnoreQueryFilters().Where(v => v.ConsorcioId == consorcioId);
        var adentro = await q.CountAsync(v => v.EgresoUtc == null, ct);
        var entradasHoy = await q.CountAsync(v => v.IngresoUtc.Date == hoy, ct);
        var salidasHoy = await q.CountAsync(v => v.EgresoUtc != null && v.EgresoUtc.Value.Date == hoy, ct);
        return Result<ResumenAccesoDto>.Ok(new ResumenAccesoDto(adentro, entradasHoy, salidasHoy));
    }

    public async Task<Result<IReadOnlyList<RegistroBitacoraDto>>> AdentroAhoraAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var registros = await _db.RegistrosVisita.IgnoreQueryFilters()
            .Where(v => v.ConsorcioId == consorcioId && v.EgresoUtc == null)
            .Select(v => new
            {
                v,
                unidad = v.UnidadId == Guid.Empty ? null
                    : _db.Unidades.IgnoreQueryFilters().Where(u => u.Id == v.UnidadId).Select(u => u.Nombre).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var lista = registros
            .OrderByDescending(x => x.v.IngresoUtc)
            .Select(x => new RegistroBitacoraDto(
                x.v.Id, x.v.VisitanteNombre, x.v.TipoVisita, x.v.Vehiculo, x.v.Patente, x.v.DocumentoVisitante,
                string.IsNullOrEmpty(x.unidad) ? "Administración" : x.unidad!,
                x.v.IngresoUtc, x.v.EgresoUtc, x.v.PaseAccesoId != null,
                string.IsNullOrWhiteSpace(x.v.RegistradoPorNombre) ? "Portería" : x.v.RegistradoPorNombre,
                x.v.Nota))
            .ToList();
        return Result<IReadOnlyList<RegistroBitacoraDto>>.Ok(lista);
    }

    public async Task<Result<RegistroBitacoraDto>> RegistrarEntradaManualAsync(
        Guid consorcioId, EntradaManualDto dto, string guardiaId, string guardiaNombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.VisitanteNombre))
            return Result<RegistroBitacoraDto>.Fail("El nombre del visitante es obligatorio.");

        var admin = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (admin is not { } adminId) return Result<RegistroBitacoraDto>.Fail("Consorcio no encontrado.");

        string unidadNombre = "Administración";
        var unidadId = Guid.Empty;
        if (dto.UnidadId is { } uid)
        {
            var u = await _db.Unidades.IgnoreQueryFilters()
                .Where(x => x.Id == uid && x.ConsorcioId == consorcioId).Select(x => x.Nombre).FirstOrDefaultAsync(ct);
            if (u is null) return Result<RegistroBitacoraDto>.Fail("Unidad no encontrada.");
            unidadId = uid;
            unidadNombre = u;
        }

        var conVehiculo = dto.Vehiculo != TipoVehiculo.SinVehiculo;
        var r = new RegistroVisita
        {
            AdministradorId = adminId,
            ConsorcioId = consorcioId,
            UnidadId = unidadId,
            VisitanteNombre = dto.VisitanteNombre.Trim(),
            TipoVisita = dto.TipoVisita,
            Vehiculo = dto.Vehiculo,
            Patente = conVehiculo && !string.IsNullOrWhiteSpace(dto.Patente) ? dto.Patente.Trim().ToUpperInvariant() : null,
            IngresoUtc = DateTime.UtcNow,
            RegistradoPorUsuarioId = guardiaId,
            RegistradoPorNombre = guardiaNombre,
            DocumentoVisitante = string.IsNullOrWhiteSpace(dto.Documento) ? null : dto.Documento.Trim(),
            Nota = string.IsNullOrWhiteSpace(dto.Nota) ? null : dto.Nota.Trim(),
        };
        _db.RegistrosVisita.Add(r);
        await _db.SaveChangesAsync(ct);

        return Result<RegistroBitacoraDto>.Ok(new RegistroBitacoraDto(
            r.Id, r.VisitanteNombre, r.TipoVisita, r.Vehiculo, r.Patente, r.DocumentoVisitante, unidadNombre,
            r.IngresoUtc, null, false, guardiaNombre, r.Nota));
    }

    // ---- helpers ----

    private static EstadoPase EstadoReal(PaseAcceso p)
    {
        if (p.Estado == EstadoPase.Activo && p.ValidoHastaUtc is { } h && h < DateTime.UtcNow)
            return EstadoPase.Vencido;
        return p.Estado;
    }

    private string GenerarQr(string token)
    {
        var payload = $"{_frontendUrl}/verificar-acceso/{token}";
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        return Convert.ToBase64String(new PngByteQRCode(data).GetGraphic(10));
    }
}
