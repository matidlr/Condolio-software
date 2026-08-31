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

public record PreferenciasNotificacionDto(
    bool SeguridadApp, bool SeguridadMail,
    bool FinanzasApp, bool FinanzasMail,
    bool ComunidadApp, bool ComunidadMail,
    bool EventosApp, bool EventosMail,
    bool EdificioApp, bool EdificioMail);

public interface INotificacionService
{
    Task<Result<NotificacionListaDto>> ListarAsync(
        Guid consorcioId, bool soloNoLeidas = false, TipoNotificacion? tipo = null, CancellationToken ct = default);

    Task<Result<NotificacionResumenDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default);

    Task<Result> MarcarLeidaAsync(Guid consorcioId, Guid notificacionId, CancellationToken ct = default);

    Task<Result> MarcarTodasLeidasAsync(Guid consorcioId, CancellationToken ct = default);

    /// <summary>Emite una notificación para los administradores del consorcio. No lanza si falla.</summary>
    Task CrearAsync(
        Guid consorcioId, TipoNotificacion tipo, string titulo, string cuerpo,
        string? enlace = null, CancellationToken ct = default);

    // ---- residente ----

    /// <summary>Emite una notificación para un residente, respetando sus preferencias. No lanza si falla.</summary>
    Task CrearParaUsuarioAsync(
        Guid consorcioId, string usuarioId, CategoriaNotificacion categoria, TipoNotificacion tipo,
        string titulo, string cuerpo, string? enlace = null, CancellationToken ct = default);

    /// <summary>Emite una notificación para todos los residentes de un consorcio, respetando preferencias.</summary>
    Task CrearParaResidentesAsync(
        Guid consorcioId, CategoriaNotificacion categoria, TipoNotificacion tipo,
        string titulo, string cuerpo, string? enlace = null, string? excluirUsuarioId = null, CancellationToken ct = default);

    Task<Result<NotificacionListaDto>> ListarUsuarioAsync(string usuarioId, bool soloNoLeidas = false, CancellationToken ct = default);
    Task<Result<NotificacionResumenDto>> ResumenUsuarioAsync(string usuarioId, CancellationToken ct = default);
    Task<Result> MarcarLeidaUsuarioAsync(string usuarioId, Guid notificacionId, CancellationToken ct = default);
    Task<Result> MarcarTodasLeidasUsuarioAsync(string usuarioId, CancellationToken ct = default);

    Task<Result<PreferenciasNotificacionDto>> PreferenciasAsync(string usuarioId, CancellationToken ct = default);
    Task<Result<PreferenciasNotificacionDto>> GuardarPreferenciasAsync(string usuarioId, PreferenciasNotificacionDto dto, CancellationToken ct = default);
}
