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
