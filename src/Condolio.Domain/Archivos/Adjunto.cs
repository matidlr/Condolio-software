using Condolio.Domain.Common;

namespace Condolio.Domain.Archivos;

public enum TipoAdjuntoOwner
{
    Nota = 0,
    Incidencia = 1,
    Ticket = 2,
    Amenidad = 3,
}

/// <summary>Archivo adjunto (imagen, video o PDF) asociado a una nota, incidencia o ticket.</summary>
public class Adjunto : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }

    public TipoAdjuntoOwner OwnerTipo { get; set; }
    public Guid OwnerId { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Tamano { get; set; }

    /// <summary>Ruta del archivo relativa a la carpeta base de storage.</summary>
    public string RutaRelativa { get; set; } = string.Empty;

    public string SubidoPorUsuarioId { get; set; } = string.Empty;

    public bool EsImagen => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
