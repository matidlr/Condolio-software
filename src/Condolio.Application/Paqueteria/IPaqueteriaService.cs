using Condolio.Application.Common;
using Condolio.Domain.Paqueteria;

namespace Condolio.Application.Paqueteria;

public record PaqueteDto(
    Guid Id,
    Guid UnidadId,
    string UnidadNombre,
    TipoPaquete Tipo,
    int Cantidad,
    string? Transportista,
    string? Descripcion,
    EstadoPaquete Estado,
    DateTime LlegadaUtc,
    DateTime? EntregaUtc,
    string RegistradoPorNombre,
    string? EntregadoPorNombre,
    string? RetiradoPorNombre);

public record ResumenPaqueteriaDto(
    int PorEntregar, int LlegaronHoy, int EntregadosHoy, int Total, int NecesitanAtencion);

public record PaquetesListaDto(IReadOnlyList<PaqueteDto> Paquetes);

public record PaqueteDetalleDto(PaqueteDto Paquete, string Referencia, IReadOnlyList<string> Residentes);

public record RegistrarPaqueteDto(
    Guid UnidadId,
    TipoPaquete Tipo,
    int Cantidad,
    string? Transportista,
    string? Descripcion,
    DateTime? LlegadaLocal);

public record EntregarPaqueteDto(string? RetiradoPor);

public interface IPaqueteriaService
{
    Task<Result<ResumenPaqueteriaDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<PaquetesListaDto>> ListarAsync(Guid consorcioId, EstadoPaquete? estado, string? busqueda, int anio, int mes, CancellationToken ct = default);
    Task<Result<PaqueteDto>> ObtenerAsync(Guid consorcioId, Guid id, CancellationToken ct = default);
    Task<Result<PaqueteDetalleDto>> ObtenerDetalleAsync(Guid consorcioId, Guid id, CancellationToken ct = default);
    Task<Result<PaqueteDto>> RegistrarAsync(Guid consorcioId, RegistrarPaqueteDto dto, string registradoPor, CancellationToken ct = default);
    Task<Result<PaqueteDto>> EntregarAsync(Guid consorcioId, Guid id, EntregarPaqueteDto dto, string entregadoPor, CancellationToken ct = default);
    Task<Result> EnviarRecordatorioAsync(Guid consorcioId, Guid id, CancellationToken ct = default);
}
