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

public enum EstadoExtraordinaria
{
    /// <summary>Todavía tiene meses por emitir.</summary>
    Activa = 0,
    /// <summary>Se emitieron todas las cuotas mensuales.</summary>
    Finalizada = 1,
    /// <summary>Se dio de baja antes de terminar.</summary>
    Cancelada = 2,
}

/// <summary>Rubro de una expensa extraordinaria (para agrupar y reportar).</summary>
public enum CategoriaExtraordinaria
{
    MejorasCapital = 0,
    ReparacionEmergencia = 1,
    ProyectoEspecial = 2,
    Legales = 3,
    Equipamiento = 4,
    Otro = 5,
}

/// <summary>Cómo se calcula el cargo de cada unidad alcanzada por una extraordinaria.</summary>
public enum MetodoReparto
{
    /// <summary>Todas las unidades pagan lo mismo.</summary>
    Igual = 0,
    /// <summary>En proporción al coeficiente de cada unidad.</summary>
    ProporcionalPorCoeficiente = 1,
    /// <summary>El administrador carga el monto de cada unidad a mano.</summary>
    Personalizado = 2,
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

// ============ Expensas extraordinarias ============

/// <summary>
/// Cuota extraordinaria aprobada por asamblea (obra, reparación mayor, compra de equipo).
/// Aplica a un subconjunto de unidades, con un método de reparto y un prorrateo opcional en meses.
/// Siempre se cobra al propietario.
/// </summary>
public class Extraordinaria : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public CategoriaExtraordinaria Categoria { get; set; } = CategoriaExtraordinaria.Otro;

    /// <summary>Fecha desde la que corre el cargo (y primer vencimiento si no hay uno explícito).</summary>
    public DateOnly FechaInicio { get; set; }
    /// <summary>Vencimiento del cargo (o de la primera cuota si se prorratea).</summary>
    public DateOnly? FechaVencimiento { get; set; }

    public MetodoReparto MetodoReparto { get; set; } = MetodoReparto.Igual;

    /// <summary>Cantidad de meses en los que se divide el cargo de cada unidad (1 = pago único).</summary>
    public int CantidadMeses { get; set; } = 1;
    /// <summary>Meses ya incluidos en una liquidación emitida.</summary>
    public int MesesEmitidos { get; set; }

    /// <summary>Suma de los cargos de todas las unidades alcanzadas.</summary>
    public decimal MontoTotal { get; set; }

    public EstadoExtraordinaria Estado { get; set; } = EstadoExtraordinaria.Activa;
    public string? Notas { get; set; }

    public List<ExtraordinariaUnidad> Unidades { get; set; } = new();

    /// <summary>Importe mensual promedio (el remanente por redondeo se ajusta en la última cuota).</summary>
    public decimal MontoPorMes =>
        CantidadMeses <= 1 ? MontoTotal : Math.Round(MontoTotal / CantidadMeses, 2);
}

/// <summary>Monto que le toca a una unidad puntual dentro de una expensa extraordinaria.</summary>
public class ExtraordinariaUnidad : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public Guid ExtraordinariaId { get; set; }
    public Guid UnidadId { get; set; }
    /// <summary>Nombre de la unidad al momento de crear el cargo.</summary>
    public string UnidadNombre { get; set; } = string.Empty;

    /// <summary>Cargo total para esta unidad (se divide por CantidadMeses al liquidar).</summary>
    public decimal MontoAsignado { get; set; }
}

// ============ Cargos y cobranzas ============

public enum OrigenCargo
{
    ExpensaOrdinaria = 0,
    Extraordinaria = 1,
    Interes = 2,
    Ajuste = 3,
    Multa = 4,
    Amenidad = 5,
    Manual = 6,
}

public enum EstadoCargo
{
    Pendiente = 0,
    PagadoParcial = 1,
    Pagado = 2,
    Anulado = 3,
}

/// <summary>
/// Cargo individual contra una unidad (una cuota de una extraordinaria, una expensa del mes, un interés).
/// La morosidad se calcula sumando los cargos pendientes con vencimiento pasado.
/// </summary>
public class CargoUnidad : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public Guid UnidadId { get; set; }
    public string UnidadNombre { get; set; } = string.Empty;

    public OrigenCargo Origen { get; set; }
    /// <summary>Extraordinaria que lo generó (si aplica).</summary>
    public Guid? ExtraordinariaId { get; set; }

    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public decimal MontoPagado { get; set; }

    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    public EstadoCargo Estado { get; set; } = EstadoCargo.Pendiente;
    public DateOnly? FechaPago { get; set; }

    /// <summary>Nº de cuota dentro de la extraordinaria (1..N).</summary>
    public int Cuota { get; set; } = 1;
    public int TotalCuotas { get; set; } = 1;

    public decimal Saldo => Monto - MontoPagado;
}

// ============ Período y gastos del mes ============

public enum EstadoPeriodo
{
    /// <summary>Se cargan gastos.</summary>
    Abierto = 0,
    /// <summary>Ya se generaron las expensas por unidad.</summary>
    Liquidado = 1,
    /// <summary>Cerrado contablemente, no se toca más.</summary>
    Cerrado = 2,
}

/// <summary>De dónde salió una línea de gasto del período.</summary>
public enum OrigenGasto
{
    /// <summary>Precargado desde un empleado activo.</summary>
    Empleado = 0,
    /// <summary>Precargado desde un gasto fijo activo.</summary>
    GastoFijo = 1,
    /// <summary>Cargado a mano solo para este período.</summary>
    Unico = 2,
}

public enum AlcanceGasto
{
    /// <summary>Se reparte entre todas las unidades.</summary>
    Todas = 0,
    /// <summary>Se reparte solo entre las unidades seleccionadas.</summary>
    Subconjunto = 1,
}

/// <summary>Mes contable de expensas de un consorcio.</summary>
public class PeriodoExpensas : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public int Anio { get; set; }
    public int Mes { get; set; }

    public EstadoPeriodo Estado { get; set; } = EstadoPeriodo.Abierto;

    public DateOnly? FechaLiquidacion { get; set; }
    public string? LiquidadoPorUsuarioId { get; set; }
    public string? Notas { get; set; }

    public List<GastoPeriodo> Gastos { get; set; } = new();
}

/// <summary>Una línea de gasto dentro de un período (sueldo, abono fijo, gasto puntual).</summary>
public class GastoPeriodo : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public Guid PeriodoExpensasId { get; set; }

    public Guid RubroGastoId { get; set; }
    public Guid? ProveedorId { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateOnly Fecha { get; set; }

    public string? MetodoPago { get; set; }
    public string? CuentaPago { get; set; }
    public string? ComprobanteRuta { get; set; }

    public OrigenGasto Origen { get; set; } = OrigenGasto.Unico;
    /// <summary>Empleado del catálogo del que se precargó (si Origen = Empleado).</summary>
    public Guid? EmpleadoId { get; set; }
    /// <summary>Gasto fijo del catálogo del que se precargó (si Origen = GastoFijo).</summary>
    public Guid? GastoFijoId { get; set; }

    public AlcanceGasto Alcance { get; set; } = AlcanceGasto.Todas;
    public CriterioDistribucion CriterioDistribucion { get; set; } = CriterioDistribucion.PorCoeficiente;

    /// <summary>Si está imputado a una extraordinaria, no entra al prorrateo ordinario del mes.</summary>
    public Guid? ExtraordinariaId { get; set; }

    /// <summary>Unidades alcanzadas cuando Alcance = Subconjunto.</summary>
    public List<GastoPeriodoUnidad> Unidades { get; set; } = new();
}

public class GastoPeriodoUnidad : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public Guid GastoPeriodoId { get; set; }
    public Guid UnidadId { get; set; }
}
