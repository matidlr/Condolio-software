using Condolio.Application.Residentes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/invitaciones")]
[AllowAnonymous]
public class InvitacionesPublicasController : ApiControllerBase
{
    private readonly IInvitacionPublicaService _invitaciones;

    public InvitacionesPublicasController(IInvitacionPublicaService invitaciones) => _invitaciones = invitaciones;

    [HttpGet("{token}")]
    public async Task<IActionResult> Ver(string token, CancellationToken ct) =>
        ToResult(await _invitaciones.VerAsync(token, ct));

    [HttpPost("{token}/aceptar")]
    public async Task<IActionResult> Aceptar(string token, AceptarInvitacionDto dto, CancellationToken ct) =>
        ToResult(await _invitaciones.AceptarAsync(token, dto, ct));
}
