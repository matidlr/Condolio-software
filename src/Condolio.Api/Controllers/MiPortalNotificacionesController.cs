using System.Security.Claims;
using Condolio.Application.Notificaciones;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-portal/notificaciones")]
[Authorize(Roles = Roles.Residente)]
public class MiPortalNotificacionesController : ApiControllerBase
{
    private readonly INotificacionService _notificaciones;

    public MiPortalNotificacionesController(INotificacionService notificaciones) => _notificaciones = notificaciones;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool soloNoLeidas, CancellationToken ct) =>
        ToResult(await _notificaciones.ListarUsuarioAsync(Uid, soloNoLeidas, ct));

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct) =>
        ToResult(await _notificaciones.ResumenUsuarioAsync(Uid, ct));

    [HttpPost("{id:guid}/leida")]
    public async Task<IActionResult> Leida(Guid id, CancellationToken ct) =>
        ToResult(await _notificaciones.MarcarLeidaUsuarioAsync(Uid, id, ct));

    [HttpPost("leidas")]
    public async Task<IActionResult> TodasLeidas(CancellationToken ct) =>
        ToResult(await _notificaciones.MarcarTodasLeidasUsuarioAsync(Uid, ct));

    [HttpGet("preferencias")]
    public async Task<IActionResult> Preferencias(CancellationToken ct) =>
        ToResult(await _notificaciones.PreferenciasAsync(Uid, ct));

    [HttpPut("preferencias")]
    public async Task<IActionResult> GuardarPreferencias(PreferenciasNotificacionDto dto, CancellationToken ct) =>
        ToResult(await _notificaciones.GuardarPreferenciasAsync(Uid, dto, ct));
}
