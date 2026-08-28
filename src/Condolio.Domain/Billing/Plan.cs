using Condolio.Domain.Common;

namespace Condolio.Domain.Billing;

/// <summary>
/// Plan de precios del SaaS. El costo mensual se arma sumando los tramos de
/// <see cref="Tramos"/> segun la cantidad de unidades administradas.
/// </summary>
public class Plan : Entity
{
    public string Nombre { get; set; } = "Estándar";
    public string Moneda { get; set; } = "ARS";
    public bool Activo { get; set; } = true;

    /// <summary>Cargo fijo mensual, independiente de la cantidad de unidades.</summary>
    public decimal CargoBaseMensual { get; set; }

    public List<PlanTramo> Tramos { get; set; } = new();

    /// <summary>Calcula el importe mensual para una cantidad de unidades dada.</summary>
    public decimal CalcularImporte(int unidades)
    {
        var total = CargoBaseMensual;
        foreach (var tramo in Tramos.OrderBy(t => t.DesdeUnidad))
        {
            if (unidades <= tramo.DesdeUnidad) break;
            var tope = tramo.HastaUnidad ?? int.MaxValue;
            var enTramo = Math.Min(unidades, tope) - tramo.DesdeUnidad;
            total += enTramo * tramo.PrecioPorUnidad;
        }
        return total;
    }
}

/// <summary>Tramo de precios por unidad: (DesdeUnidad, HastaUnidad] a PrecioPorUnidad.</summary>
public class PlanTramo : Entity
{
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public int DesdeUnidad { get; set; }
    public int? HastaUnidad { get; set; }
    public decimal PrecioPorUnidad { get; set; }
}
