using Condolio.Application.Common;
using Condolio.Domain.Notificaciones;

namespace Condolio.Application.Notificaciones;

public record NotificacionDto(
    Guid Id,
    TipoNotificacion Tipo,
    string Titulo,
    string Cuerpo,
    string? Enlace,
    bool Leida,
    DateTime CreadoUtc);

public record NotificacionResumenDto(int Total, int NoLeidas);

public record NotificacionListaDto(
    IReadOnlyList<NotificacionDto> Notificaciones,
    int Total,
    int NoLeidas);

public interface INotificacionService
{
    Task<Result<NotificacionListaDto>> ListarAsync(
        Guid consorcioId, bool soloNoLeidas = false, TipoNotificacion? tipo = null, CancellationToken ct = default);

    Task<Result<NotificacionResumenDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default);

    Task<Result> MarcarLeidaAsync(Guid consorcioId, Guid notificacionId, CancellationToken ct = default);

    Task<Result> MarcarTodasLeidasAsync(Guid consorcioId, CancellationToken ct = default);

    /// <summary>Emite una notificación para el consorcio. No lanza si falla.</summary>
    Task CrearAsync(
        Guid consorcioId, TipoNotificacion tipo, string titulo, string cuerpo,
        string? enlace = null, CancellationToken ct = default);
}
