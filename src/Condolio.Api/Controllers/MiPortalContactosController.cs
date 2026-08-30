using System.Security.Claims;
using Condolio.Application.Contactos;
using Condolio.Application.Residentes;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/mi-portal/contactos")]
[Authorize(Roles = Roles.Residente)]
public class MiPortalContactosController : ApiControllerBase
{
    private readonly IContactoService _contactos;
    private readonly IMiPortalService _portal;

    public MiPortalContactosController(IContactoService contactos, IMiPortalService portal)
    {
        _contactos = contactos;
        _portal = portal;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string Nombre => User.FindFirstValue("nombre_completo") ?? User.FindFirstValue(ClaimTypes.Name) ?? "Residente";

    private async Task<Guid?> ConsorcioAsync(CancellationToken ct)
    {
        var r = await _portal.CasaAsync(Uid, ct);
        return r.Exito ? r.Valor!.ConsorcioId : null;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        if (await ConsorcioAsync(ct) is not { } cid) return NotFound(new { message = "No tenés una unidad asignada." });
        return ToResult(await _contactos.ListarAsync(cid, Uid, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Crear(GuardarContactoDto dto, CancellationToken ct)
    {
        if (await ConsorcioAsync(ct) is not { } cid) return NotFound(new { message = "No tenés una unidad asignada." });
        return ToResult(await _contactos.CrearAsync(cid, Uid, Nombre, dto, ct));
    }

    [HttpPut("{contactoId:guid}")]
    public async Task<IActionResult> Actualizar(Guid contactoId, GuardarContactoDto dto, CancellationToken ct)
    {
        if (await ConsorcioAsync(ct) is not { } cid) return NotFound(new { message = "No tenés una unidad asignada." });
        return ToResult(await _contactos.ActualizarAsync(cid, contactoId, dto, ct));
    }

    [HttpDelete("{contactoId:guid}")]
    public async Task<IActionResult> Eliminar(Guid contactoId, CancellationToken ct)
    {
        if (await ConsorcioAsync(ct) is not { } cid) return NotFound(new { message = "No tenés una unidad asignada." });
        return ToResult(await _contactos.EliminarAsync(cid, contactoId, Uid, ct));
    }
}
