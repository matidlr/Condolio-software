using Condolio.Domain.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Domain.Consorcios;

/// <summary>Un edificio/consorcio administrado por un <c>Administrador</c> (tenant).</summary>
public class Consorcio : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string Pais { get; set; } = "AR";
    public string? Cuit { get; set; }

    public bool Activo { get; set; } = true;

    public List<Unidad> Unidades { get; set; } = new();
}
