using Condolio.Application.Common;
using Condolio.Domain.Expensas;

namespace Condolio.Application.Expensas;

public record AbrirPeriodoDto(int Anio, int Mes);

public record PeriodoResumenDto(
    Guid Id, int Anio, int Mes, EstadoPeriodo Estado,
    DateOnly? FechaLiquidacion, int CantidadGastos, decimal TotalGastos);

public record TotalRubroDto(Guid RubroGastoId, string Rubro, TipoRubro Tipo, decimal Total);

public record GastoPeriodoDto(
    Guid Id, Guid RubroGastoId, string RubroNombre, TipoRubro TipoRubro,
    Guid? ProveedorId, string? ProveedorNombre,
    string Descripcion, decimal Monto, DateOnly Fecha,
    string? MetodoPago, string? CuentaPago, bool TieneComprobante,
    OrigenGasto Origen, AlcanceGasto Alcance, CriterioDistribucion CriterioDistribucion,
    Guid? ExtraordinariaId, string? ExtraordinariaTitulo,
    IReadOnlyList<Guid> UnidadIds);

public record PeriodoDetalleDto(
    Guid Id, int Anio, int Mes, EstadoPeriodo Estado,
    DateOnly? FechaLiquidacion, string? Notas,
    IReadOnlyList<GastoPeriodoDto> Gastos,
    IReadOnlyList<TotalRubroDto> PorRubro,
    decimal TotalOrdinario, decimal TotalExtraordinario, decimal TotalFondoReserva,
    decimal TotalImputadoAExtraordinarias, decimal TotalAPrratear);

public record GuardarGastoPeriodoDto(
    Guid RubroGastoId, Guid? ProveedorId, string Descripcion, decimal Monto, DateOnly Fecha,
    string? MetodoPago, string? CuentaPago,
    AlcanceGasto Alcance, CriterioDistribucion CriterioDistribucion,
    Guid? ExtraordinariaId, IReadOnlyList<Guid>? UnidadIds);

public interface IPeriodosExpensasService
{
    Task<Result<IReadOnlyList<PeriodoResumenDto>>> ListarAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<PeriodoDetalleDto>> AbrirAsync(Guid consorcioId, AbrirPeriodoDto dto, CancellationToken ct = default);
    Task<Result<PeriodoDetalleDto>> ObtenerAsync(Guid consorcioId, Guid periodoId, CancellationToken ct = default);
    Task<Result> ReabrirAsync(Guid consorcioId, Guid periodoId, CancellationToken ct = default);

    Task<Result<GastoPeriodoDto>> CrearGastoAsync(Guid consorcioId, Guid periodoId, GuardarGastoPeriodoDto dto, CancellationToken ct = default);
    Task<Result<GastoPeriodoDto>> ActualizarGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, GuardarGastoPeriodoDto dto, CancellationToken ct = default);
    Task<Result> EliminarGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, CancellationToken ct = default);

    Task<Result> GuardarComprobanteGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, Stream contenido, string extension, CancellationToken ct = default);
    Task<Result<(Stream Contenido, string ContentType)>> AbrirComprobanteGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, CancellationToken ct = default);
}
