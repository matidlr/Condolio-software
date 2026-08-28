using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Domain.Billing;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Billing;

public class SuscripcionService : ISuscripcionService
{
    private readonly CondolioDbContext _db;
    private readonly IClock _clock;

    public SuscripcionService(CondolioDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> IniciarTrialAsync(Guid administradorId, CancellationToken ct = default)
    {
        if (await _db.Suscripciones.AnyAsync(s => s.AdministradorId == administradorId, ct))
            return Result.Fail("El administrador ya tiene una suscripción.");

        var plan = await _db.Planes.Include(p => p.Tramos)
            .FirstOrDefaultAsync(p => p.Activo, ct);
        if (plan is null) return Result.Fail("No hay un plan activo configurado.");

        var ahora = _clock.UtcNow;
        _db.Suscripciones.Add(new Suscripcion
        {
            AdministradorId = administradorId,
            PlanId = plan.Id,
            Estado = EstadoSuscripcion.Trial,
            TrialInicioUtc = ahora,
            TrialFinUtc = ahora.AddMonths(2),
            UnidadesFacturadas = 0,
            ImporteMensual = plan.CargoBaseMensual,
        });
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<decimal>> RecalcularImporteAsync(Guid administradorId, CancellationToken ct = default)
    {
        var suscripcion = await _db.Suscripciones
            .Include(s => s.Plan).ThenInclude(p => p.Tramos)
            .FirstOrDefaultAsync(s => s.AdministradorId == administradorId, ct);
        if (suscripcion is null) return Result<decimal>.Fail("Sin suscripción.");

        // Nota: las Unidades tienen query filter por tenant; acá contamos sin filtro
        // porque el recálculo puede correr en un job (SuperAdmin / sistema).
        var unidades = await _db.Unidades.IgnoreQueryFilters()
            .CountAsync(u => u.AdministradorId == administradorId && u.Facturable, ct);

        var importe = suscripcion.Plan.CalcularImporte(unidades);
        suscripcion.UnidadesFacturadas = unidades;
        suscripcion.ImporteMensual = importe;
        await _db.SaveChangesAsync(ct);
        return Result<decimal>.Ok(importe);
    }

    public async Task<Result<EstadoSuscripcionDto>> ObtenerEstadoAsync(Guid administradorId, CancellationToken ct = default)
    {
        var s = await _db.Suscripciones
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.AdministradorId == administradorId, ct);
        if (s is null) return Result<EstadoSuscripcionDto>.Fail("Sin suscripción.");

        var ahora = _clock.UtcNow;
        var acceso = s.Estado switch
        {
            EstadoSuscripcion.Trial => ahora <= s.TrialFinUtc,
            EstadoSuscripcion.Activa => true,
            EstadoSuscripcion.PagoPendiente => true, // período de gracia
            _ => false,
        };

        return Result<EstadoSuscripcionDto>.Ok(new EstadoSuscripcionDto(
            s.Estado, s.TrialFinUtc, s.ProximoCobroUtc, s.UnidadesFacturadas,
            s.ImporteMensual, s.Plan.Moneda, acceso));
    }
}
