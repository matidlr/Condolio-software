using Condolio.Application.Common;
using Condolio.Domain.Unidades;

namespace Condolio.Application.Residentes;

public record ResidenteDto(
    Guid Id,
    string Nombre,
    string Apellido,
    string? Email,
    string? Telefono,
    RolUnidad Rol,
    bool EsContactoPrincipal,
    bool TieneAcceso,
    Guid UnidadId,
    string UnidadNombre);

public record DirectorioDto(
    IReadOnlyList<ResidenteDto> Residentes,
    int Total,
    int Propietarios,
    int Inquilinos,
    int Gestores,
    int UnidadesSinAsignar);

public record ResidenteSinUnidadDto(string UsuarioId, string Nombre, string Apellido, string Email);

public record InvitacionDto(
    Guid Id,
    string Email,
    string? Nombre,
    string Estado,
    Guid? UnidadId,
    string? UnidadNombre,
    RolUnidad Rol,
    DateTime CreadoUtc,
    DateTime ExpiraUtc);

public record CrearInvitacionDto(string Email, string? Nombre, Guid? UnidadId, RolUnidad Rol = RolUnidad.Propietario);

public record InvitacionPublicaDto(
    string ConsorcioNombre,
    string Email,
    string? Nombre,
    string? UnidadNombre,
    RolUnidad Rol,
    bool Valida,
    string? Motivo);

public record AceptarInvitacionDto(string Nombre, string Apellido, string? Telefono, string Password);

public interface IInvitacionPublicaService
{
    Task<Result<InvitacionPublicaDto>> VerAsync(string token, CancellationToken ct = default);
    Task<Result<AceptarInvitacionResultado>> AceptarAsync(string token, AceptarInvitacionDto dto, CancellationToken ct = default);
}

public record AceptarInvitacionResultado(string Token, DateTime ExpiraUtc, string Email, string Nombre, IReadOnlyList<string> Roles);

public record InvitarLoteItem(string? Nombre, string Email, string? Telefono, string? Unidad, string? Rol);

public record InvitarLoteResultadoFila(string Email, bool Ok, string? Motivo, string? UnidadNombre);

public record InvitarLoteResultado(int Enviadas, int Fallidas, IReadOnlyList<InvitarLoteResultadoFila> Filas);

public interface IResidenteService
{
    Task<Result<DirectorioDto>> DirectorioAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<InvitacionDto>>> InvitacionesAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<InvitacionDto>> InvitarAsync(Guid consorcioId, CrearInvitacionDto dto, CancellationToken ct = default);
    Task<Result<InvitacionDto>> EditarInvitacionAsync(Guid consorcioId, Guid invitacionId, CrearInvitacionDto dto, CancellationToken ct = default);
    Task<Result<InvitarLoteResultado>> InvitarLoteAsync(Guid consorcioId, IReadOnlyList<InvitarLoteItem> items, bool notificar, CancellationToken ct = default);
    Task<Result> CancelarInvitacionAsync(Guid consorcioId, Guid invitacionId, CancellationToken ct = default);
    Task<Result> ReenviarInvitacionAsync(Guid consorcioId, Guid invitacionId, CancellationToken ct = default);
    Task<Result<int>> ReenviarPendientesAsync(Guid consorcioId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ResidenteSinUnidadDto>>> PorAsignarAsync(Guid consorcioId, CancellationToken ct = default);
}
