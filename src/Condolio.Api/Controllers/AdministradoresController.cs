using System.Security.Claims;
using Condolio.Application.Common;
using Condolio.Application.Tenancy;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Condolio.Api.Authorization;

namespace Condolio.Api.Controllers;

/// <summary>Equipo de administradores de la cuenta (tenant) y sus permisos.</summary>
[ApiController]
[Route("api/administradores")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereAdminGeneral]
public class AdministradoresController : ApiControllerBase
{
    private readonly IAdminMiembroService _miembros;
    private readonly ITenantContext _tenant;

    public AdministradoresController(IAdminMiembroService miembros, ITenantContext tenant)
    {
        _miembros = miembros;
        _tenant = tenant;
    }

    private bool TieneTenant(out Guid id)
    {
        id = _tenant.AdministradorId ?? Guid.Empty;
        return id != Guid.Empty;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        TieneTenant(out var id)
            ? ToResult(await _miembros.ListarAsync(id, ct))
            : BadRequest(new { message = "Sin cuenta de administrador." });

    [HttpPost]
    public async Task<IActionResult> Agregar(AgregarAdminDto dto, CancellationToken ct) =>
        TieneTenant(out var id)
            ? ToResult(await _miembros.AgregarAsync(id, Uid, dto, ct))
            : BadRequest(new { message = "Sin cuenta de administrador." });

    [HttpPut("{usuarioId}/rol")]
    public async Task<IActionResult> CambiarRol(string usuarioId, GuardarRolAdminDto dto, CancellationToken ct) =>
        TieneTenant(out var id)
            ? ToResult(await _miembros.CambiarRolAsync(id, usuarioId, dto, ct))
            : BadRequest(new { message = "Sin cuenta de administrador." });

    [HttpDelete("{*id}")]
    public async Task<IActionResult> Quitar(string id, CancellationToken ct) =>
        TieneTenant(out var tenantId)
            ? ToResult(await _miembros.QuitarAsync(tenantId, id, ct))
            : BadRequest(new { message = "Sin cuenta de administrador." });
}
