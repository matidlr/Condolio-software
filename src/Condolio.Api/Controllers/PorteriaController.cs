using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using System.Security.Claims;
using Condolio.Application.Accesos;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/porteria")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Seguridad)]
public class PorteriaController : ApiControllerBase
{
    private readonly IPaseAccesoService _pases;

    public PorteriaController(IPaseAccesoService pases) => _pases = pases;

    public record VerificarBody(string Token);

    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar(Guid consorcioId, VerificarBody body, CancellationToken ct)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var nombre = User.FindFirstValue("nombre_completo") ?? User.FindFirstValue(ClaimTypes.Name) ?? "Portería";
        return ToResult(await _pases.VerificarAsync(consorcioId, body.Token, uid, nombre, ct));
    }
}
