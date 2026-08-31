using Condolio.Domain.Common;

namespace Condolio.Domain.Residentes;

/// <summary>Cargo de un residente dentro de la junta / consejo de administración del consorcio.</summary>
public enum CargoJunta
{
    Presidente = 0,
    Vicepresidente = 1,
    Tesorero = 2,
    Secretario = 3,
    Miembro = 4,
}

/// <summary>Vínculo entre un usuario y un cargo de la junta de un consorcio.</summary>
public class MiembroJunta : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    /// <summary>ApplicationUser del residente.</summary>
    public string UsuarioId { get; set; } = string.Empty;

    public CargoJunta Cargo { get; set; }
}
