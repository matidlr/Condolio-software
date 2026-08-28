using Condolio.Domain.Common;
using Condolio.Domain.Consorcios;

namespace Condolio.Domain.Unidades;

public enum TipoUnidad
{
    Departamento = 0,
    Local = 1,
    Cochera = 2,
    Baulera = 3,
}

public enum TipoOcupacion
{
    /// <summary>El propietario vive en esta unidad.</summary>
    HabitadoPorPropietario = 0,
    /// <summary>Unidad alquilada a inquilinos.</summary>
    Alquiler = 1,
    /// <summary>Actualmente desocupada.</summary>
    Desocupado = 3,
}

/// <summary>Unidad funcional de un consorcio. La cantidad de unidades facturables define el precio del SaaS.</summary>
public class Unidad : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }
    public Consorcio Consorcio { get; set; } = null!;

    /// <summary>Nombre / identificador de la unidad: "1A", "PB 2", "Local 3".</summary>
    public string Nombre { get; set; } = string.Empty;

    public int Piso { get; set; }

    /// <summary>Superficie en m². Informativo; ayuda a sugerir el coeficiente.</summary>
    public decimal? AreaM2 { get; set; }

    /// <summary>Torre / sección / bloque al que pertenece la unidad.</summary>
    public string? Seccion { get; set; }

    public TipoUnidad Tipo { get; set; } = TipoUnidad.Departamento;

    public TipoOcupacion Ocupacion { get; set; } = TipoOcupacion.Desocupado;

    /// <summary>Permite que los inquilinos vean su saldo y suban comprobantes de pago.</summary>
    public bool InquilinosVenFinanzas { get; set; }

    /// <summary>Cuota de mantenimiento (expensa fija) mensual. Null = sin configurar.</summary>
    public decimal? CuotaMantenimiento { get; set; }

    /// <summary>Porcentual de expensas (coeficiente). Suma 100 dentro del consorcio.</summary>
    public decimal? Coeficiente { get; set; }

    /// <summary>Cuenta para el cálculo de precio de la suscripción del administrador.</summary>
    public bool Facturable { get; set; } = true;

    /// <summary>Personas asociadas a la unidad (propietarios, inquilinos, gestores).</summary>
    public List<UnidadPersona> Personas { get; set; } = new();
}
