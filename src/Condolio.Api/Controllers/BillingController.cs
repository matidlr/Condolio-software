using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Condolio.Api.Authorization;

namespace Condolio.Api.Controllers;

/// <summary>Suscripción y facturación del administrador.</summary>
[ApiController]
[Route("api/billing")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereAdminGeneral]
public class BillingController : ApiControllerBase
{
    private readonly ISuscripcionService _suscripciones;
    private readonly ITenantContext _tenant;

    public BillingController(ISuscripcionService suscripciones, ITenantContext tenant)
    {
        _suscripciones = suscripciones;
        _tenant = tenant;
    }

    [HttpGet("estado")]
    public async Task<IActionResult> Estado(CancellationToken ct)
    {
        if (_tenant.AdministradorId is not { } adminId)
            return BadRequest(new { message = "Sin administrador asociado." });
        return ToResult(await _suscripciones.ObtenerEstadoAsync(adminId, ct));
    }
}
