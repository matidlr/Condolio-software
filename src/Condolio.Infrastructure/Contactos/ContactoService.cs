using Condolio.Application.Common;
using Condolio.Application.Contactos;
using Condolio.Domain.Contactos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Contactos;

public class ContactoService : IContactoService
{
    private readonly CondolioDbContext _db;

    public ContactoService(CondolioDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ContactoDto>>> ListarAsync(Guid consorcioId, string? usuarioId, CancellationToken ct = default)
    {
        var lista = await _db.Contactos.IgnoreQueryFilters()
            .Where(c => c.ConsorcioId == consorcioId)
            .OrderBy(c => c.Categoria).ThenBy(c => c.Nombre)
            .Select(c => new ContactoDto(
                c.Id, c.Nombre, c.Categoria, c.Telefono, c.Email, c.Empresa, c.Notas,
                string.IsNullOrWhiteSpace(c.CreadoPorNombre) ? "—" : c.CreadoPorNombre,
                usuarioId != null && c.CreadoPorUsuarioId == usuarioId,
                c.CreadoUtc))
            .ToListAsync(ct);
        return Result<IReadOnlyList<ContactoDto>>.Ok(lista);
    }

    public async Task<Result<ContactoDto>> CrearAsync(
        Guid consorcioId, string usuarioId, string usuarioNombre, GuardarContactoDto dto, CancellationToken ct = default)
    {
        var val = Validar(dto);
        if (val is not null) return Result<ContactoDto>.Fail(val);

        var adminId = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (adminId is not { } admin) return Result<ContactoDto>.Fail("Consorcio no encontrado.");

        var c = new Contacto
        {
            AdministradorId = admin,
            ConsorcioId = consorcioId,
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombre = string.IsNullOrWhiteSpace(usuarioNombre) ? "—" : usuarioNombre,
        };
        Aplicar(c, dto);
        _db.Contactos.Add(c);
        await _db.SaveChangesAsync(ct);
        return Result<ContactoDto>.Ok(Mapear(c, usuarioId));
    }

    public async Task<Result<ContactoDto>> ActualizarAsync(Guid consorcioId, Guid contactoId, GuardarContactoDto dto, CancellationToken ct = default)
    {
        var val = Validar(dto);
        if (val is not null) return Result<ContactoDto>.Fail(val);

        var c = await _db.Contactos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == contactoId && x.ConsorcioId == consorcioId, ct);
        if (c is null) return Result<ContactoDto>.Fail("Contacto no encontrado.");

        Aplicar(c, dto);
        await _db.SaveChangesAsync(ct);
        return Result<ContactoDto>.Ok(Mapear(c, c.CreadoPorUsuarioId));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid contactoId, string? soloSiCreadoPor, CancellationToken ct = default)
    {
        var c = await _db.Contactos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == contactoId && x.ConsorcioId == consorcioId, ct);
        if (c is null) return Result.Fail("Contacto no encontrado.");
        if (soloSiCreadoPor is not null && c.CreadoPorUsuarioId != soloSiCreadoPor)
            return Result.Fail("Solo quien lo creó puede eliminar este contacto.");

        _db.Contactos.Remove(c);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static string? Validar(GuardarContactoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return "El nombre es obligatorio.";
        if (string.IsNullOrWhiteSpace(dto.Telefono)) return "El teléfono es obligatorio.";
        return null;
    }

    private static void Aplicar(Contacto c, GuardarContactoDto dto)
    {
        c.Nombre = dto.Nombre.Trim();
        c.Categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? "Otro" : dto.Categoria.Trim();
        c.Telefono = dto.Telefono.Trim();
        c.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        c.Empresa = string.IsNullOrWhiteSpace(dto.Empresa) ? null : dto.Empresa.Trim();
        c.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();
    }

    private static ContactoDto Mapear(Contacto c, string? usuarioId) => new(
        c.Id, c.Nombre, c.Categoria, c.Telefono, c.Email, c.Empresa, c.Notas,
        string.IsNullOrWhiteSpace(c.CreadoPorNombre) ? "—" : c.CreadoPorNombre,
        usuarioId != null && c.CreadoPorUsuarioId == usuarioId,
        c.CreadoUtc);
}
