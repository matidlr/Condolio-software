using Condolio.Domain.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Domain.Tickets;

public enum CategoriaTicket
{
    Amenidades = 0,
    Seguridad = 1,
    Mantenimiento = 2,
    Mascotas = 3,
    Ruido = 4,
    Vecinos = 5,
    Servicios = 6,
    Otro = 7,
}

public enum EstadoTicket
{
    Nuevo = 0,
    EnProgreso = 1,
    EsperandoInformacion = 2,
    PendienteAprobacion = 3,
    Resuelto = 4,
}

public enum PrioridadTicket
{
    Baja = 0,
    Media = 1,
    Alta = 2,
    Critica = 3,
}

/// <summary>
/// Reclamo formal gestionado por la administración. Se crea manualmente ("Nuevo Ticket")
/// o al escalar una <see cref="UnidadIncidencia"/>.
/// </summary>
public class Ticket : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }

    /// <summary>Correlativo por consorcio, para mostrar "#12".</summary>
    public int Numero { get; set; }

    public string? Titulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public CategoriaTicket Categoria { get; set; } = CategoriaTicket.Otro;
    public EstadoTicket Estado { get; set; } = EstadoTicket.Nuevo;
    public PrioridadTicket Prioridad { get; set; } = PrioridadTicket.Media;

    public Guid? UnidadId { get; set; }
    public Guid? IncidenciaId { get; set; }

    /// <summary>Etiquetas separadas por coma.</summary>
    public string? Etiquetas { get; set; }
    /// <summary>Texto libre de ubicación ("Área común", "Cochera 3", etc.).</summary>
    public string? Ubicacion { get; set; }
    public DateTime? FechaLimite { get; set; }

    public string ReportadoPorUsuarioId { get; set; } = string.Empty;
    /// <summary>Nombre del reportante congelado al crear (por si luego se borra el usuario).</summary>
    public string ReportadoPorNombre { get; set; } = string.Empty;
    public DateTime ReportadoUtc { get; set; } = DateTime.UtcNow;

    public string? AsignadoAUsuarioId { get; set; }
    public string? AsignadoANombre { get; set; }

    /// <summary>Momento del último cambio de estado, para "Tiempo en Estado".</summary>
    public DateTime EstadoDesdeUtc { get; set; } = DateTime.UtcNow;
    public DateTime UltimaActividadUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ArchivadoUtc { get; set; }

    public List<TicketComentario> Comentarios { get; set; } = new();
}

public class TicketComentario : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
    public string AutorUsuarioId { get; set; } = string.Empty;
    /// <summary>true = nota interna del equipo; false = respuesta visible para el residente.</summary>
    public bool EsInterna { get; set; }
}
