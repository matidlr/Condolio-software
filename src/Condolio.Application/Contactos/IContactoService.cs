using Condolio.Application.Common;

namespace Condolio.Application.Contactos;

public record ContactoDto(
    Guid Id,
    string Nombre,
    string Categoria,
    string Telefono,
    string? Email,
    string? Empresa,
    string? Notas,
    string CreadoPor,
    bool EsMio,
    DateTime CreadoUtc);

public record GuardarContactoDto(
    string Nombre,
    string Categoria,
    string Telefono,
    string? Email,
    string? Empresa,
    string? Notas);

public interface IContactoService
{
    Task<Result<IReadOnlyList<ContactoDto>>> ListarAsync(Guid consorcioId, string? usuarioId, CancellationToken ct = default);
    Task<Result<ContactoDto>> CrearAsync(Guid consorcioId, string usuarioId, string usuarioNombre, GuardarContactoDto dto, CancellationToken ct = default);
    Task<Result<ContactoDto>> ActualizarAsync(Guid consorcioId, Guid contactoId, GuardarContactoDto dto, CancellationToken ct = default);
    Task<Result> EliminarAsync(Guid consorcioId, Guid contactoId, string? soloSiCreadoPor, CancellationToken ct = default);
}
