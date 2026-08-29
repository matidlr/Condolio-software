using Condolio.Domain.Common;

namespace Condolio.Domain.Notificaciones;

public enum TipoNotificacion
{
    General = 0,
    InvitacionEnviada = 1,
    MiembroSeUnio = 2,
    ComentarioPublicacion = 3,
    NuevaEncuesta = 4,
    NuevoTicket = 5,
    NuevaReserva = 6,
    DocumentoNuevo = 7,
}

/// <summary>Notificación para el/los administrador(es) de un consorcio.</summary>
public class Notificacion : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public TipoNotificacion Tipo { get; set; } = TipoNotificacion.General;
    public string Titulo { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    /// <summary>Ruta interna opcional para navegar al hacer clic.</summary>
    public string? Enlace { get; set; }

    public DateTime? LeidaUtc { get; set; }
}
