using System.Security.Claims;
using Condolio.Application.Accesos;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/seguridad")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class SeguridadController : ApiControllerBase
{
    private readonly IAccesoAdminService _accesos;

    public SeguridadController(IAccesoAdminService accesos) => _accesos = accesos;

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string Nombre => User.FindFirstValue("nombre_completo")
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? User.FindFirstValue(ClaimTypes.Email)
        ?? "Administración";

    // ---- QR / Pases ----

    [HttpGet("pases")]
    public async Task<IActionResult> Pases(
        Guid consorcioId, [FromQuery] int anio, [FromQuery] int mes, [FromQuery] string? q, CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        return ToResult(await _accesos.ListarPasesAsync(
            consorcioId, anio == 0 ? ahora.Year : anio, mes == 0 ? ahora.Month : mes, q, ct));
    }

    [HttpGet("pases/{paseId:guid}")]
    public async Task<IActionResult> Pase(Guid consorcioId, Guid paseId, CancellationToken ct) =>
        ToResult(await _accesos.ObtenerPaseAsync(consorcioId, paseId, ct));

    [HttpPost("pases")]
    public async Task<IActionResult> CrearPase(Guid consorcioId, CrearPaseAdminDto dto, CancellationToken ct) =>
        ToResult(await _accesos.CrearPaseAsync(consorcioId, Uid, Nombre, dto, ct));

    [HttpPost("pases/{paseId:guid}/revocar")]
    public async Task<IActionResult> RevocarPase(Guid consorcioId, Guid paseId, CancellationToken ct) =>
        ToResult(await _accesos.RevocarPaseAsync(consorcioId, paseId, ct));

    // ---- Bitácora ----

    [HttpGet("bitacora")]
    public async Task<IActionResult> Bitacora(
        Guid consorcioId, [FromQuery] DateOnly? fecha, [FromQuery] int dias,
        [FromQuery] string? filtro, [FromQuery] string? q, CancellationToken ct) =>
        ToResult(await _accesos.BitacoraAsync(
            consorcioId, fecha ?? DateOnly.FromDateTime(DateTime.UtcNow), dias <= 0 ? 1 : dias, filtro, q, ct));

    [HttpPost("bitacora/{registroId:guid}/egreso")]
    public async Task<IActionResult> Egreso(Guid consorcioId, Guid registroId, CancellationToken ct) =>
        ToResult(await _accesos.RegistrarEgresoAsync(consorcioId, registroId, ct));
}
