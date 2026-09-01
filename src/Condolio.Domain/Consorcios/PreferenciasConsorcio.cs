using Condolio.Domain.Common;

namespace Condolio.Domain.Consorcios;

/// <summary>Funciones y permisos configurables de un consorcio (una fila por consorcio).</summary>
public class PreferenciasConsorcio : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    // ---- Anuncios y muro ----

    /// <summary>Los residentes pueden crear publicaciones en el muro.</summary>
    public bool ResidentesPublican { get; set; } = true;

    /// <summary>Se permiten comentarios en las publicaciones del muro.</summary>
    public bool ComentariosHabilitados { get; set; } = true;

    /// <summary>Toda publicación de un administrador se envía además por correo a los residentes.</summary>
    public bool AnunciosSiemprePorCorreo { get; set; }
}
