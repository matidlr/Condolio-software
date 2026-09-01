using Condolio.Application.Common;
using Condolio.Domain.Tenancy;

namespace Condolio.Application.Tenancy;

/// <summary>
/// Fila de la lista de administradores: un miembro real (<see cref="Id"/> = usuarioId) o una
/// invitación pendiente a un correo sin cuenta todavía (<see cref="Id"/> = "inv:{invitacionId}").
/// </summary>
public record AdminMiembroDto(
    string Id,
    string Nombre,
    string Email,
    bool EsGeneral,
    bool EsDueno,
    bool Pendiente,
    IReadOnlyList<AreaAdmin> Areas);

public record GuardarRolAdminDto(bool EsGeneral, IReadOnlyList<AreaAdmin> Areas);

public record AgregarAdminDto(string Email, bool EsGeneral, IReadOnlyList<AreaAdmin> Areas);

public interface IAdminMiembroService
{
    Task<Result<IReadOnlyList<AdminMiembroDto>>> ListarAsync(Guid administradorId, CancellationToken ct = default);
    Task<Result<AdminMiembroDto>> AgregarAsync(Guid administradorId, string invitadoPorUsuarioId, AgregarAdminDto dto, CancellationToken ct = default);
    Task<Result<AdminMiembroDto>> CambiarRolAsync(Guid administradorId, string usuarioId, GuardarRolAdminDto dto, CancellationToken ct = default);
    Task<Result> QuitarAsync(Guid administradorId, string id, CancellationToken ct = default);
}
