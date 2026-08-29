using Condolio.Application.Common;
using Condolio.Domain.Calendario;

namespace Condolio.Application.Calendario;

public record EventoDto(
    Guid Id,
    string Titulo,
    string? Descripcion,
    string? Ubicacion,
    CategoriaEvento Categoria,
    DateTime InicioUtc,
    DateTime FinUtc,
    bool TodoElDia,
    bool NotificoComunidad,
    string CreadoPor);

public record GuardarEventoDto(
    string Titulo,
    string? Descripcion,
    string? Ubicacion,
    CategoriaEvento Categoria,
    DateTime InicioUtc,
    DateTime FinUtc,
    bool TodoElDia,
    bool NotificarComunidad);

public interface IEventoService
{
    Task<Result<IReadOnlyList<EventoDto>>> ListarAsync(Guid consorcioId, DateTime desde, DateTime hasta, CancellationToken ct = default);
    Task<Result<EventoDto>> ObtenerAsync(Guid consorcioId, Guid eventoId, CancellationToken ct = default);
    Task<Result<EventoDto>> CrearAsync(Guid consorcioId, GuardarEventoDto dto, CancellationToken ct = default);
    Task<Result<EventoDto>> ActualizarAsync(Guid consorcioId, Guid eventoId, GuardarEventoDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid eventoId, CancellationToken ct = default);
}
