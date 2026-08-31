using Condolio.Domain.Common;

namespace Condolio.Domain.Personal;

public enum TipoPersonal
{
    Seguridad = 0,
    Mantenimiento = 1,
    Limpieza = 2,
    Administracion = 3,
    Jardineria = 4,
    Otro = 5,
    Recepcionista = 6,
    Conserje = 7,
    Piletero = 8,
    Subcontratista = 9,
}

/// <summary>Personal de la comunidad (seguridad, mantenimiento, etc.). Puede tener una cuenta de acceso a la app.</summary>
public class MiembroPersonal : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public TipoPersonal Tipo { get; set; } = TipoPersonal.Seguridad;

    /// <summary>Id del ApplicationUser si tiene cuenta de inicio de sesión (rol Personal).</summary>
    public string? UsuarioId { get; set; }
    public string? Email { get; set; }

    /// <summary>true = credencial de caseta (dispositivo); false = persona del staff.</summary>
    public bool EsDispositivo { get; set; }

    public bool Activo { get; set; } = true;

    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}
