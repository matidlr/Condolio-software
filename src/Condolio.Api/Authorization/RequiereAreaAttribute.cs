using System.Security.Claims;
using Condolio.Application.Common;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Condolio.Api.Authorization;

/// <summary>
/// Exige que el administrador tenga acceso a un área (o sea general / SuperAdmin).
/// No reemplaza a <c>[Authorize]</c>: se combina con él.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiereAreaAttribute : Attribute, IAuthorizationFilter
{
    private readonly AreaAdmin _area;

    public RequiereAreaAttribute(AreaAdmin area) => _area = area;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) return;               // lo maneja [Authorize]
        if (user.IsInRole(Roles.SuperAdmin)) return;
        if (!user.IsInRole(Roles.Administrador)) return;                   // no es admin: otra ruta

        var general = user.FindFirstValue(CondolioClaims.AdminGeneral);
        if (general is null or "true") return;                            // sin claim = legacy = general

        var areas = (user.FindFirstValue(CondolioClaims.AdminAreas) ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!areas.Contains(_area.ToString(), StringComparer.OrdinalIgnoreCase))
            context.Result = new ObjectResult(new { message = "Tu cuenta no tiene acceso a esta sección." })
            { StatusCode = StatusCodes.Status403Forbidden };
    }
}

/// <summary>Exige que el administrador tenga acceso general (config, administradores, facturación…).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiereAdminGeneralAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) return;
        if (user.IsInRole(Roles.SuperAdmin)) return;
        if (!user.IsInRole(Roles.Administrador)) return;

        var general = user.FindFirstValue(CondolioClaims.AdminGeneral);
        if (general == "false")
            context.Result = new ObjectResult(new { message = "Solo un administrador general puede hacer esto." })
            { StatusCode = StatusCodes.Status403Forbidden };
    }
}
