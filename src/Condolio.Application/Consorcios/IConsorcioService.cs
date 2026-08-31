using Condolio.Application.Common;
using Condolio.Domain.Consorcios;

namespace Condolio.Application.Consorcios;

public record ConsorcioDto(
    Guid Id,
    string Nombre,
    TipoConsorcio Tipo,
    string Direccion,
    string? Localidad,
    string? Provincia,
    string Pais,
    int CantidadUnidades);

public record CrearConsorcioDto(
    string Nombre,
    string Direccion,
    string? Localidad,
    string? Provincia,
    string? Pais,
    string? Cuit,
    TipoConsorcio Tipo = TipoConsorcio.EdificioResidencial,
    string? CodigoPostal = null,
    double? Latitud = null,
    double? Longitud = null);

public record ActualizarConsorcioDto(
    string Nombre,
    TipoConsorcio Tipo,
    string Direccion,
    string? Localidad,
    string? Provincia,
    string? Pais,
    string? CodigoPostal,
    string? Cuit,
    double? Latitud,
    double? Longitud);

public record ConsorcioDetalleDto(
    Guid Id,
    string Nombre,
    TipoConsorcio Tipo,
    string Direccion,
    string? Localidad,
    string? Provincia,
    string Pais,
    string? CodigoPostal,
    string? Cuit,
    double? Latitud,
    double? Longitud,
    int CantidadUnidades);

public interface IConsorcioService
{
    Task<IReadOnlyList<ConsorcioDto>> ListarAsync(CancellationToken ct = default);
    Task<Result<ConsorcioDto>> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<Result<ConsorcioDetalleDto>> DetalleAsync(Guid id, CancellationToken ct = default);
    Task<Result<ConsorcioDto>> CrearAsync(CrearConsorcioDto dto, CancellationToken ct = default);
    Task<Result<ConsorcioDetalleDto>> ActualizarAsync(Guid id, ActualizarConsorcioDto dto, CancellationToken ct = default);

    /// <summary>Elimina un consorcio. Solo se permite si no tiene unidades ni residentes cargados.</summary>
    Task<Result> EliminarAsync(Guid id, CancellationToken ct = default);
}
