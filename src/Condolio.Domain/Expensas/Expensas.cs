using Condolio.Domain.Common;

namespace Condolio.Domain.Expensas;

// ============ Enums ============

/// <summary>Tipo de rubro para agrupar los gastos en la liquidación.</summary>
public enum TipoRubro
{
    Ordinario = 0,
    Extraordinario = 1,
    FondoReserva = 2,
}

/// <summary>Cómo se reparte un gasto entre las unidades.</summary>
public enum CriterioDistribucion
{
    /// <summary>Según el coeficiente (porcentual) de cada unidad. Suma 100.</summary>
    PorCoeficiente = 0,
    /// <summary>En partes iguales entre las unidades facturables.</summary>
    PartesIguales = 1,
}

public enum FondoReservaTipo
{
    Ninguno = 0,
    /// <summary>Porcentaje de los gastos ordinarios del período.</summary>
    PorcentajeDeGastos = 1,
    /// <summary>Monto fijo mensual, prorrateado por coeficiente.</summary>
    MontoFijo = 2,
}

// ============ Configuración ============

/// <summary>Parámetros de expensas de un consorcio (una fila por consorcio).</summary>
public class ConfiguracionExpensas : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    /// <summary>Día del mes del primer vencimiento (1-28).</summary>
    public int DiaPrimerVencimiento { get; set; } = 10;

    /// <summary>Día del segundo vencimiento. Null = sin segundo vencimiento.</summary>
    public int? DiaSegundoVencimiento { get; set; } = 20;

    /// <summary>Recargo % que se aplica si paga después del primer vencimiento.</summary>
    public decimal RecargoSegundoVencimientoPct { get; set; }

    /// <summary>Interés mensual % sobre el saldo impago a la fecha de emisión.</summary>
    public decimal TasaInteresMoraMensualPct { get; set; }

    public FondoReservaTipo FondoReservaTipo { get; set; } = FondoReservaTipo.Ninguno;
    public decimal FondoReservaValor { get; set; }

    /// <summary>Si true, el inquilino paga las expensas ordinarias (default legal en AR).</summary>
    public bool InquilinoPagaOrdinarias { get; set; } = true;

    /// <summary>Redondea el total de cada unidad al peso más cercano.</summary>
    public bool RedondearAlPeso { get; set; } = true;

    // ---- Mercado Pago ----

    /// <summary>Access token de la cuenta de Mercado Pago del administrador (para cobrar).</summary>
    public string? MercadoPagoAccessToken { get; set; }
    public string? MercadoPagoPublicKey { get; set; }
    public bool MercadoPagoActivo { get; set; }
}

// ============ Catálogo ============

/// <summary>Rubro para agrupar gastos en la liquidación (Sueldos, Servicios, Honorarios…).</summary>
public class RubroGasto : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public TipoRubro Tipo { get; set; } = TipoRubro.Ordinario;
    public int Orden { get; set; }

    /// <summary>Rubro creado por el sistema: no se puede borrar.</summary>
    public bool EsSistema { get; set; }
}

/// <summary>Proveedor del consorcio (a quién se le paga un gasto).</summary>
public class Proveedor : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Empresa { get; set; }
    /// <summary>Categoría: Plomería, Electricidad, Limpieza, Mantenimiento, Seguridad, Otro…</summary>
    public string? Rubro { get; set; }
    public string? Cuit { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? TelefonoAlt { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    /// <summary>CBU para transferencias.</summary>
    public string? Cbu { get; set; }
    public string? Alias { get; set; }
    public string? Horario { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Proveedor destacado / de confianza del consorcio.</summary>
    public bool Recomendado { get; set; }
}

// ============ Gastos fijos ============

/// <summary>Empleado del consorcio (encargado, portero, personal de limpieza en relación de dependencia).</summary>
public class Empleado : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Cuil { get; set; }

    /// <summary>Ej. "Encargado permanente con vivienda", "Suplente con retiro".</summary>
    public string? Categoria { get; set; }

    public decimal SueldoBasico { get; set; }

    /// <summary>Cargas sociales patronales como % del sueldo básico.</summary>
    public decimal CargasSocialesPct { get; set; }

    /// <summary>Provisiona 1/12 del sueldo por mes para el aguinaldo (SAC).</summary>
    public bool ProvisionaAguinaldo { get; set; } = true;

    /// <summary>ART, sindicato (SUTERH), seguro de vida y otros conceptos fijos mensuales.</summary>
    public decimal OtrosConceptosMensuales { get; set; }

    /// <summary>Rubro donde se imputa el costo (por defecto "Sueldos y jornales").</summary>
    public Guid? RubroGastoId { get; set; }

    public DateOnly? FechaIngreso { get; set; }
    public bool Activo { get; set; } = true;
    public string? Notas { get; set; }

    /// <summary>Costo total mensual que genera este empleado.</summary>
    public decimal CostoMensualTotal =>
        SueldoBasico
        + Math.Round(SueldoBasico * CargasSocialesPct / 100m, 2)
        + OtrosConceptosMensuales
        + (ProvisionaAguinaldo ? Math.Round(SueldoBasico / 12m, 2) : 0m);
}

/// <summary>Gasto fijo recurrente que se precarga en cada período (abono ascensor, seguro, honorarios…).</summary>
public class GastoFijo : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public Guid RubroGastoId { get; set; }
    public Guid? ProveedorId { get; set; }

    public decimal MontoEstimado { get; set; }
    public CriterioDistribucion CriterioDistribucion { get; set; } = CriterioDistribucion.PorCoeficiente;

    public bool Activo { get; set; } = true;
    public string? Notas { get; set; }
}
