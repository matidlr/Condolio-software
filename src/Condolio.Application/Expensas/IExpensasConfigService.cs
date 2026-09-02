using Condolio.Application.Common;
using Condolio.Domain.Expensas;

namespace Condolio.Application.Expensas;

// ---------- Configuración ----------

public record ConfigExpensasDto(
    int DiaPrimerVencimiento,
    int? DiaSegundoVencimiento,
    decimal RecargoSegundoVencimientoPct,
    decimal TasaInteresMoraMensualPct,
    FondoReservaTipo FondoReservaTipo,
    decimal FondoReservaValor,
    bool InquilinoPagaOrdinarias,
    bool RedondearAlPeso,
    bool MercadoPagoActivo,
    string? MercadoPagoTokenPreview);

public record GuardarConfigExpensasDto(
    int DiaPrimerVencimiento,
    int? DiaSegundoVencimiento,
    decimal RecargoSegundoVencimientoPct,
    decimal TasaInteresMoraMensualPct,
    FondoReservaTipo FondoReservaTipo,
    decimal FondoReservaValor,
    bool InquilinoPagaOrdinarias,
    bool RedondearAlPeso);

public record GuardarMercadoPagoDto(string? AccessToken, string? PublicKey);

// ---------- Rubros ----------

public record RubroGastoDto(Guid Id, string Nombre, TipoRubro Tipo, int Orden, bool EsSistema);
public record GuardarRubroDto(string Nombre, TipoRubro Tipo);

// ---------- Proveedores ----------

public record ProveedorDto(
    Guid Id, string Nombre, string? Empresa, string? Rubro, string? Cuit, string? Email,
    string? Telefono, string? TelefonoAlt, string? Direccion, string? SitioWeb,
    string? Cbu, string? Alias, string? Horario, string? Notas, bool Activo, bool Recomendado);

public record GuardarProveedorDto(
    string Nombre, string? Empresa, string? Rubro, string? Cuit, string? Email,
    string? Telefono, string? TelefonoAlt, string? Direccion, string? SitioWeb,
    string? Cbu, string? Alias, string? Horario, string? Notas, bool Recomendado = false);

public record ProveedoresListaDto(
    IReadOnlyList<ProveedorDto> Proveedores, int Total, int Activos, int Inactivos, int Recomendados);

// ---------- Empleados ----------

public record EmpleadoDto(
    Guid Id, string Nombre, string Apellido, string? Cuil, string? Categoria,
    decimal SueldoBasico, decimal CargasSocialesPct, bool ProvisionaAguinaldo,
    decimal OtrosConceptosMensuales, Guid? RubroGastoId, DateOnly? FechaIngreso,
    bool Activo, string? Notas, decimal CostoMensualTotal);

public record GuardarEmpleadoDto(
    string Nombre, string Apellido, string? Cuil, string? Categoria,
    decimal SueldoBasico, decimal CargasSocialesPct, bool ProvisionaAguinaldo,
    decimal OtrosConceptosMensuales, Guid? RubroGastoId, DateOnly? FechaIngreso, string? Notas);

// ---------- Gastos fijos ----------

public record GastoFijoDto(
    Guid Id, string Descripcion, Guid RubroGastoId, string RubroNombre,
    Guid? ProveedorId, string? ProveedorNombre, decimal MontoEstimado,
    CriterioDistribucion CriterioDistribucion, bool Activo, string? Notas);

public record GuardarGastoFijoDto(
    string Descripcion, Guid RubroGastoId, Guid? ProveedorId, decimal MontoEstimado,
    CriterioDistribucion CriterioDistribucion, string? Notas);

// ---------- Resumen para la pantalla de gastos fijos ----------

public record GastosFijosResumenDto(
    IReadOnlyList<EmpleadoDto> Empleados,
    IReadOnlyList<GastoFijoDto> Gastos,
    decimal TotalEmpleados,
    decimal TotalGastos,
    decimal TotalMensual);

public interface IExpensasConfigService
{
    Task<Result<ConfigExpensasDto>> ObtenerConfigAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<ConfigExpensasDto>> GuardarConfigAsync(Guid consorcioId, GuardarConfigExpensasDto dto, CancellationToken ct = default);
    Task<Result<ConfigExpensasDto>> GuardarMercadoPagoAsync(Guid consorcioId, GuardarMercadoPagoDto dto, CancellationToken ct = default);

    Task<Result<IReadOnlyList<RubroGastoDto>>> ListarRubrosAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<RubroGastoDto>> CrearRubroAsync(Guid consorcioId, GuardarRubroDto dto, CancellationToken ct = default);
    Task<Result<RubroGastoDto>> ActualizarRubroAsync(Guid consorcioId, Guid id, GuardarRubroDto dto, CancellationToken ct = default);
    Task<Result> EliminarRubroAsync(Guid consorcioId, Guid id, CancellationToken ct = default);

    Task<Result<ProveedoresListaDto>> ListarProveedoresAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<ProveedorDto>> CrearProveedorAsync(Guid consorcioId, GuardarProveedorDto dto, CancellationToken ct = default);
    Task<Result<ProveedorDto>> ActualizarProveedorAsync(Guid consorcioId, Guid id, GuardarProveedorDto dto, CancellationToken ct = default);
    Task<Result> CambiarEstadoProveedorAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default);
    Task<Result> CambiarRecomendadoProveedorAsync(Guid consorcioId, Guid id, bool recomendado, CancellationToken ct = default);

    Task<Result<GastosFijosResumenDto>> ResumenGastosFijosAsync(Guid consorcioId, CancellationToken ct = default);

    Task<Result<EmpleadoDto>> CrearEmpleadoAsync(Guid consorcioId, GuardarEmpleadoDto dto, CancellationToken ct = default);
    Task<Result<EmpleadoDto>> ActualizarEmpleadoAsync(Guid consorcioId, Guid id, GuardarEmpleadoDto dto, CancellationToken ct = default);
    Task<Result> CambiarEstadoEmpleadoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default);

    Task<Result<GastoFijoDto>> CrearGastoFijoAsync(Guid consorcioId, GuardarGastoFijoDto dto, CancellationToken ct = default);
    Task<Result<GastoFijoDto>> ActualizarGastoFijoAsync(Guid consorcioId, Guid id, GuardarGastoFijoDto dto, CancellationToken ct = default);
    Task<Result> CambiarEstadoGastoFijoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default);
}
