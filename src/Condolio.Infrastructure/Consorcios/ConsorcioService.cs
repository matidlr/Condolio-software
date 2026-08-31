using Condolio.Application.Common;
using Condolio.Application.Consorcios;
using Condolio.Domain.Consorcios;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Consorcios;

public class ConsorcioService : IConsorcioService
{
    private readonly CondolioDbContext _db;

    public ConsorcioService(CondolioDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConsorcioDto>> ListarAsync(CancellationToken ct = default)
    {
        return await _db.Consorcios
            .OrderBy(c => c.Nombre)
            .Select(c => new ConsorcioDto(
                c.Id, c.Nombre, c.Tipo, c.Direccion, c.Localidad, c.Provincia, c.Pais,
                c.Unidades.Count))
            .ToListAsync(ct);
    }

    public async Task<Result<ConsorcioDto>> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await _db.Consorcios
            .Where(c => c.Id == id)
            .Select(c => new ConsorcioDto(
                c.Id, c.Nombre, c.Tipo, c.Direccion, c.Localidad, c.Provincia, c.Pais,
                c.Unidades.Count))
            .FirstOrDefaultAsync(ct);

        return dto is null
            ? Result<ConsorcioDto>.Fail("Consorcio no encontrado.")
            : Result<ConsorcioDto>.Ok(dto);
    }

    public async Task<Result<ConsorcioDto>> CrearAsync(CrearConsorcioDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<ConsorcioDto>.Fail("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Direccion))
            return Result<ConsorcioDto>.Fail("La dirección es obligatoria.");

        var consorcio = new Consorcio
        {
            Nombre = dto.Nombre.Trim(),
            Tipo = dto.Tipo,
            Direccion = dto.Direccion.Trim(),
            Localidad = dto.Localidad?.Trim(),
            Provincia = dto.Provincia?.Trim(),
            Pais = string.IsNullOrWhiteSpace(dto.Pais) ? "AR" : dto.Pais!.Trim(),
            CodigoPostal = dto.CodigoPostal?.Trim(),
            Cuit = dto.Cuit?.Trim(),
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
        };
        _db.Consorcios.Add(consorcio);
        await _db.SaveChangesAsync(ct);

        return Result<ConsorcioDto>.Ok(new ConsorcioDto(
            consorcio.Id, consorcio.Nombre, consorcio.Tipo, consorcio.Direccion, consorcio.Localidad,
            consorcio.Provincia, consorcio.Pais, 0));
    }

    public async Task<Result> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        var consorcio = await _db.Consorcios.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (consorcio is null) return Result.Fail("Sociedad no encontrada.");

        if (await _db.UnidadPersonas.AnyAsync(p => p.Unidad.ConsorcioId == id, ct))
            return Result.Fail("No se puede eliminar: la sociedad ya tiene residentes cargados.");

        var unidades = await _db.Unidades.Where(u => u.ConsorcioId == id).ToListAsync(ct);
        _db.Unidades.RemoveRange(unidades);
        _db.Consorcios.Remove(consorcio);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
