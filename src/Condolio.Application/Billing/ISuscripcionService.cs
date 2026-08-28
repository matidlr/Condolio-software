using Condolio.Application.Common;
using Condolio.Domain.Billing;

namespace Condolio.Application.Billing;

public record EstadoSuscripcionDto(
    EstadoSuscripcion Estado,
    DateTime TrialFinUtc,
    DateTime? ProximoCobroUtc,
    int UnidadesFacturadas,
    decimal ImporteMensual,
    string Moneda,
    bool AccesoPermitido);

public interface ISuscripcionService
{
    /// <summary>Crea la suscripción en Trial (2 meses) al dar de alta un administrador.</summary>
    Task<Result> IniciarTrialAsync(Guid administradorId, CancellationToken ct = default);

    /// <summary>Recalcula el importe mensual según la cantidad de unidades facturables actuales.</summary>
    Task<Result<decimal>> RecalcularImporteAsync(Guid administradorId, CancellationToken ct = default);

    /// <summary>Estado para el panel del administrador y el gate de acceso.</summary>
    Task<Result<EstadoSuscripcionDto>> ObtenerEstadoAsync(Guid administradorId, CancellationToken ct = default);
}
