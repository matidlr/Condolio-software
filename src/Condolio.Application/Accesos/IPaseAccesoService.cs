using Condolio.Application.Common;
using Condolio.Domain.Accesos;

namespace Condolio.Application.Accesos;

public record PaseAccesoDto(
    Guid Id,
    TipoPase TipoPase,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string VisitanteNombre,
    string? Patente,
    DateTime FechaEntrada,
    DateTime? ValidoHastaUtc,
    EstadoPase Estado,
    string Token,
    string CreadoPor,
    DateTime CreadoUtc,
    int UsosCount,
    int UsosMax,
    string ConsorcioNombre,
    string UnidadNombre,
    string QrPngBase64);

public record CrearPaseDto(
    TipoPase TipoPase,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string VisitanteNombre,
    string? Patente,
    DateTime FechaEntrada,
    DateTime? ValidoHasta);

public interface IPaseAccesoService
{
    Task<Result<IReadOnlyList<PaseAccesoDto>>> MisPasesAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<PaseAccesoDto>> ObtenerAsync(string usuarioId, Guid paseId, CancellationToken ct = default);
    Task<Result<PaseAccesoDto>> CrearAsync(string usuarioId, CrearPaseDto dto, CancellationToken ct = default);
    Task<Result> RevocarAsync(string usuarioId, Guid paseId, CancellationToken ct = default);
}
