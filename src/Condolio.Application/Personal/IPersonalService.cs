using Condolio.Application.Common;
using Condolio.Domain.Personal;

namespace Condolio.Application.Personal;

public record MiembroPersonalDto(
    Guid Id,
    string Nombre,
    string Apellido,
    TipoPersonal Tipo,
    bool TieneCuenta,
    string? Email,
    Guid? CredencialId,
    string? CredencialNombre,
    bool Activo);

public record PersonalListaDto(
    IReadOnlyList<MiembroPersonalDto> Miembros,
    int Total,
    int Seguridad,
    int ConAcceso);

/// <summary>Al guardar: <c>CredencialId</c> vincula (o desvincula si es null) el acceso a una credencial de caseta.</summary>
public record GuardarPersonalDto(
    string Nombre,
    string Apellido,
    TipoPersonal Tipo,
    Guid? CredencialId);

public record CredencialOpcionDto(Guid Id, string Nombre, string Email);

/// <summary>Resultado de crear personal; si se creó una cuenta, incluye la contraseña temporal (se muestra una sola vez).</summary>
public record PersonalCreadoDto(MiembroPersonalDto Miembro, string? PasswordTemporal);

public interface IPersonalService
{
    Task<Result<PersonalListaDto>> ListarAsync(Guid consorcioId, string? busqueda, CancellationToken ct = default);
    Task<Result<PersonalCreadoDto>> CrearAsync(Guid consorcioId, GuardarPersonalDto dto, CancellationToken ct = default);
    Task<Result<MiembroPersonalDto>> ActualizarAsync(Guid consorcioId, Guid id, GuardarPersonalDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct = default);

    /// <summary>Credenciales de caseta disponibles para vincular a un miembro del staff.</summary>
    Task<Result<IReadOnlyList<CredencialOpcionDto>>> CredencialesDisponiblesAsync(Guid consorcioId, Guid? incluirId = null, CancellationToken ct = default);
}
