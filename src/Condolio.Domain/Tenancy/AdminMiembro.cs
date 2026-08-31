using Condolio.Domain.Common;

namespace Condolio.Domain.Tenancy;

/// <summary>Áreas del panel a las que un administrador con acceso limitado puede entrar.</summary>
public enum AreaAdmin
{
    Finanzas = 0,
    Operacion = 1,
    Seguridad = 2,
    Comunicacion = 3,
    Residentes = 4,
}

/// <summary>
/// Miembro del equipo de un <see cref="Administrador"/> (tenant): el dueño y los co-administradores
/// que invita, con su nivel de acceso.
/// </summary>
public class AdminMiembro : Entity
{
    public Guid AdministradorId { get; set; }

    /// <summary>ApplicationUser del co-administrador.</summary>
    public string UsuarioId { get; set; } = string.Empty;

    /// <summary>true = administrador general (acceso completo). false = acceso limitado a <see cref="AreasCsv"/>.</summary>
    public bool EsGeneral { get; set; } = true;

    /// <summary>Áreas permitidas cuando <see cref="EsGeneral"/> es false, como CSV de <see cref="AreaAdmin"/>.</summary>
    public string AreasCsv { get; set; } = string.Empty;

    /// <summary>El dueño original del tenant: no se puede quitar ni bajar de general.</summary>
    public bool EsDueno { get; set; }
}
