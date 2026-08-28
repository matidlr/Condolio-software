using Condolio.Application.Common;
using Condolio.Application.Unidades;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Unidades;

public class ActividadUnidadService : IActividadUnidadService
{
    private readonly CondolioDbContext _db;
    private readonly ITenantContext _tenant;

    public ActividadUnidadService(CondolioDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task RegistrarAsync(Guid unidadId, TipoActividad tipo, string titulo, string? detalle, CancellationToken ct = default)
    {
        _db.UnidadActividades.Add(new UnidadActividad
        {
            UnidadId = unidadId,
            Tipo = tipo,
            Titulo = titulo,
            Detalle = detalle,
            ActorUsuarioId = _tenant.UsuarioId,
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Result<IReadOnlyList<ActividadUnidadDto>>> ListarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default)
    {
        var existe = await _db.Unidades.AnyAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (!existe) return Result<IReadOnlyList<ActividadUnidadDto>>.Fail("Unidad no encontrada.");

        var items = await (
            from a in _db.UnidadActividades.Where(a => a.UnidadId == unidadId)
            join u in _db.Users on a.ActorUsuarioId equals u.Id into gj
            from u in gj.DefaultIfEmpty()
            orderby a.CreadoUtc descending
            select new ActividadUnidadDto(
                a.Id,
                a.Tipo,
                a.Titulo,
                a.Detalle,
                u != null ? (u.Nombre + " " + u.Apellido).Trim() : "Sistema",
                a.CreadoUtc))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ActividadUnidadDto>>.Ok(items);
    }
}
