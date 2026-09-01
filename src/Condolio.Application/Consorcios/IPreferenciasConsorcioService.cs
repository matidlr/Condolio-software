using Condolio.Application.Common;

namespace Condolio.Application.Consorcios;

public record PreferenciasConsorcioDto(
    bool ResidentesPublican,
    bool ComentariosHabilitados,
    bool AnunciosSiemprePorCorreo);

public interface IPreferenciasConsorcioService
{
    Task<Result<PreferenciasConsorcioDto>> ObtenerAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<PreferenciasConsorcioDto>> GuardarAsync(Guid consorcioId, PreferenciasConsorcioDto dto, CancellationToken ct = default);
}
