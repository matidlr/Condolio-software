using Condolio.Application.Panel;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/panel")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class PanelController : ApiControllerBase
{
    private readonly IPanelService _panel;

    public PanelController(IPanelService panel) => _panel = panel;

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _panel.ResumenAsync(consorcioId, ct));
}
