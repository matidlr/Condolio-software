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
    /// <summary>Solo miembros de la junta.</summary>
    Junta = 3,
}

public enum CategoriaDocumento
{
    General = 0,
    ReglasYRegulaciones = 1,
    ActasReuniones = 2,
    Financiero = 3,
    LegalYContratos = 4,
    Recibos = 5,
    Mantenimiento = 6,
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
    public CategoriaDocumento Categoria { get; set; } = CategoriaDocumento.General;
    public bool Destacado { get; set; }
    public DateTime? UltimoAccesoUtc { get; set; }

    public string SubidoPorUsuarioId { get; set; } = string.Empty;
    public string SubidoPorNombre { get; set; } = string.Empty;
}

/// <summary>Registro de una vista o descarga de un documento (para analíticas).</summary>
public class DocumentoAcceso : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }
    public Guid DocumentoId { get; set; }
    /// <summary>false = vista previa, true = descarga.</summary>
    public bool EsDescarga { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
}
