using Condolio.Domain.Common;

namespace Condolio.Domain.Accesos;

public enum TipoPase
{
    /// <summary>Una sola entrada.</summary>
    UnaEntrada = 0,
    /// <summary>QR temporal: válido por un rango de fechas.</summary>
    Temporal = 1,
    /// <summary>Pase de fiesta: varias personas / varios ingresos en un día.</summary>
    PaseFiesta = 2,
}

public enum TipoVisita
{
    Familia = 0,
    Amigo = 1,
    Huesped = 2,
    EntregaComida = 3,
    EntregaDomicilio = 4,
    ProveedorServicios = 5,
    Empleado = 6,
    Visita = 7,
    Taxi = 8,
    Residente = 9,
    Otro = 10,
}

public enum TipoVehiculo
{
    SinVehiculo = 0,
    Auto = 1,
    Motocicleta = 2,
}

public enum EstadoPase
{
    Activo = 0,
    Usado = 1,
    Vencido = 2,
    Revocado = 3,
}

/// <summary>Pase de acceso (QR) generado por un residente para una visita.</summary>
public class PaseAcceso : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }
    public Guid UnidadId { get; set; }

    public string CreadoPorUsuarioId { get; set; } = string.Empty;
    public string CreadoPorNombre { get; set; } = string.Empty;

    public TipoPase TipoPase { get; set; } = TipoPase.UnaEntrada;
    public TipoVisita TipoVisita { get; set; } = TipoVisita.Familia;
    public TipoVehiculo Vehiculo { get; set; } = TipoVehiculo.SinVehiculo;

    public string VisitanteNombre { get; set; } = string.Empty;
    public string? Patente { get; set; }

    public DateTime FechaEntrada { get; set; }
    public DateTime? ValidoHastaUtc { get; set; }

    public int UsosMax { get; set; } = 1;
    public int UsosCount { get; set; }
    public DateTime? PrimerUsoUtc { get; set; }

    public EstadoPase Estado { get; set; } = EstadoPase.Activo;

    /// <summary>Token opaco que viaja dentro del QR.</summary>
    public string Token { get; set; } = Guid.NewGuid().ToString("N");
}

/// <summary>Visita registrada en la caseta/portería para una unidad.</summary>
public class RegistroVisita : Entity, ITenantOwned
{
    public Guid AdministradorId { get; set; }
    public Guid ConsorcioId { get; set; }
    public Guid UnidadId { get; set; }

    public Guid? PaseAccesoId { get; set; }

    public string VisitanteNombre { get; set; } = string.Empty;
    public TipoVisita TipoVisita { get; set; } = TipoVisita.Familia;
    public TipoVehiculo Vehiculo { get; set; } = TipoVehiculo.SinVehiculo;
    public string? Patente { get; set; }

    public DateTime IngresoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EgresoUtc { get; set; }

    public string RegistradoPorUsuarioId { get; set; } = string.Empty;
    public string RegistradoPorNombre { get; set; } = string.Empty;
    public string? Nota { get; set; }
}
