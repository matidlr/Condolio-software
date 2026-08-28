using Condolio.Application.Common;

namespace Condolio.Application.Consorcios;

public record ConsorcioDto(
    Guid Id,
    string Nombre,
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
    string? Cuit);

public interface IConsorcioService
{
    Task<IReadOnlyList<ConsorcioDto>> ListarAsync(CancellationToken ct = default);
    Task<Result<ConsorcioDto>> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<Result<ConsorcioDto>> CrearAsync(CrearConsorcioDto dto, CancellationToken ct = default);
}
