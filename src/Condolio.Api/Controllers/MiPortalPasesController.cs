using System.Security.Claims;
using Condolio.Application.Accesos;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-portal/pases")]
[Authorize(Roles = Roles.Residente)]
public class MiPortalPasesController : ApiControllerBase
{
    private readonly IPaseAccesoService _pases;

    public MiPortalPasesController(IPaseAccesoService pases) => _pases = pases;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Mis(CancellationToken ct) =>
        ToResult(await _pases.MisPasesAsync(Uid, ct));

    [HttpGet("{paseId:guid}")]
    public async Task<IActionResult> Obtener(Guid paseId, CancellationToken ct) =>
        ToResult(await _pases.ObtenerAsync(Uid, paseId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(CrearPaseDto dto, CancellationToken ct) =>
        ToResult(await _pases.CrearAsync(Uid, dto, ct));

    [HttpDelete("{paseId:guid}")]
    public async Task<IActionResult> Revocar(Guid paseId, CancellationToken ct) =>
        ToResult(await _pases.RevocarAsync(Uid, paseId, ct));

    [HttpGet("~/api/mi-portal/visitas")]
    public async Task<IActionResult> MisVisitas(CancellationToken ct) =>
        ToResult(await _pases.MisVisitasAsync(Uid, ct));
}
