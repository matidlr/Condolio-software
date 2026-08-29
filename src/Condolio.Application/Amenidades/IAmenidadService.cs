using Condolio.Application.Common;

namespace Condolio.Application.Amenidades;

public record AmenidadHorarioDto(int Dia, bool Cerrado, int AbreMin, int CierraMin);

public record AmenidadDto(
    Guid Id,
    string Nombre,
    string? Descripcion,
    IReadOnlyList<Guid> ImagenesIds,
    bool Reservable,
    int IntervaloMinutos,
    bool LimiteMensual,
    int MaxReservasPorUnidad,
    bool TieneCosto,
    decimal? Tarifa,
    bool RequiereAprobacion,
    DateOnly? ReservableDesde,
    IReadOnlyList<string> DiasBloqueados,
    string? MensajeReserva,
    IReadOnlyList<AmenidadHorarioDto> Horarios,
    int ReservasProximas,
    DateTime CreadoUtc,
    DateTime? ActualizadoUtc);

public record AmenidadListaDto(
    IReadOnlyList<AmenidadDto> Amenidades,
    int Total,
    int Reservables,
    int ReservacionesEsteMes,
    int AprobacionesPendientes,
    decimal IngresosGenerados);

public record GuardarAmenidadDto(
    string Nombre,
    string? Descripcion,
    List<Guid>? ImagenesIds,
    bool Reservable,
    int IntervaloMinutos,
    bool LimiteMensual,
    int MaxReservasPorUnidad,
    bool TieneCosto,
    decimal? Tarifa,
    bool RequiereAprobacion,
    DateOnly? ReservableDesde,
    List<string>? DiasBloqueados,
    string? MensajeReserva,
    List<AmenidadHorarioDto>? Horarios);

public interface IAmenidadService
{
    Task<Result<AmenidadListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<AmenidadDto>> ObtenerAsync(Guid consorcioId, Guid amenidadId, CancellationToken ct = default);
    Task<Result<AmenidadDto>> CrearAsync(Guid consorcioId, GuardarAmenidadDto dto, CancellationToken ct = default);
    Task<Result<AmenidadDto>> ActualizarAsync(Guid consorcioId, Guid amenidadId, GuardarAmenidadDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid amenidadId, CancellationToken ct = default);
}
