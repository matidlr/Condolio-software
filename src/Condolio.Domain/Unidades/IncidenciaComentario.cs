using Condolio.Domain.Common;

namespace Condolio.Domain.Unidades;

/// <summary>Nota / comentario dentro del historial de una incidencia.</summary>
public class IncidenciaComentario : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid IncidenciaId { get; set; }
    public UnidadIncidencia Incidencia { get; set; } = null!;

    public string Texto { get; set; } = string.Empty;
    public string AutorUsuarioId { get; set; } = string.Empty;
}
