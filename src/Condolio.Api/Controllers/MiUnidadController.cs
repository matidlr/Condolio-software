using System.Security.Claims;
using Condolio.Application.Residentes;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-unidad")]
[Authorize(Roles = Roles.Residente)]
public class MiUnidadController : ApiControllerBase
{
    private readonly IVistaResidenteService _vista;
    private readonly IMiPortalService _portal;

    public MiUnidadController(IVistaResidenteService vista, IMiPortalService portal)
    {
        _vista = vista;
        _portal = portal;
    }

    [HttpGet]
    public async Task<IActionResult> MiPanel(CancellationToken ct)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var nombre = User.FindFirstValue(ClaimTypes.Email) ?? "";
        return ToResult(await _vista.MiPanelAsync(uid, nombre, ct));
    }

    [HttpGet("~/api/mi-portal/casa")]
    public async Task<IActionResult> Casa(CancellationToken ct)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        return ToResult(await _portal.CasaAsync(uid, ct));
    }
}
