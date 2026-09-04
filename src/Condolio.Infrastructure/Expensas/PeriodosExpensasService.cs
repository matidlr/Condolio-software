using Condolio.Application.Common;
using Condolio.Application.Expensas;
using Condolio.Domain.Expensas;
using Condolio.Infrastructure.Archivos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Expensas;

public class PeriodosExpensasService : IPeriodosExpensasService
{
    private readonly CondolioDbContext _db;
    private readonly IFileStorage _storage;

    public PeriodosExpensasService(CondolioDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    // ============ Períodos ============

    public async Task<Result<IReadOnlyList<PeriodoResumenDto>>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var lista = await _db.PeriodosExpensas.Where(p => p.ConsorcioId == consorcioId)
            .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
            .Select(p => new PeriodoResumenDto(
                p.Id, p.Anio, p.Mes, p.Estado, p.FechaLiquidacion,
                p.Gastos.Count, p.Gastos.Sum(g => (decimal?)g.Monto) ?? 0m))
            .ToListAsync(ct);
        return Result<IReadOnlyList<PeriodoResumenDto>>.Ok(lista);
    }

    public async Task<Result<PeriodoDetalleDto>> AbrirAsync(Guid consorcioId, AbrirPeriodoDto dto, CancellationToken ct = default)
    {
        if (dto.Mes is < 1 or > 12) return Result<PeriodoDetalleDto>.Fail("Mes inválido.");
        if (dto.Anio is < 2020 or > 2100) return Result<PeriodoDetalleDto>.Fail("Año inválido.");
        if (await _db.PeriodosExpensas.AnyAsync(p => p.ConsorcioId == consorcioId && p.Anio == dto.Anio && p.Mes == dto.Mes, ct))
            return Result<PeriodoDetalleDto>.Fail($"Ya existe el período {dto.Mes:00}/{dto.Anio}.");

        var adminId = await AdminIdAsync(consorcioId, ct);
        if (adminId == Guid.Empty) return Result<PeriodoDetalleDto>.Fail("Consorcio no encontrado.");

        var periodo = new PeriodoExpensas
        {
            AdministradorId = adminId, ConsorcioId = consorcioId, Anio = dto.Anio, Mes = dto.Mes,
        };
        _db.PeriodosExpensas.Add(periodo);
        await _db.SaveChangesAsync(ct);

        var primerDia = new DateOnly(dto.Anio, dto.Mes, 1);
        var rubroSueldos = await _db.RubrosGasto
            .Where(r => r.ConsorcioId == consorcioId && r.Nombre.StartsWith("Sueldos"))
            .Select(r => (Guid?)r.Id).FirstOrDefaultAsync(ct);

        var empleados = await _db.Empleados.Where(x => x.ConsorcioId == consorcioId && x.Activo).ToListAsync(ct);
        var gastosFijos = await _db.GastosFijos.Where(x => x.ConsorcioId == consorcioId && x.Activo).ToListAsync(ct);

        var lineas = new List<GastoPeriodo>();
        foreach (var e in empleados)
            lineas.Add(new GastoPeriodo
            {
                AdministradorId = adminId, ConsorcioId = consorcioId, PeriodoExpensasId = periodo.Id,
                RubroGastoId = e.RubroGastoId ?? rubroSueldos ?? Guid.Empty,
                Descripcion = $"{e.Nombre} {e.Apellido}".Trim(),
                Monto = e.CostoMensualTotal, Fecha = primerDia,
                Origen = OrigenGasto.Empleado, EmpleadoId = e.Id,
            });
        foreach (var g in gastosFijos)
            lineas.Add(new GastoPeriodo
            {
                AdministradorId = adminId, ConsorcioId = consorcioId, PeriodoExpensasId = periodo.Id,
                RubroGastoId = g.RubroGastoId, ProveedorId = g.ProveedorId,
                Descripcion = g.Descripcion, Monto = g.MontoEstimado, Fecha = primerDia,
                Origen = OrigenGasto.GastoFijo, GastoFijoId = g.Id,
                CriterioDistribucion = g.CriterioDistribucion,
            });

        if (lineas.Count > 0)
        {
            _db.GastosPeriodo.AddRange(lineas.Where(l => l.RubroGastoId != Guid.Empty));
            await _db.SaveChangesAsync(ct);
        }

        return await ObtenerAsync(consorcioId, periodo.Id, ct);
    }

    public async Task<Result<PeriodoDetalleDto>> ObtenerAsync(Guid consorcioId, Guid periodoId, CancellationToken ct = default)
    {
        var periodo = await _db.PeriodosExpensas
            .Include(p => p.Gastos).ThenInclude(g => g.Unidades)
            .FirstOrDefaultAsync(p => p.Id == periodoId && p.ConsorcioId == consorcioId, ct);
        if (periodo is null) return Result<PeriodoDetalleDto>.Fail("Período no encontrado.");

        var rubros = await _db.RubrosGasto.Where(r => r.ConsorcioId == consorcioId)
            .ToDictionaryAsync(r => r.Id, r => (r.Nombre, r.Tipo), ct);
        var provs = await _db.Proveedores.Where(p => p.ConsorcioId == consorcioId)
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);
        var extras = await _db.Extraordinarias.Where(x => x.ConsorcioId == consorcioId)
            .ToDictionaryAsync(x => x.Id, x => x.Titulo, ct);

        var gastos = periodo.Gastos.OrderBy(g => g.Fecha).ThenBy(g => g.Descripcion).Select(g =>
        {
            var (rn, rt) = rubros.GetValueOrDefault(g.RubroGastoId, ("—", TipoRubro.Ordinario));
            return new GastoPeriodoDto(
                g.Id, g.RubroGastoId, rn, rt,
                g.ProveedorId, g.ProveedorId is { } pid ? provs.GetValueOrDefault(pid) : null,
                g.Descripcion, g.Monto, g.Fecha, g.MetodoPago, g.CuentaPago, g.ComprobanteRuta is not null,
                g.Origen, g.Alcance, g.CriterioDistribucion,
                g.ExtraordinariaId, g.ExtraordinariaId is { } eid ? extras.GetValueOrDefault(eid) : null,
                g.Unidades.Select(u => u.UnidadId).ToList());
        }).ToList();

        var porRubro = gastos.GroupBy(g => g.RubroGastoId)
            .Select(gr => new TotalRubroDto(gr.Key, gr.First().RubroNombre, gr.First().TipoRubro, gr.Sum(x => x.Monto)))
            .OrderBy(r => r.Tipo).ThenBy(r => r.Rubro).ToList();

        decimal ordinario = gastos.Where(g => g.ExtraordinariaId is null && g.TipoRubro == TipoRubro.Ordinario).Sum(g => g.Monto);
        decimal extraordinario = gastos.Where(g => g.ExtraordinariaId is null && g.TipoRubro == TipoRubro.Extraordinario).Sum(g => g.Monto);
        decimal fondo = gastos.Where(g => g.ExtraordinariaId is null && g.TipoRubro == TipoRubro.FondoReserva).Sum(g => g.Monto);
        decimal imputado = gastos.Where(g => g.ExtraordinariaId is not null).Sum(g => g.Monto);

        return Result<PeriodoDetalleDto>.Ok(new PeriodoDetalleDto(
            periodo.Id, periodo.Anio, periodo.Mes, periodo.Estado, periodo.FechaLiquidacion, periodo.Notas,
            gastos, porRubro, ordinario, extraordinario, fondo, imputado,
            ordinario + extraordinario + fondo));
    }

    public async Task<Result> ReabrirAsync(Guid consorcioId, Guid periodoId, CancellationToken ct = default)
    {
        var periodo = await _db.PeriodosExpensas.FirstOrDefaultAsync(p => p.Id == periodoId && p.ConsorcioId == consorcioId, ct);
        if (periodo is null) return Result.Fail("Período no encontrado.");
        if (periodo.Estado == EstadoPeriodo.Cerrado) return Result.Fail("El período está cerrado.");
        periodo.Estado = EstadoPeriodo.Abierto;
        periodo.FechaLiquidacion = null;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ============ Gastos del período ============

    public async Task<Result<GastoPeriodoDto>> CrearGastoAsync(Guid consorcioId, Guid periodoId, GuardarGastoPeriodoDto dto, CancellationToken ct = default)
    {
        var periodo = await _db.PeriodosExpensas.FirstOrDefaultAsync(p => p.Id == periodoId && p.ConsorcioId == consorcioId, ct);
        if (periodo is null) return Result<GastoPeriodoDto>.Fail("Período no encontrado.");
        if (periodo.Estado != EstadoPeriodo.Abierto) return Result<GastoPeriodoDto>.Fail("El período no está abierto.");

        var err = await ValidarGastoAsync(consorcioId, dto, ct);
        if (err is not null) return Result<GastoPeriodoDto>.Fail(err);

        var adminId = periodo.AdministradorId;
        var g = new GastoPeriodo
        {
            AdministradorId = adminId, ConsorcioId = consorcioId, PeriodoExpensasId = periodoId,
            Origen = OrigenGasto.Unico,
        };
        AplicarGasto(g, dto);
        _db.GastosPeriodo.Add(g);
        await _db.SaveChangesAsync(ct);
        await SincronizarUnidadesAsync(g, dto, adminId, consorcioId, ct);

        return await MapGastoAsync(consorcioId, g.Id, ct);
    }

    public async Task<Result<GastoPeriodoDto>> ActualizarGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, GuardarGastoPeriodoDto dto, CancellationToken ct = default)
    {
        var g = await _db.GastosPeriodo
            .FirstOrDefaultAsync(x => x.Id == gastoId && x.PeriodoExpensasId == periodoId && x.ConsorcioId == consorcioId, ct);
        if (g is null) return Result<GastoPeriodoDto>.Fail("Gasto no encontrado.");
        var periodo = await _db.PeriodosExpensas.FirstAsync(p => p.Id == periodoId, ct);
        if (periodo.Estado != EstadoPeriodo.Abierto) return Result<GastoPeriodoDto>.Fail("El período no está abierto.");

        var err = await ValidarGastoAsync(consorcioId, dto, ct);
        if (err is not null) return Result<GastoPeriodoDto>.Fail(err);

        AplicarGasto(g, dto);
        await _db.GastoPeriodoUnidades.Where(u => u.GastoPeriodoId == gastoId).ExecuteDeleteAsync(ct);
        await _db.SaveChangesAsync(ct);
        await SincronizarUnidadesAsync(g, dto, g.AdministradorId, consorcioId, ct);

        return await MapGastoAsync(consorcioId, gastoId, ct);
    }

    public async Task<Result> EliminarGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, CancellationToken ct = default)
    {
        var g = await _db.GastosPeriodo
            .FirstOrDefaultAsync(x => x.Id == gastoId && x.PeriodoExpensasId == periodoId && x.ConsorcioId == consorcioId, ct);
        if (g is null) return Result.Fail("Gasto no encontrado.");
        var estado = await _db.PeriodosExpensas.Where(p => p.Id == periodoId).Select(p => p.Estado).FirstAsync(ct);
        if (estado != EstadoPeriodo.Abierto) return Result.Fail("El período no está abierto.");

        if (g.ComprobanteRuta is not null) _storage.Eliminar(g.ComprobanteRuta);
        _db.GastosPeriodo.Remove(g);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private async Task<string?> ValidarGastoAsync(Guid consorcioId, GuardarGastoPeriodoDto d, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(d.Descripcion)) return "La descripción es obligatoria.";
        if (d.Monto < 0) return "El monto no puede ser negativo.";
        if (!await _db.RubrosGasto.AnyAsync(r => r.Id == d.RubroGastoId && r.ConsorcioId == consorcioId, ct))
            return "Elegí un rubro válido.";
        if (d.ProveedorId is { } pid && !await _db.Proveedores.AnyAsync(p => p.Id == pid && p.ConsorcioId == consorcioId, ct))
            return "El proveedor no pertenece al consorcio.";
        if (d.ExtraordinariaId is { } xid && !await _db.Extraordinarias.AnyAsync(x => x.Id == xid && x.ConsorcioId == consorcioId, ct))
            return "La extraordinaria no pertenece al consorcio.";
        if (d.Alcance == AlcanceGasto.Subconjunto)
        {
            var ids = (d.UnidadIds ?? Array.Empty<Guid>()).Distinct().ToList();
            if (ids.Count == 0) return "Elegí al menos una unidad.";
            var validas = await _db.Unidades.CountAsync(u => u.ConsorcioId == consorcioId && ids.Contains(u.Id), ct);
            if (validas != ids.Count) return "Alguna unidad no pertenece al consorcio.";
        }
        return null;
    }

    private static void AplicarGasto(GastoPeriodo g, GuardarGastoPeriodoDto d)
    {
        g.RubroGastoId = d.RubroGastoId;
        g.ProveedorId = d.ProveedorId;
        g.Descripcion = d.Descripcion.Trim();
        g.Monto = Math.Max(0, d.Monto);
        g.Fecha = d.Fecha;
        g.MetodoPago = Limpiar(d.MetodoPago);
        g.CuentaPago = Limpiar(d.CuentaPago);
        g.Alcance = d.Alcance;
        g.CriterioDistribucion = d.CriterioDistribucion;
        g.ExtraordinariaId = d.ExtraordinariaId;
    }

    private async Task SincronizarUnidadesAsync(GastoPeriodo g, GuardarGastoPeriodoDto dto, Guid adminId, Guid consorcioId, CancellationToken ct)
    {
        if (dto.Alcance != AlcanceGasto.Subconjunto || dto.UnidadIds is null) return;
        _db.GastoPeriodoUnidades.AddRange(dto.UnidadIds.Distinct().Select(uid => new GastoPeriodoUnidad
        {
            AdministradorId = adminId, ConsorcioId = consorcioId, GastoPeriodoId = g.Id, UnidadId = uid,
        }));
        await _db.SaveChangesAsync(ct);
    }

    private async Task<Result<GastoPeriodoDto>> MapGastoAsync(Guid consorcioId, Guid gastoId, CancellationToken ct)
    {
        var g = await _db.GastosPeriodo.Include(x => x.Unidades).AsNoTracking().FirstAsync(x => x.Id == gastoId, ct);
        var rubro = await _db.RubrosGasto.Where(r => r.Id == g.RubroGastoId)
            .Select(r => new { r.Nombre, r.Tipo }).FirstOrDefaultAsync(ct);
        string? prov = g.ProveedorId is { } pid
            ? await _db.Proveedores.Where(p => p.Id == pid).Select(p => p.Nombre).FirstOrDefaultAsync(ct) : null;
        string? extra = g.ExtraordinariaId is { } eid
            ? await _db.Extraordinarias.Where(x => x.Id == eid).Select(x => x.Titulo).FirstOrDefaultAsync(ct) : null;

        return Result<GastoPeriodoDto>.Ok(new GastoPeriodoDto(
            g.Id, g.RubroGastoId, rubro?.Nombre ?? "—", rubro?.Tipo ?? TipoRubro.Ordinario,
            g.ProveedorId, prov, g.Descripcion, g.Monto, g.Fecha, g.MetodoPago, g.CuentaPago,
            g.ComprobanteRuta is not null, g.Origen, g.Alcance, g.CriterioDistribucion,
            g.ExtraordinariaId, extra, g.Unidades.Select(u => u.UnidadId).ToList()));
    }

    // ============ Comprobantes ============

    public async Task<Result> GuardarComprobanteGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, Stream contenido, string extension, CancellationToken ct = default)
    {
        var g = await _db.GastosPeriodo
            .FirstOrDefaultAsync(x => x.Id == gastoId && x.PeriodoExpensasId == periodoId && x.ConsorcioId == consorcioId, ct);
        if (g is null) return Result.Fail("Gasto no encontrado.");

        var ext = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf")) return Result.Fail("Formato no soportado (jpg, png, webp o pdf).");

        var ruta = $"expensas/{consorcioId:N}/gastos/{gastoId:N}{ext}";
        if (g.ComprobanteRuta is not null && g.ComprobanteRuta != ruta) _storage.Eliminar(g.ComprobanteRuta);
        await _storage.GuardarAsync(ruta, contenido, ct);
        g.ComprobanteRuta = ruta;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<(Stream Contenido, string ContentType)>> AbrirComprobanteGastoAsync(Guid consorcioId, Guid periodoId, Guid gastoId, CancellationToken ct = default)
    {
        var ruta = await _db.GastosPeriodo
            .Where(x => x.Id == gastoId && x.PeriodoExpensasId == periodoId && x.ConsorcioId == consorcioId)
            .Select(x => x.ComprobanteRuta).FirstOrDefaultAsync(ct);
        if (ruta is null || !_storage.Existe(ruta)) return Result<(Stream, string)>.Fail("No hay comprobante.");
        var tipo = Path.GetExtension(ruta).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg",
        };
        return Result<(Stream, string)>.Ok((_storage.Abrir(ruta), tipo));
    }

    // ============ helpers ============

    private async Task<Guid> AdminIdAsync(Guid consorcioId, CancellationToken ct) =>
        await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => c.AdministradorId).FirstOrDefaultAsync(ct);

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
