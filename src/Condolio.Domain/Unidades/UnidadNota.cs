using Condolio.Domain.Common;

namespace Condolio.Domain.Unidades;

/// <summary>Nota interna del equipo administrativo sobre una unidad.</summary>
public class UnidadNota : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public Guid UnidadId { get; set; }
    public Unidad Unidad { get; set; } = null!;

    public string Texto { get; set; } = string.Empty;

    /// <summary>Usuario Identity que creó la nota.</summary>
    public string AutorUsuarioId { get; set; } = string.Empty;
}
