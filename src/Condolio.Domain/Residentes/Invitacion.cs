using Condolio.Domain.Common;
using Condolio.Domain.Consorcios;
using Condolio.Domain.Unidades;

namespace Condolio.Domain.Residentes;

public enum EstadoInvitacion
{
    Pendiente = 0,
    Aceptada = 1,
    Expirada = 2,
    Cancelada = 3,
}

/// <summary>Invitación por email para que un residente se sume al portal de la comunidad.</summary>
public class Invitacion : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }
    public Consorcio Consorcio { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
    public string? Nombre { get; set; }

    /// <summary>Unidad y rol asignados (opcionales; se pueden completar después).</summary>
    public Guid? UnidadId { get; set; }
    public RolUnidad Rol { get; set; } = RolUnidad.Propietario;

    public string Token { get; set; } = Guid.NewGuid().ToString("N");
    public EstadoInvitacion Estado { get; set; } = EstadoInvitacion.Pendiente;
    public DateTime ExpiraUtc { get; set; } = DateTime.UtcNow.AddDays(14);

    public string InvitadoPorUsuarioId { get; set; } = string.Empty;
}
