using Condolio.Application.Residentes;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/residentes")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class ResidentesController : ApiControllerBase
{
    private readonly IResidenteService _residentes;

    public ResidentesController(IResidenteService residentes) => _residentes = residentes;

    [HttpGet]
    public async Task<IActionResult> Directorio(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _residentes.DirectorioAsync(consorcioId, ct));

    [HttpGet("invitaciones")]
    public async Task<IActionResult> Invitaciones(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _residentes.InvitacionesAsync(consorcioId, ct));

    public record InvitarLoteBody(IReadOnlyList<InvitarLoteItem> Items, bool Notificar = false);

    [HttpPost("invitaciones")]
    public async Task<IActionResult> Invitar(Guid consorcioId, CrearInvitacionDto dto, CancellationToken ct) =>
        ToResult(await _residentes.InvitarAsync(consorcioId, dto, ct));

    [HttpPost("invitaciones/lote")]
    public async Task<IActionResult> InvitarLote(Guid consorcioId, InvitarLoteBody body, CancellationToken ct) =>
        ToResult(await _residentes.InvitarLoteAsync(consorcioId, body.Items, body.Notificar, ct));

    [HttpPut("invitaciones/{invitacionId:guid}")]
    public async Task<IActionResult> EditarInvitacion(Guid consorcioId, Guid invitacionId, CrearInvitacionDto dto, CancellationToken ct) =>
        ToResult(await _residentes.EditarInvitacionAsync(consorcioId, invitacionId, dto, ct));

    [HttpPost("invitaciones/{invitacionId:guid}/reenviar")]
    public async Task<IActionResult> Reenviar(Guid consorcioId, Guid invitacionId, CancellationToken ct) =>
        ToResult(await _residentes.ReenviarInvitacionAsync(consorcioId, invitacionId, ct));

    [HttpPost("invitaciones/reenviar-pendientes")]
    public async Task<IActionResult> ReenviarPendientes(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _residentes.ReenviarPendientesAsync(consorcioId, ct));

    [HttpGet("por-asignar")]
    public async Task<IActionResult> PorAsignar(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _residentes.PorAsignarAsync(consorcioId, ct));

    [HttpDelete("invitaciones/{invitacionId:guid}")]
    public async Task<IActionResult> Cancelar(Guid consorcioId, Guid invitacionId, CancellationToken ct) =>
        ToResult(await _residentes.CancelarInvitacionAsync(consorcioId, invitacionId, ct));
}
