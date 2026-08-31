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
    bool Activo);

public record PersonalListaDto(
    IReadOnlyList<MiembroPersonalDto> Miembros,
    int Total,
    int Seguridad,
    int ConAcceso);

/// <summary>Al crear: si <c>EmailCuenta</c> viene, se crea una cuenta de acceso (rol Personal) con ese email.</summary>
public record GuardarPersonalDto(
    string Nombre,
    string Apellido,
    TipoPersonal Tipo,
    string? EmailCuenta);

/// <summary>Resultado de crear personal; si se creó una cuenta, incluye la contraseña temporal (se muestra una sola vez).</summary>
public record PersonalCreadoDto(MiembroPersonalDto Miembro, string? PasswordTemporal);

public interface IPersonalService
{
    Task<Result<PersonalListaDto>> ListarAsync(Guid consorcioId, string? busqueda, CancellationToken ct = default);
    Task<Result<PersonalCreadoDto>> CrearAsync(Guid consorcioId, GuardarPersonalDto dto, CancellationToken ct = default);
    Task<Result<MiembroPersonalDto>> ActualizarAsync(Guid consorcioId, Guid id, GuardarPersonalDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct = default);
}
