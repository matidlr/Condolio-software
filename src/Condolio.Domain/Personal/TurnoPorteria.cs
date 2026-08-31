using Condolio.Domain.Common;

namespace Condolio.Domain.Personal;

/// <summary>Turno de un miembro del staff en una caseta (credencial de dispositivo). Mientras está abierto,
/// las entradas/salidas registradas desde esa caseta se atribuyen a la persona del turno.</summary>
public class TurnoPorteria : Entity
{
    public Guid ConsorcioId { get; set; }

    /// <summary>ApplicationUser de la credencial de caseta desde la que se abrió el turno.</summary>
    public string CredencialUsuarioId { get; set; } = string.Empty;

    public Guid MiembroPersonalId { get; set; }
    public string PersonalNombre { get; set; } = string.Empty;

    public DateTime InicioUtc { get; set; }
    public DateTime? FinUtc { get; set; }

    /// <summary>Nota que deja quien cierra el turno para el siguiente.</summary>
    public string? NotasCierre { get; set; }
}
