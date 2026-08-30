using Condolio.Application.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Application.Residentes;

public record MiUnidadDto(
    Guid UnidadId,
    string UnidadNombre,
    Guid ConsorcioId,
    string ConsorcioNombre,
    RolUnidad Rol,
    bool EsContactoPrincipal,
    decimal? CuotaMantenimiento,
    decimal Saldo);

public record MiPanelDto(string Nombre, IReadOnlyList<MiUnidadDto> Unidades);

public interface IVistaResidenteService
{
    Task<Result<MiPanelDto>> MiPanelAsync(string usuarioId, string nombre, CancellationToken ct = default);
}
