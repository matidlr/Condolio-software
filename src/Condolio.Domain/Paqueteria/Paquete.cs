using Condolio.Domain.Common;

namespace Condolio.Domain.Paqueteria;

public enum TipoPaquete
{
    Paquete = 0,
    Correo = 1,
    Otro = 2,
}

public enum EstadoPaquete
{
    /// <summary>Llegó a la caseta, esperando que lo retire el residente.</summary>
    EnRecepcion = 0,
    Entregado = 1,
}

/// <summary>Paquete / encomienda recibido en la caseta para una unidad.</summary>
public class Paquete : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }

    public Guid UnidadId { get; set; }
    public string UnidadNombre { get; set; } = string.Empty;

    public TipoPaquete Tipo { get; set; } = TipoPaquete.Paquete;
    public int Cantidad { get; set; } = 1;
    public string? Transportista { get; set; }
    public string? Descripcion { get; set; }

    public EstadoPaquete Estado { get; set; } = EstadoPaquete.EnRecepcion;

    public DateTime LlegadaUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EntregaUtc { get; set; }

    public string RegistradoPorNombre { get; set; } = string.Empty;
    public string? EntregadoPorNombre { get; set; }

    /// <summary>Nombre de quien retiró el paquete (residente u otra persona autorizada).</summary>
    public string? RetiradoPorNombre { get; set; }
}
