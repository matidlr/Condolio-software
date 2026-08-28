using Condolio.Domain.Common;
using Condolio.Domain.Tenancy;

namespace Condolio.Domain.Billing;

public enum EstadoSuscripcion
{
    /// <summary>2 meses gratis al dar de alta el administrador.</summary>
    Trial = 0,
    /// <summary>Suscripción paga y al día.</summary>
    Activa = 1,
    /// <summary>Venció el trial o falló el cobro; en período de gracia.</summary>
    PagoPendiente = 2,
    /// <summary>Sin pago; acceso bloqueado (solo lectura / bloqueo total).</summary>
    Suspendida = 3,
    /// <summary>Baja definitiva.</summary>
    Cancelada = 4,
}

/// <summary>
/// Suscripción SaaS de un <see cref="Administrador"/>. El precio se recalcula por
/// cantidad de unidades facturables administradas (ver <see cref="Plan"/>).
/// </summary>
public class Suscripcion : Entity
{
    public Guid AdministradorId { get; set; }
    public Administrador Administrador { get; set; } = null!;

    public EstadoSuscripcion Estado { get; set; } = EstadoSuscripcion.Trial;

    public DateTime TrialInicioUtc { get; set; } = DateTime.UtcNow;
    public DateTime TrialFinUtc { get; set; } = DateTime.UtcNow.AddMonths(2);

    /// <summary>Próxima fecha de cobro cuando <see cref="Estado"/> es Activa.</summary>
    public DateTime? ProximoCobroUtc { get; set; }

    /// <summary>Cantidad de unidades tomada en el último cálculo de precio.</summary>
    public int UnidadesFacturadas { get; set; }

    /// <summary>Importe mensual vigente (moneda del plan), ya calculado por tramos.</summary>
    public decimal ImporteMensual { get; set; }

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    /// <summary>Id de la suscripción/preapproval en Mercado Pago.</summary>
    public string? ProveedorSuscripcionId { get; set; }
}
