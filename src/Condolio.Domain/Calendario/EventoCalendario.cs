using Condolio.Domain.Common;

namespace Condolio.Domain.Calendario;

public enum CategoriaEvento
{
    General = 0,
    Reunion = 1,
    Mantenimiento = 2,
    Social = 3,
    Amenidad = 4,
}

/// <summary>Evento del calendario comunitario del consorcio.</summary>
public class EventoCalendario : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid ConsorcioId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Ubicacion { get; set; }
    public CategoriaEvento Categoria { get; set; } = CategoriaEvento.General;

    public DateTime InicioUtc { get; set; }
    public DateTime FinUtc { get; set; }
    public bool TodoElDia { get; set; }

    /// <summary>Si al guardar se publicó un anuncio para avisar a la comunidad.</summary>
    public bool NotificoComunidad { get; set; }

    public string CreadoPorUsuarioId { get; set; } = string.Empty;
    public string CreadoPorNombre { get; set; } = string.Empty;
}
