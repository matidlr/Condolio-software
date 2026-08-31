using Condolio.Application.Common;
using Condolio.Domain.Tenancy;

namespace Condolio.Application.Tenancy;

public record AdminMiembroDto(
    string UsuarioId,
    string Nombre,
    string Email,
    bool EsGeneral,
    bool EsDueno,
    IReadOnlyList<AreaAdmin> Areas);

public record GuardarRolAdminDto(bool EsGeneral, IReadOnlyList<AreaAdmin> Areas);

public record AgregarAdminDto(string Email, bool EsGeneral, IReadOnlyList<AreaAdmin> Areas);

public interface IAdminMiembroService
{
    Task<Result<IReadOnlyList<AdminMiembroDto>>> ListarAsync(Guid administradorId, CancellationToken ct = default);
    Task<Result<AdminMiembroDto>> AgregarAsync(Guid administradorId, AgregarAdminDto dto, CancellationToken ct = default);
    Task<Result<AdminMiembroDto>> CambiarRolAsync(Guid administradorId, string usuarioId, GuardarRolAdminDto dto, CancellationToken ct = default);
    Task<Result> QuitarAsync(Guid administradorId, string usuarioId, CancellationToken ct = default);
}
