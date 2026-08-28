using Condolio.Domain.Common;

namespace Condolio.Domain.Unidades;

public enum CategoriaIncidencia
{
    Ruido = 0,
    Mascotas = 1,
    Seguridad = 2,
    Mantenimiento = 3,
    DanoPropiedad = 4,
    Visitante = 5,
    Disputa = 6,
    Cortesia = 7,
    Vecinos = 8,
    Otro = 9,
}

public enum SeveridadIncidencia
{
    Baja = 0,
    Media = 1,
    Alta = 2,
    Critica = 3,
}

/// <summary>Registro confidencial de un evento de la unidad. Nunca visible para residentes.</summary>
public class UnidadIncidencia : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid UnidadId { get; set; }
    public Unidad Unidad { get; set; } = null!;

    public string? Titulo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public CategoriaIncidencia Categoria { get; set; } = CategoriaIncidencia.Otro;
    public SeveridadIncidencia Severidad { get; set; } = SeveridadIncidencia.Media;
    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;

    /// <summary>Etiquetas separadas por coma.</summary>
    public string? Etiquetas { get; set; }

    public string AutorUsuarioId { get; set; } = string.Empty;

    /// <summary>Fecha en que se escaló a ticket (módulo Tickets, pendiente).</summary>
    public DateTime? EscaladaUtc { get; set; }

    public List<IncidenciaComentario> Comentarios { get; set; } = new();
}
