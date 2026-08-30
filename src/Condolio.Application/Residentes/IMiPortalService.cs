using Condolio.Application.Amenidades;
using Condolio.Application.Common;

namespace Condolio.Application.Residentes;

public record CalendarioItemDto(
    Guid Id,
    string Titulo,
    string? Descripcion,
    string? Ubicacion,
    DateTime Inicio,
    DateTime Fin,
    bool TodoElDia,
    string Tipo,
    string? Categoria);

public record CrearEventoResidenteDto(
    string Titulo,
    string? Descripcion,
    string? Ubicacion,
    string Categoria,
    DateTime Inicio,
    DateTime Fin,
    bool TodoElDia);

public record EncuestaPendienteDto(Guid Id, string Titulo, int? DiasRestantes);

public record ReservaResumenDto(Guid Id, string Amenidad, DateTime Inicio, DateTime Fin, string Estado);

public record PortalCasaDto(
    Guid ConsorcioId,
    string ConsorcioNombre,
    string? Localidad,
    string UnidadNombre,
    IReadOnlyList<EncuestaPendienteDto> EncuestasPendientes,
    IReadOnlyList<ReservaResumenDto> ReservasProximas,
    int PaquetesPendientes,
    int NotificacionesNoLeidas);

public record SlotDto(DateTime Inicio, DateTime Fin);

public record MiReservaDto(
    Guid Id,
    Guid AmenidadId,
    string Amenidad,
    IReadOnlyList<Guid> ImagenesIds,
    DateTime Inicio,
    DateTime Fin,
    string Estado,
    string? Nota,
    DateTime CreadoUtc);

public record MisReservasDto(IReadOnlyList<MiReservaDto> Activas, IReadOnlyList<MiReservaDto> Previas);

public interface IMiPortalService
{
    Task<Result<PortalCasaDto>> CasaAsync(string usuarioId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<AmenidadDto>>> AmenidadesAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<AmenidadDto>> AmenidadAsync(string usuarioId, Guid amenidadId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SlotDto>>> SlotsAsync(string usuarioId, Guid amenidadId, DateOnly fecha, CancellationToken ct = default);
    Task<Result<MiReservaDto>> SolicitarReservaAsync(string usuarioId, Guid amenidadId, DateTime inicio, DateTime fin, string? nota, CancellationToken ct = default);
    Task<Result<MisReservasDto>> MisReservasAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<MiReservaDto>> MiReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default);
    Task<Result> CancelarReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CalendarioItemDto>>> CalendarioAsync(string usuarioId, DateTime desde, DateTime hasta, CancellationToken ct = default);
    Task<Result<CalendarioItemDto>> CrearEventoAsync(string usuarioId, CrearEventoResidenteDto dto, CancellationToken ct = default);
}
