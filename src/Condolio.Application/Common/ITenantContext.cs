namespace Condolio.Application.Common;

/// <summary>
/// Contexto del tenant activo para la request actual (scoped). Se resuelve del claim
/// <c>tenantId</c> del JWT. El <see cref="CondolioDbContext"/> lo usa en el query filter global.
/// </summary>
public interface ITenantContext
{
    /// <summary>Administrador (tenant) actual, o null para SuperAdmin / requests sin tenant.</summary>
    Guid? AdministradorId { get; }

    /// <summary>True cuando el usuario autenticado es SuperAdmin (ve todos los tenants).</summary>
    bool EsSuperAdmin { get; }

    string? UsuarioId { get; }
}
