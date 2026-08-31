using Condolio.Application.Common;
using Condolio.Domain.Accesos;

namespace Condolio.Application.Accesos;

public record PaseAdminDto(
    Guid Id,
    string Codigo,
    TipoPase TipoPase,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string VisitanteNombre,
    string? Patente,
    DateTime FechaEntrada,
    DateTime? ValidoHastaUtc,
    EstadoPase Estado,
    string CreadoPor,
    DateTime CreadoUtc,
    int UsosCount,
    int UsosMax,
    string Destino);

public record PasesAdminListaDto(
    IReadOnlyList<PaseAdminDto> Pases,
    int Activos,
    int GeneradosEnRango,
    int EscaneadosHoy);

public record CrearPaseAdminDto(
    Guid? UnidadId,
    TipoPase TipoPase,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string VisitanteNombre,
    string? Patente,
    DateTime FechaEntrada,
    DateTime? ValidoHasta);

public record RegistroBitacoraDto(
    Guid Id,
    string VisitanteNombre,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string? Patente,
    string? Documento,
    string Unidad,
    DateTime IngresoUtc,
    DateTime? EgresoUtc,
    bool ConQr,
    string RegistradoPor,
    string? Nota);

public record BitacoraDto(
    IReadOnlyList<RegistroBitacoraDto> Registros,
    int AdentroAhora);

public record ResumenAccesoDto(int AdentroAhora, int EntradasHoy, int SalidasHoy);

public record EntradaManualDto(
    string VisitanteNombre,
    TipoVisita TipoVisita,
    TipoVehiculo Vehiculo,
    string? Patente,
    Guid? UnidadId,
    string? Nota);

public interface IAccesoAdminService
{
    Task<Result<PasesAdminListaDto>> ListarPasesAsync(Guid consorcioId, int anio, int mes, string? busqueda, CancellationToken ct = default);
    Task<Result<PaseAccesoDto>> ObtenerPaseAsync(Guid consorcioId, Guid paseId, CancellationToken ct = default);
    Task<Result<PaseAccesoDto>> CrearPaseAsync(Guid consorcioId, string usuarioId, string usuarioNombre, CrearPaseAdminDto dto, CancellationToken ct = default);
    Task<Result> RevocarPaseAsync(Guid consorcioId, Guid paseId, CancellationToken ct = default);

    Task<Result<BitacoraDto>> BitacoraAsync(Guid consorcioId, DateOnly fecha, int dias, string? filtro, string? busqueda, CancellationToken ct = default);
    Task<Result> RegistrarEgresoAsync(Guid consorcioId, Guid registroId, CancellationToken ct = default);

    Task<Result<ResumenAccesoDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RegistroBitacoraDto>>> AdentroAhoraAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<RegistroBitacoraDto>> RegistrarEntradaManualAsync(Guid consorcioId, EntradaManualDto dto, string guardiaId, string guardiaNombre, CancellationToken ct = default);
}
