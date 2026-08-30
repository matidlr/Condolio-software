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

public record VisitaDto(
    Guid Id,
    string VisitanteNombre,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string? Patente,
    DateTime IngresoUtc,
    DateTime? EgresoUtc,
    string RegistradoPor,
    string? Nota);

public record VerificarPaseResultado(
    bool Valido,
    string? Motivo,
    string VisitanteNombre,
    TipoVisita TipoVisita,
    string? Patente,
    string UnidadNombre,
    string ConsorcioNombre,
    int UsosRestantes);

public interface IPaseAccesoService
{
    Task<Result<IReadOnlyList<PaseAccesoDto>>> MisPasesAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<PaseAccesoDto>> ObtenerAsync(string usuarioId, Guid paseId, CancellationToken ct = default);
    Task<Result<PaseAccesoDto>> CrearAsync(string usuarioId, CrearPaseDto dto, CancellationToken ct = default);
    Task<Result> RevocarAsync(string usuarioId, Guid paseId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<VisitaDto>>> MisVisitasAsync(string usuarioId, CancellationToken ct = default);

    /// <summary>Portería: valida un token de QR y registra el ingreso.</summary>
    Task<Result<VerificarPaseResultado>> VerificarAsync(Guid consorcioId, string token, string guardiaUsuarioId, string guardiaNombre, CancellationToken ct = default);
}
