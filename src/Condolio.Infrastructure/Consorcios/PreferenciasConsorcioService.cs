using Condolio.Application.Common;
using Condolio.Application.Consorcios;
using Condolio.Domain.Consorcios;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Consorcios;

public class PreferenciasConsorcioService : IPreferenciasConsorcioService
{
    private readonly CondolioDbContext _db;

    public PreferenciasConsorcioService(CondolioDbContext db) => _db = db;

    public async Task<Result<PreferenciasConsorcioDto>> ObtenerAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var p = await CargarAsync(consorcioId, ct);
        return p is null
            ? Result<PreferenciasConsorcioDto>.Fail("Consorcio no encontrado.")
            : Result<PreferenciasConsorcioDto>.Ok(Map(p));
    }

    public async Task<Result<PreferenciasConsorcioDto>> GuardarAsync(
        Guid consorcioId, PreferenciasConsorcioDto dto, CancellationToken ct = default)
    {
        var p = await CargarAsync(consorcioId, ct);
        if (p is null) return Result<PreferenciasConsorcioDto>.Fail("Consorcio no encontrado.");

        p.ResidentesPublican = dto.ResidentesPublican;
        p.ComentariosHabilitados = dto.ComentariosHabilitados;
        p.AnunciosSiemprePorCorreo = dto.AnunciosSiemprePorCorreo;
        await _db.SaveChangesAsync(ct);
        return Result<PreferenciasConsorcioDto>.Ok(Map(p));
    }

    /// <summary>Devuelve (creando si hace falta) la fila de preferencias del consorcio.</summary>
    private async Task<PreferenciasConsorcio?> CargarAsync(Guid consorcioId, CancellationToken ct)
    {
        var p = await _db.PreferenciasConsorcio.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ConsorcioId == consorcioId, ct);
        if (p is not null) return p;

        var adminId = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (adminId is not { } admin) return null;

        p = new PreferenciasConsorcio { AdministradorId = admin, ConsorcioId = consorcioId };
        _db.PreferenciasConsorcio.Add(p);
        await _db.SaveChangesAsync(ct);
        return p;
    }

    private static PreferenciasConsorcioDto Map(PreferenciasConsorcio p) =>
        new(p.ResidentesPublican, p.ComentariosHabilitados, p.AnunciosSiemprePorCorreo);
}
