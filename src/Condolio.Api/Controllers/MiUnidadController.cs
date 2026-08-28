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

    public MiUnidadController(IVistaResidenteService vista) => _vista = vista;

    [HttpGet]
    public async Task<IActionResult> MiPanel(CancellationToken ct)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        var nombre = User.FindFirstValue(ClaimTypes.Email) ?? "";
        return ToResult(await _vista.MiPanelAsync(uid, nombre, ct));
    }
}
