using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Residentes;

public class VistaResidenteService : IVistaResidenteService
{
    private readonly CondolioDbContext _db;

    public VistaResidenteService(CondolioDbContext db) => _db = db;

    public async Task<Result<MiPanelDto>> MiPanelAsync(string usuarioId, string nombre, CancellationToken ct = default)
    {
        var nombreReal = await _db.Users
            .Where(u => u.Id == usuarioId)
            .Select(u => (u.Nombre + " " + u.Apellido).Trim())
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(nombreReal)) nombre = nombreReal;

        var unidades = await _db.UnidadPersonas
            .IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new MiUnidadDto(
                p.UnidadId,
                p.Unidad.Nombre,
                p.Unidad.ConsorcioId,
                p.Unidad.Consorcio.Nombre,
                p.Rol,
                p.EsContactoPrincipal,
                p.Unidad.CuotaMantenimiento,
                0m)) // TODO: saldo real cuando exista el módulo de expensas
            .ToListAsync(ct);

        return Result<MiPanelDto>.Ok(new MiPanelDto(nombre, unidades));
    }
}
