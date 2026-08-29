using Condolio.Application.Common;

namespace Condolio.Application.Panel;

public record PanelResumenDto(
    string ConsorcioNombre,
    int Unidades,
    int Residentes,
    int ResidentesActivos,
    int PublicacionesMes,
    int TicketsAbiertos,
    int ReservasSemana,
    int PaquetesPendientes,
    decimal PagosMes,
    int EntradasHoy,
    int CodigosQrActivos);

public interface IPanelService
{
    Task<Result<PanelResumenDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default);
}
