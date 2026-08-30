using Condolio.Application.Common;

namespace Condolio.Application.Residentes;

public record EncuestaPendienteDto(Guid Id, string Titulo, int? DiasRestantes);

public record ReservaResumenDto(Guid Id, string Amenidad, DateTime Inicio, string Estado);

public record PortalCasaDto(
    Guid ConsorcioId,
    string ConsorcioNombre,
    string? Localidad,
    string UnidadNombre,
    IReadOnlyList<EncuestaPendienteDto> EncuestasPendientes,
    IReadOnlyList<ReservaResumenDto> ReservasProximas,
    int PaquetesPendientes,
    int NotificacionesNoLeidas);

public interface IMiPortalService
{
    Task<Result<PortalCasaDto>> CasaAsync(string usuarioId, CancellationToken ct = default);
}
