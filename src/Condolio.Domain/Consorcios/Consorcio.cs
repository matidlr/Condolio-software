using Condolio.Domain.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Domain.Consorcios;

public enum TipoConsorcio
{
    /// <summary>Edificio de múltiples unidades con pisos y departamentos.</summary>
    EdificioResidencial = 0,
    /// <summary>Barrio cerrado de casas independientes; cada casa es una unidad.</summary>
    ResidencialPrivada = 1,
}

/// <summary>Un edificio/consorcio administrado por un <c>Administrador</c> (tenant).</summary>
public class Consorcio : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public TipoConsorcio Tipo { get; set; } = TipoConsorcio.EdificioResidencial;
    public string Direccion { get; set; } = string.Empty;
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string Pais { get; set; } = "AR";
    public string? CodigoPostal { get; set; }
    public string? Cuit { get; set; }

    public double? Latitud { get; set; }
    public double? Longitud { get; set; }

    public bool Activo { get; set; } = true;

    public List<Unidad> Unidades { get; set; } = new();
}
