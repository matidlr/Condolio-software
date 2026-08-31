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
    IncidenciaActualizada = 8,
    RespuestaIncidencia = 9,
    NuevoAnuncio = 10,
}

/// <summary>Grupos de notificaciones para las preferencias del residente.</summary>
public enum CategoriaNotificacion
{
    General = 0,
    Seguridad = 1,
    Finanzas = 2,
    Comunidad = 3,
    EventosReservas = 4,
    EdificioEntregas = 5,
}

/// <summary>
/// Notificación. Si <see cref="DestinatarioUsuarioId"/> es null, es para los administradores
/// del consorcio; si tiene valor, es para ese usuario (residente).
/// </summary>
public class Notificacion : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string? DestinatarioUsuarioId { get; set; }
    public CategoriaNotificacion Categoria { get; set; } = CategoriaNotificacion.General;

    public TipoNotificacion Tipo { get; set; } = TipoNotificacion.General;
    public string Titulo { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    /// <summary>Ruta interna opcional para navegar al hacer clic.</summary>
    public string? Enlace { get; set; }

    public DateTime? LeidaUtc { get; set; }
}

/// <summary>Preferencias de notificación de un usuario (una fila por usuario).</summary>
public class PreferenciasNotificacion : Entity
{
    public string UsuarioId { get; set; } = string.Empty;

    public bool SeguridadApp { get; set; } = true;
    public bool SeguridadMail { get; set; } = true;
    public bool FinanzasApp { get; set; } = true;
    public bool FinanzasMail { get; set; } = true;
    public bool ComunidadApp { get; set; } = true;
    public bool ComunidadMail { get; set; } = false;
    public bool EventosApp { get; set; } = true;
    public bool EventosMail { get; set; } = false;
    public bool EdificioApp { get; set; } = true;
    public bool EdificioMail { get; set; } = false;

    public bool AppHabilitada(CategoriaNotificacion c) => c switch
    {
        CategoriaNotificacion.Seguridad => SeguridadApp,
        CategoriaNotificacion.Finanzas => FinanzasApp,
        CategoriaNotificacion.Comunidad => ComunidadApp,
        CategoriaNotificacion.EventosReservas => EventosApp,
        CategoriaNotificacion.EdificioEntregas => EdificioApp,
        _ => true,
    };
}
