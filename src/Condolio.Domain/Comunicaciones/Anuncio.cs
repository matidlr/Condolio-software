using Condolio.Domain.Common;

namespace Condolio.Domain.Comunicaciones;

public enum CategoriaAnuncio
{
    General = 0,
    Mantenimiento = 1,
    Urgente = 2,
    Evento = 3,
}

/// <summary>Publicación del muro de comunicaciones del consorcio, visible para los residentes.</summary>
public class Anuncio : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public CategoriaAnuncio Categoria { get; set; } = CategoriaAnuncio.General;

    public bool Fijado { get; set; }
    public DateTime PublicadoUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Fecha del evento (solo cuando <see cref="Categoria"/> es Evento).</summary>
    public DateTime? EventoFechaUtc { get; set; }

    public string AutorUsuarioId { get; set; } = string.Empty;
    public string AutorNombre { get; set; } = string.Empty;

    /// <summary>Ids de <see cref="Condolio.Domain.Archivos.Adjunto"/> separados por coma.</summary>
    public string? ImagenesIds { get; set; }

    public List<AnuncioComentario> Comentarios { get; set; } = new();
    public List<AnuncioLike> Likes { get; set; } = new();
}

public class AnuncioComentario : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid AnuncioId { get; set; }
    public Anuncio Anuncio { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
    public string AutorUsuarioId { get; set; } = string.Empty;
    public string AutorNombre { get; set; } = string.Empty;
}

public class AnuncioLike : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid AnuncioId { get; set; }
    public Anuncio Anuncio { get; set; } = null!;
    public string UsuarioId { get; set; } = string.Empty;
    public string UsuarioNombre { get; set; } = string.Empty;
}
