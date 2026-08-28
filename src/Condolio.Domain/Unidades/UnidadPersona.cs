using Condolio.Domain.Common;

namespace Condolio.Domain.Unidades;

public enum RolUnidad
{
    Propietario = 0,
    Inquilino = 1,
    Gestor = 2,
}

/// <summary>Vínculo entre una persona y una unidad, con su rol y acceso opcional al portal.</summary>
public class UnidadPersona : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid UnidadId { get; set; }
    public Unidad Unidad { get; set; } = null!;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }

    public RolUnidad Rol { get; set; } = RolUnidad.Propietario;

    /// <summary>Contacto principal de la unidad para notificaciones y expensas.</summary>
    public bool EsContactoPrincipal { get; set; }

    /// <summary>Usuario Identity si la persona tiene acceso al portal de residentes.</summary>
    public string? UsuarioId { get; set; }
}
