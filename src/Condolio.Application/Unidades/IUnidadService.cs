using Condolio.Application.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Application.Unidades;

public record PersonaMiniDto(string Nombre, bool EsContactoPrincipal);

public record UnidadDto(
    Guid Id,
    Guid ConsorcioId,
    string Nombre,
    int Piso,
    TipoUnidad Tipo,
    TipoOcupacion Ocupacion,
    decimal? AreaM2,
    string? Seccion,
    decimal? CuotaMantenimiento,
    decimal? Coeficiente,
    bool Facturable,
    IReadOnlyList<PersonaMiniDto> Propietarios,
    IReadOnlyList<PersonaMiniDto> Inquilinos,
    IReadOnlyList<PersonaMiniDto> Gestores);

public record DetalleUnidadCampos(
    string Nombre,
    int Piso,
    TipoUnidad Tipo,
    decimal? AreaM2,
    string? Seccion,
    decimal? CuotaMantenimiento,
    decimal? Coeficiente,
    bool Facturable);

public record PersonaUnidadDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string? Email,
    string? Telefono,
    RolUnidad Rol,
    bool EsContactoPrincipal,
    bool TieneAcceso);

public record GuardarPersonaDto(
    string Nombre,
    string Apellido,
    string? Email,
    string? Telefono,
    RolUnidad Rol,
    bool EsContactoPrincipal = false,
    string? UsuarioId = null);

public record UnidadDetalleDto(
    Guid Id,
    Guid ConsorcioId,
    string ConsorcioNombre,
    string Nombre,
    int Piso,
    TipoUnidad Tipo,
    TipoOcupacion Ocupacion,
    bool InquilinosVenFinanzas,
    decimal? AreaM2,
    string? Seccion,
    decimal? CuotaMantenimiento,
    decimal? Coeficiente,
    bool Facturable,
    string OcupacionEfectiva,
    int Residentes,
    decimal Saldo,
    bool NecesitaAtencion,
    IReadOnlyList<PersonaUnidadDto> Personas);

public record CrearUnidadDto(
    string Nombre,
    int Piso,
    TipoUnidad Tipo,
    decimal? CuotaMantenimiento,
    decimal? Coeficiente,
    bool Facturable,
    decimal? AreaM2 = null,
    string? Seccion = null);

public record ActualizarUnidadDto(
    string Nombre,
    int Piso,
    TipoUnidad Tipo,
    decimal? CuotaMantenimiento,
    decimal? Coeficiente,
    bool Facturable,
    decimal? AreaM2 = null,
    string? Seccion = null);

/// <summary>Alta masiva: una línea por unidad. Si <see cref="Reemplazar"/> es true, borra las unidades existentes primero.</summary>
public record CrearUnidadesLoteDto(IReadOnlyList<CrearUnidadDto> Unidades, bool Reemplazar = false);

/// <summary>Importación por CSV: upsert por nombre. Si <see cref="EliminarFaltantes"/>, borra las que no vienen.</summary>
public record ImportarUnidadesDto(IReadOnlyList<CrearUnidadDto> Unidades, bool EliminarFaltantes = true);

public record ImportarUnidadesResultado(int Nuevas, int Actualizadas, int Eliminadas, int TotalDespues);

public record EdicionMasivaItem(
    Guid Id,
    string Nombre,
    TipoUnidad Tipo,
    TipoOcupacion Ocupacion,
    int Piso,
    decimal? AreaM2,
    decimal? CuotaMantenimiento,
    decimal? Coeficiente,
    string? Seccion);

public record EdicionMasivaDto(IReadOnlyList<EdicionMasivaItem> Items);

public interface IUnidadService
{
    Task<Result<IReadOnlyList<UnidadDto>>> ListarAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<UnidadDetalleDto>> ObtenerAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default);
    Task<Result<UnidadDto>> CrearAsync(Guid consorcioId, CrearUnidadDto dto, CancellationToken ct = default);
    Task<Result<int>> CrearLoteAsync(Guid consorcioId, CrearUnidadesLoteDto dto, CancellationToken ct = default);
    Task<Result<ImportarUnidadesResultado>> ImportarAsync(Guid consorcioId, ImportarUnidadesDto dto, CancellationToken ct = default);
    Task<Result<int>> EditarMasivoAsync(Guid consorcioId, EdicionMasivaDto dto, CancellationToken ct = default);
    Task<Result<UnidadDto>> ActualizarAsync(Guid consorcioId, Guid unidadId, ActualizarUnidadDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default);

    Task<Result> CambiarOcupacionAsync(Guid consorcioId, Guid unidadId, TipoOcupacion ocupacion, CancellationToken ct = default);
    Task<Result> CambiarInquilinosVenFinanzasAsync(Guid consorcioId, Guid unidadId, bool permitir, CancellationToken ct = default);
    Task<Result<PersonaUnidadDto>> AgregarPersonaAsync(Guid consorcioId, Guid unidadId, GuardarPersonaDto dto, CancellationToken ct = default);
    Task<Result> MarcarContactoPrincipalAsync(Guid consorcioId, Guid unidadId, Guid personaId, CancellationToken ct = default);
    Task<Result> CambiarRolPersonaAsync(Guid consorcioId, Guid unidadId, Guid personaId, RolUnidad rol, CancellationToken ct = default);
    Task<Result> EliminarPersonaAsync(Guid consorcioId, Guid unidadId, Guid personaId, CancellationToken ct = default);
}
