using Condolio.Application.Common;
using Condolio.Application.Paqueteria;
using Condolio.Domain.Paqueteria;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Paqueteria;

public class PaqueteriaService : IPaqueteriaService
{
    private readonly CondolioDbContext _db;

    public PaqueteriaService(CondolioDbContext db) => _db = db;

    public async Task<Result<ResumenPaqueteriaDto>> ResumenAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var hoy = DateTime.Now.Date;
        var manana = hoy.AddDays(1);
        var q = _db.Paquetes.IgnoreQueryFilters().Where(p => p.ConsorcioId == consorcioId);

        var porEntregar = await q.CountAsync(p => p.Estado == EstadoPaquete.EnRecepcion, ct);
        var llegaronHoy = await q.CountAsync(p => p.LlegadaUtc >= hoy && p.LlegadaUtc < manana, ct);
        var entregadosHoy = await q.CountAsync(p => p.EntregaUtc != null && p.EntregaUtc >= hoy && p.EntregaUtc < manana, ct);

        return Result<ResumenPaqueteriaDto>.Ok(new ResumenPaqueteriaDto(porEntregar, llegaronHoy, entregadosHoy));
    }

    public async Task<Result<PaquetesListaDto>> ListarAsync(
        Guid consorcioId, EstadoPaquete? estado, string? busqueda, int anio, int mes, CancellationToken ct = default)
    {
        var q = _db.Paquetes.IgnoreQueryFilters().Where(p => p.ConsorcioId == consorcioId);

        if (estado is { } e) q = q.Where(p => p.Estado == e);

        if (anio > 0 && mes > 0)
        {
            var desde = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var hasta = desde.AddMonths(1);
            q = q.Where(p => p.LlegadaUtc >= desde && p.LlegadaUtc < hasta);
        }

        var texto = (busqueda ?? "").Trim().ToLowerInvariant();
        if (texto.Length > 0)
            q = q.Where(p => p.UnidadNombre.ToLower().Contains(texto)
                || (p.Transportista ?? "").ToLower().Contains(texto)
                || (p.Descripcion ?? "").ToLower().Contains(texto));

        var lista = await q.OrderByDescending(p => p.LlegadaUtc)
            .Select(p => Map(p))
            .ToListAsync(ct);

        return Result<PaquetesListaDto>.Ok(new PaquetesListaDto(lista));
    }

    public async Task<Result<PaqueteDto>> ObtenerAsync(Guid consorcioId, Guid id, CancellationToken ct = default)
    {
        var p = await _db.Paquetes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        return p is null
            ? Result<PaqueteDto>.Fail("Paquete no encontrado.")
            : Result<PaqueteDto>.Ok(Map(p));
    }

    public async Task<Result<PaqueteDto>> RegistrarAsync(
        Guid consorcioId, RegistrarPaqueteDto dto, string registradoPor, CancellationToken ct = default)
    {
        var admin = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (admin is not { } adminId) return Result<PaqueteDto>.Fail("Consorcio no encontrado.");

        var unidad = await _db.Unidades.IgnoreQueryFilters()
            .Where(u => u.Id == dto.UnidadId && u.ConsorcioId == consorcioId)
            .Select(u => u.Nombre).FirstOrDefaultAsync(ct);
        if (unidad is null) return Result<PaqueteDto>.Fail("Elegí una unidad válida.");

        var cantidad = dto.Cantidad < 1 ? 1 : dto.Cantidad;
        var llegada = dto.LlegadaLocal is { } l
            ? DateTime.SpecifyKind(l, DateTimeKind.Unspecified)
            : DateTime.Now;

        var p = new Paquete
        {
            AdministradorId = adminId,
            ConsorcioId = consorcioId,
            UnidadId = dto.UnidadId,
            UnidadNombre = unidad,
            Tipo = dto.Tipo,
            Cantidad = cantidad,
            Transportista = string.IsNullOrWhiteSpace(dto.Transportista) ? null : dto.Transportista.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Estado = EstadoPaquete.EnRecepcion,
            LlegadaUtc = llegada,
            RegistradoPorNombre = registradoPor,
        };
        _db.Paquetes.Add(p);
        await _db.SaveChangesAsync(ct);

        return Result<PaqueteDto>.Ok(Map(p));
    }

    public async Task<Result<PaqueteDto>> EntregarAsync(
        Guid consorcioId, Guid id, EntregarPaqueteDto dto, string entregadoPor, CancellationToken ct = default)
    {
        var p = await _db.Paquetes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (p is null) return Result<PaqueteDto>.Fail("Paquete no encontrado.");
        if (p.Estado == EstadoPaquete.Entregado) return Result<PaqueteDto>.Fail("Este paquete ya fue entregado.");

        p.Estado = EstadoPaquete.Entregado;
        p.EntregaUtc = DateTime.Now;
        p.EntregadoPorNombre = entregadoPor;
        p.RetiradoPorNombre = string.IsNullOrWhiteSpace(dto.RetiradoPor) ? null : dto.RetiradoPor.Trim();
        await _db.SaveChangesAsync(ct);

        return Result<PaqueteDto>.Ok(Map(p));
    }

    private static PaqueteDto Map(Paquete p) => new(
        p.Id, p.UnidadId, p.UnidadNombre, p.Tipo, p.Cantidad, p.Transportista, p.Descripcion,
        p.Estado, p.LlegadaUtc, p.EntregaUtc, p.RegistradoPorNombre, p.EntregadoPorNombre, p.RetiradoPorNombre);
}
