using Condolio.Application.Accesos;
using Condolio.Application.Common;
using Condolio.Domain.Accesos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QRCoder;

namespace Condolio.Infrastructure.Accesos;

public class PaseAccesoService : IPaseAccesoService
{
    private readonly CondolioDbContext _db;
    private readonly string _frontendUrl;

    public PaseAccesoService(CondolioDbContext db, IConfiguration config)
    {
        _db = db;
        _frontendUrl = (config["Frontend:BaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
    }

    public async Task<Result<IReadOnlyList<PaseAccesoDto>>> MisPasesAsync(string usuarioId, CancellationToken ct = default)
    {
        var pases = await _db.PasesAcceso.IgnoreQueryFilters()
            .Where(p => p.CreadoPorUsuarioId == usuarioId)
            .OrderByDescending(p => p.CreadoUtc)
            .Take(50)
            .ToListAsync(ct);

        var ctx = await ContextoAsync(usuarioId, ct);
        var lista = pases.Select(p => Mapear(p, ctx.consorcio, ctx.unidad)).ToList();
        return Result<IReadOnlyList<PaseAccesoDto>>.Ok(lista);
    }

    public async Task<Result<PaseAccesoDto>> ObtenerAsync(string usuarioId, Guid paseId, CancellationToken ct = default)
    {
        var p = await _db.PasesAcceso.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == paseId && x.CreadoPorUsuarioId == usuarioId, ct);
        if (p is null) return Result<PaseAccesoDto>.Fail("Pase no encontrado.");

        var ctx = await ContextoAsync(usuarioId, ct);
        return Result<PaseAccesoDto>.Ok(Mapear(p, ctx.consorcio, ctx.unidad));
    }

    public async Task<Result<PaseAccesoDto>> CrearAsync(string usuarioId, CrearPaseDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.VisitanteNombre))
            return Result<PaseAccesoDto>.Fail("El nombre del visitante es obligatorio.");

        var origen = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => new
            {
                x.AdministradorId,
                x.UnidadId,
                x.Unidad.ConsorcioId,
                ConsorcioNombre = x.Unidad.Consorcio.Nombre,
                UnidadNombre = x.Unidad.Nombre,
                Nombre = (x.Nombre + " " + x.Apellido).Trim(),
            })
            .FirstOrDefaultAsync(ct);
        if (origen is null) return Result<PaseAccesoDto>.Fail("No tenés una unidad asignada.");

        var esConVehiculo = dto.Vehiculo != TipoVehiculo.SinVehiculo;
        var p = new PaseAcceso
        {
            AdministradorId = origen.AdministradorId,
            ConsorcioId = origen.ConsorcioId,
            UnidadId = origen.UnidadId,
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombre = string.IsNullOrWhiteSpace(origen.Nombre) ? "Residente" : origen.Nombre,
            TipoPase = dto.TipoPase,
            TipoVisita = dto.TipoVisita,
            Vehiculo = dto.Vehiculo,
            VisitanteNombre = dto.VisitanteNombre.Trim(),
            Patente = esConVehiculo && !string.IsNullOrWhiteSpace(dto.Patente) ? dto.Patente.Trim().ToUpperInvariant() : null,
            FechaEntrada = dto.FechaEntrada.Date,
            ValidoHastaUtc = dto.TipoPase == TipoPase.UnaEntrada ? dto.FechaEntrada.Date.AddDays(1) : dto.ValidoHasta,
            UsosMax = dto.TipoPase == TipoPase.PaseFiesta ? 50 : dto.TipoPase == TipoPase.Temporal ? 20 : 1,
        };

        _db.PasesAcceso.Add(p);
        await _db.SaveChangesAsync(ct);

        return Result<PaseAccesoDto>.Ok(Mapear(p, origen.ConsorcioNombre, origen.UnidadNombre));
    }

    public async Task<Result> RevocarAsync(string usuarioId, Guid paseId, CancellationToken ct = default)
    {
        var n = await _db.PasesAcceso.IgnoreQueryFilters()
            .Where(p => p.Id == paseId && p.CreadoPorUsuarioId == usuarioId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Estado, EstadoPase.Revocado), ct);
        return n == 0 ? Result.Fail("Pase no encontrado.") : Result.Ok();
    }

    public async Task<Result<IReadOnlyList<VisitaDto>>> MisVisitasAsync(string usuarioId, CancellationToken ct = default)
    {
        var unidadId = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId).Select(p => (Guid?)p.UnidadId).FirstOrDefaultAsync(ct);
        if (unidadId is not { } uid) return Result<IReadOnlyList<VisitaDto>>.Fail("No tenés una unidad asignada.");

        var visitas = await _db.RegistrosVisita.IgnoreQueryFilters()
            .Where(v => v.UnidadId == uid)
            .OrderByDescending(v => v.IngresoUtc)
            .Take(200)
            .Select(v => new VisitaDto(
                v.Id, v.VisitanteNombre, v.TipoVisita, v.Vehiculo, v.Patente,
                v.IngresoUtc, v.EgresoUtc,
                string.IsNullOrWhiteSpace(v.RegistradoPorNombre) ? "Portería" : v.RegistradoPorNombre,
                v.Nota))
            .ToListAsync(ct);
        return Result<IReadOnlyList<VisitaDto>>.Ok(visitas);
    }

    public async Task<Result<VerificarPaseResultado>> VerificarAsync(
        Guid consorcioId, string token, string guardiaUsuarioId, string guardiaNombre, CancellationToken ct = default)
    {
        var p = await _db.PasesAcceso.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Token == token && x.ConsorcioId == consorcioId, ct);
        if (p is null)
            return Result<VerificarPaseResultado>.Ok(new VerificarPaseResultado(
                false, "El código QR no corresponde a este consorcio.", "", TipoVisita.Familia, null, "", "", 0));

        var unidad = await _db.Unidades.IgnoreQueryFilters()
            .Where(u => u.Id == p.UnidadId)
            .Select(u => new { u.Nombre, Consorcio = u.Consorcio.Nombre })
            .FirstOrDefaultAsync(ct);

        string? motivo = p.Estado switch
        {
            EstadoPase.Revocado => "El pase fue revocado por el residente.",
            EstadoPase.Vencido => "El pase está vencido.",
            EstadoPase.Usado => "El pase ya fue utilizado.",
            _ when p.ValidoHastaUtc is { } h && h < DateTime.UtcNow => "El pase está vencido.",
            _ when p.UsosCount >= p.UsosMax => "El pase no tiene ingresos disponibles.",
            _ => null,
        };

        var res = new VerificarPaseResultado(
            motivo is null, motivo, p.VisitanteNombre, p.TipoVisita, p.Patente,
            unidad?.Nombre ?? "—", unidad?.Consorcio ?? "—", Math.Max(0, p.UsosMax - p.UsosCount - 1));

        if (motivo is not null) return Result<VerificarPaseResultado>.Ok(res);

        p.UsosCount++;
        p.PrimerUsoUtc ??= DateTime.UtcNow;
        if (p.UsosCount >= p.UsosMax) p.Estado = EstadoPase.Usado;

        _db.RegistrosVisita.Add(new RegistroVisita
        {
            AdministradorId = p.AdministradorId,
            ConsorcioId = p.ConsorcioId,
            UnidadId = p.UnidadId,
            PaseAccesoId = p.Id,
            VisitanteNombre = p.VisitanteNombre,
            TipoVisita = p.TipoVisita,
            Vehiculo = p.Vehiculo,
            Patente = p.Patente,
            IngresoUtc = DateTime.UtcNow,
            RegistradoPorUsuarioId = guardiaUsuarioId,
            RegistradoPorNombre = guardiaNombre,
        });
        await _db.SaveChangesAsync(ct);

        return Result<VerificarPaseResultado>.Ok(res);
    }

    // ---- helpers ----

    private async Task<(string consorcio, string unidad)> ContextoAsync(string usuarioId, CancellationToken ct)
    {
        var x = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new { C = p.Unidad.Consorcio.Nombre, U = p.Unidad.Nombre })
            .FirstOrDefaultAsync(ct);
        return (x?.C ?? "", x?.U ?? "");
    }

    private PaseAccesoDto Mapear(PaseAcceso p, string consorcioNombre, string unidadNombre)
    {
        var estado = p.Estado;
        if (estado == EstadoPase.Activo && p.ValidoHastaUtc is { } hasta && hasta < DateTime.UtcNow)
            estado = EstadoPase.Vencido;

        return new PaseAccesoDto(
            p.Id, p.TipoPase, p.TipoVisita, p.Vehiculo, p.VisitanteNombre, p.Patente,
            p.FechaEntrada, p.ValidoHastaUtc, estado, p.Token, p.CreadoPorNombre, p.CreadoUtc,
            p.UsosCount, p.UsosMax, consorcioNombre, unidadNombre, GenerarQr(p.Token));
    }

    private string GenerarQr(string token)
    {
        var payload = $"{_frontendUrl}/verificar-acceso/{token}";
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(10);
        return Convert.ToBase64String(png);
    }
}
