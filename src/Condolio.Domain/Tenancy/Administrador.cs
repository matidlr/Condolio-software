using Condolio.Domain.Billing;
using Condolio.Domain.Common;

namespace Condolio.Domain.Tenancy;

/// <summary>
/// El tenant del SaaS: la persona/empresa que administra uno o más consorcios.
/// Todo lo del application plane cuelga de acá vía <see cref="ITenantOwned.AdministradorId"/>.
/// </summary>
public class Administrador : Entity
{
    public string RazonSocial { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Cuit { get; set; }

    /// <summary>Usuario Identity dueño de la cuenta (rol Administrador).</summary>
    public string UsuarioId { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public Suscripcion? Suscripcion { get; set; }
}
