using Condolio.Application.Common;
using Condolio.Domain.Amenidades;

namespace Condolio.Application.Amenidades;

public record ReservaDto(
    Guid Id,
    Guid AmenidadId,
    string AmenidadNombre,
    Guid? UnidadId,
    string? UnidadNombre,
    string Solicitante,
    DateTime Inicio,
    DateTime Fin,
    EstadoReserva Estado,
    decimal? Importe,
    string? Nota,
    DateTime CreadoUtc);

public record ReservaListaDto(
    IReadOnlyList<ReservaDto> Reservas,
    int Pendientes,
    int Aprobadas,
    int Rechazadas,
    int HoyConfirmadas);

public record CrearReservaDto(
    Guid AmenidadId,
    Guid? UnidadId,
    DateTime Inicio,
    DateTime Fin,
    string? Nota);

public interface IReservaService
{
    Task<Result<ReservaListaDto>> ListarAsync(Guid consorcioId, DateTime desde, DateTime hasta, CancellationToken ct = default);
    Task<Result<ReservaDto>> CrearAsync(Guid consorcioId, CrearReservaDto dto, CancellationToken ct = default);
    Task<Result<ReservaDto>> CambiarEstadoAsync(Guid consorcioId, Guid reservaId, EstadoReserva estado, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid reservaId, CancellationToken ct = default);
}
