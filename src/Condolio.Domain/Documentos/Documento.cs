using Condolio.Domain.Common;

namespace Condolio.Domain.Documentos;

public enum NivelAcceso
{
    /// <summary>Solo administradores.</summary>
    Admin = 0,
    /// <summary>Administradores y propietarios.</summary>
    Propietarios = 1,
    /// <summary>Toda la comunidad (incluye inquilinos).</summary>
    Todos = 2,
}

public class CarpetaDocumento : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public Guid? CarpetaPadreId { get; set; }
    public NivelAcceso Nivel { get; set; } = NivelAcceso.Todos;
}

public class Documento : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public Guid? CarpetaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Tamano { get; set; }
    public string RutaRelativa { get; set; } = string.Empty;

    public NivelAcceso Nivel { get; set; } = NivelAcceso.Todos;
    public bool Destacado { get; set; }
    public DateTime? UltimoAccesoUtc { get; set; }

    public string SubidoPorUsuarioId { get; set; } = string.Empty;
    public string SubidoPorNombre { get; set; } = string.Empty;
}
