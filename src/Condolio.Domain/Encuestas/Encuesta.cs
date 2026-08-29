using Condolio.Domain.Common;

namespace Condolio.Domain.Encuestas;

public enum EstadoEncuesta
{
    Borrador = 0,
    Activa = 1,
    Cerrada = 2,
}

public enum CategoriaEncuesta
{
    General = 0,
    Mantenimiento = 1,
    Evento = 2,
}

/// <summary>Encuesta / votación de la comunidad.</summary>
public class Encuesta : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public CategoriaEncuesta Categoria { get; set; } = CategoriaEncuesta.General;
    public EstadoEncuesta Estado { get; set; } = EstadoEncuesta.Borrador;

    /// <summary>Permite seleccionar más de una opción.</summary>
    public bool MultiplesOpciones { get; set; }
    /// <summary>Oculta quién votó cada opción.</summary>
    public bool Anonima { get; set; }

    public DateTime? PublicadaUtc { get; set; }
    public DateTime? CierreUtc { get; set; }

    public string AutorUsuarioId { get; set; } = string.Empty;
    public string AutorNombre { get; set; } = string.Empty;

    public List<OpcionEncuesta> Opciones { get; set; } = new();
    public List<VotoEncuesta> Votos { get; set; } = new();
}

public class OpcionEncuesta : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid EncuestaId { get; set; }
    public Encuesta Encuesta { get; set; } = null!;

    public string Texto { get; set; } = string.Empty;
    public int Orden { get; set; }
}

public class VotoEncuesta : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid EncuestaId { get; set; }
    public Encuesta Encuesta { get; set; } = null!;

    public Guid OpcionId { get; set; }

    public string UsuarioId { get; set; } = string.Empty;
    public string UsuarioNombre { get; set; } = string.Empty;
}
