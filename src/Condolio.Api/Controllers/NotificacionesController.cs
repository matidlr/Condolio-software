using Condolio.Application.Notificaciones;
using Condolio.Domain.Notificaciones;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/notificaciones")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class NotificacionesController : ApiControllerBase
{
    private readonly INotificacionService _notificaciones;

    public NotificacionesController(INotificacionService notificaciones) => _notificaciones = notificaciones;

    [HttpGet]
    public async Task<IActionResult> Listar(
        Guid consorcioId,
        [FromQuery] bool soloNoLeidas,
        [FromQuery] TipoNotificacion? tipo,
        CancellationToken ct) =>
        ToResult(await _notificaciones.ListarAsync(consorcioId, soloNoLeidas, tipo, ct));

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _notificaciones.ResumenAsync(consorcioId, ct));

    [HttpPost("{notificacionId:guid}/leida")]
    public async Task<IActionResult> MarcarLeida(Guid consorcioId, Guid notificacionId, CancellationToken ct) =>
        ToResult(await _notificaciones.MarcarLeidaAsync(consorcioId, notificacionId, ct));

    [HttpPost("leidas")]
    public async Task<IActionResult> MarcarTodasLeidas(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _notificaciones.MarcarTodasLeidasAsync(consorcioId, ct));

    [HttpPost("{notificacionId:guid}/alternar-leida")]
    public async Task<IActionResult> AlternarLeida(Guid consorcioId, Guid notificacionId, CancellationToken ct) =>
        ToResult(await _notificaciones.AlternarLeidaAsync(consorcioId, notificacionId, ct));

    [HttpPost("{notificacionId:guid}/fijar")]
    public async Task<IActionResult> AlternarFijada(Guid consorcioId, Guid notificacionId, CancellationToken ct) =>
        ToResult(await _notificaciones.AlternarFijadaAsync(consorcioId, notificacionId, ct));
}
