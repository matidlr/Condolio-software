using System.Security.Claims;
using Condolio.Application.Common;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;

namespace Condolio.Infrastructure.Tenancy;

/// <summary>Resuelve el tenant de la request desde los claims del JWT.</summary>
public class HttpTenantContext : ITenantContext
{
    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) return;

        UsuarioId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        EsSuperAdmin = user.IsInRole(Roles.SuperAdmin);

        if (Guid.TryParse(user.FindFirstValue(CondolioClaims.TenantId), out var tenantId))
            AdministradorId = tenantId;
    }

    public Guid? AdministradorId { get; }
    public bool EsSuperAdmin { get; }
    public string? UsuarioId { get; }
}
