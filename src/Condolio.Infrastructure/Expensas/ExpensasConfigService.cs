using Condolio.Application.Common;
using Condolio.Application.Expensas;
using Condolio.Domain.Expensas;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Expensas;

public class ExpensasConfigService : IExpensasConfigService
{
    private static readonly (string Nombre, TipoRubro Tipo)[] RubrosBase =
    {
        ("Sueldos y jornales", TipoRubro.Ordinario),
        ("Cargas sociales", TipoRubro.Ordinario),
        ("Honorarios administración", TipoRubro.Ordinario),
        ("Servicios (luz, agua, gas)", TipoRubro.Ordinario),
        ("Seguros", TipoRubro.Ordinario),
        ("Impuestos y tasas", TipoRubro.Ordinario),
        ("Mantenimiento y reparaciones", TipoRubro.Ordinario),
        ("Ascensores", TipoRubro.Ordinario),
        ("Limpieza e insumos", TipoRubro.Ordinario),
        ("Gastos bancarios", TipoRubro.Ordinario),
        ("Varios", TipoRubro.Ordinario),
        ("Obras y mejoras", TipoRubro.Extraordinario),
        ("Fondo de reserva", TipoRubro.FondoReserva),
    };

    private readonly CondolioDbContext _db;

    public ExpensasConfigService(CondolioDbContext db) => _db = db;

    // ============ Configuración ============

    public async Task<Result<ConfigExpensasDto>> ObtenerConfigAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var c = await CargarConfigAsync(consorcioId, ct);
        return c is null ? Result<ConfigExpensasDto>.Fail("Consorcio no encontrado.") : Result<ConfigExpensasDto>.Ok(MapConfig(c));
    }

    public async Task<Result<ConfigExpensasDto>> GuardarConfigAsync(Guid consorcioId, GuardarConfigExpensasDto dto, CancellationToken ct = default)
    {
        var c = await CargarConfigAsync(consorcioId, ct);
        if (c is null) return Result<ConfigExpensasDto>.Fail("Consorcio no encontrado.");

        c.DiaPrimerVencimiento = Math.Clamp(dto.DiaPrimerVencimiento, 1, 28);
        c.DiaSegundoVencimiento = dto.DiaSegundoVencimiento is { } d ? Math.Clamp(d, 1, 28) : null;
        c.RecargoSegundoVencimientoPct = Math.Max(0, dto.RecargoSegundoVencimientoPct);
        c.TasaInteresMoraMensualPct = Math.Max(0, dto.TasaInteresMoraMensualPct);
        c.FondoReservaTipo = dto.FondoReservaTipo;
        c.FondoReservaValor = Math.Max(0, dto.FondoReservaValor);
        c.InquilinoPagaOrdinarias = dto.InquilinoPagaOrdinarias;
        c.RedondearAlPeso = dto.RedondearAlPeso;
        await _db.SaveChangesAsync(ct);
        return Result<ConfigExpensasDto>.Ok(MapConfig(c));
    }

    public async Task<Result<ConfigExpensasDto>> GuardarMercadoPagoAsync(Guid consorcioId, GuardarMercadoPagoDto dto, CancellationToken ct = default)
    {
        var c = await CargarConfigAsync(consorcioId, ct);
        if (c is null) return Result<ConfigExpensasDto>.Fail("Consorcio no encontrado.");

        var token = dto.AccessToken?.Trim();
        c.MercadoPagoAccessToken = string.IsNullOrWhiteSpace(token) ? null : token;
        c.MercadoPagoPublicKey = string.IsNullOrWhiteSpace(dto.PublicKey) ? null : dto.PublicKey.Trim();
        c.MercadoPagoActivo = c.MercadoPagoAccessToken is not null;
        await _db.SaveChangesAsync(ct);
        return Result<ConfigExpensasDto>.Ok(MapConfig(c));
    }

    private async Task<ConfiguracionExpensas?> CargarConfigAsync(Guid consorcioId, CancellationToken ct)
    {
        var c = await _db.ConfiguracionExpensas.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.ConsorcioId == consorcioId, ct);
        if (c is not null) return c;

        var adminId = await _db.Consorcios.IgnoreQueryFilters()
            .Where(x => x.Id == consorcioId).Select(x => (Guid?)x.AdministradorId).FirstOrDefaultAsync(ct);
        if (adminId is not { } admin) return null;

        c = new ConfiguracionExpensas { AdministradorId = admin, ConsorcioId = consorcioId };
        _db.ConfiguracionExpensas.Add(c);

        // Rubros base la primera vez.
        if (!await _db.RubrosGasto.IgnoreQueryFilters().AnyAsync(r => r.ConsorcioId == consorcioId, ct))
        {
            var orden = 0;
            foreach (var (nombre, tipo) in RubrosBase)
                _db.RubrosGasto.Add(new RubroGasto
                {
                    AdministradorId = admin, ConsorcioId = consorcioId,
                    Nombre = nombre, Tipo = tipo, Orden = orden++, EsSistema = true,
                });
        }

        await _db.SaveChangesAsync(ct);
        return c;
    }

    private static ConfigExpensasDto MapConfig(ConfiguracionExpensas c) => new(
        c.DiaPrimerVencimiento, c.DiaSegundoVencimiento, c.RecargoSegundoVencimientoPct,
        c.TasaInteresMoraMensualPct, c.FondoReservaTipo, c.FondoReservaValor,
        c.InquilinoPagaOrdinarias, c.RedondearAlPeso, c.MercadoPagoActivo,
        c.MercadoPagoAccessToken is { Length: > 8 } t ? $"{t[..4]}…{t[^4..]}" : c.MercadoPagoAccessToken is null ? null : "configurado");

    // ============ Rubros ============

    public async Task<Result<IReadOnlyList<RubroGastoDto>>> ListarRubrosAsync(Guid consorcioId, CancellationToken ct = default)
    {
        await CargarConfigAsync(consorcioId, ct);
        var lista = await _db.RubrosGasto.Where(r => r.ConsorcioId == consorcioId)
            .OrderBy(r => r.Tipo).ThenBy(r => r.Orden).ThenBy(r => r.Nombre)
            .Select(r => new RubroGastoDto(r.Id, r.Nombre, r.Tipo, r.Orden, r.EsSistema))
            .ToListAsync(ct);
        return Result<IReadOnlyList<RubroGastoDto>>.Ok(lista);
    }

    public async Task<Result<RubroGastoDto>> CrearRubroAsync(Guid consorcioId, GuardarRubroDto dto, CancellationToken ct = default)
    {
        await CargarConfigAsync(consorcioId, ct);
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<RubroGastoDto>.Fail("El nombre es obligatorio.");
        var adminId = await AdminIdAsync(consorcioId, ct);
        var maxOrden = await _db.RubrosGasto.Where(r => r.ConsorcioId == consorcioId).Select(r => (int?)r.Orden).MaxAsync(ct) ?? 0;
        var r = new RubroGasto
        {
            AdministradorId = adminId, ConsorcioId = consorcioId,
            Nombre = dto.Nombre.Trim(), Tipo = dto.Tipo, Orden = maxOrden + 1,
        };
        _db.RubrosGasto.Add(r);
        await _db.SaveChangesAsync(ct);
        return Result<RubroGastoDto>.Ok(new RubroGastoDto(r.Id, r.Nombre, r.Tipo, r.Orden, false));
    }

    public async Task<Result<RubroGastoDto>> ActualizarRubroAsync(Guid consorcioId, Guid id, GuardarRubroDto dto, CancellationToken ct = default)
    {
        var r = await _db.RubrosGasto.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (r is null) return Result<RubroGastoDto>.Fail("Rubro no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<RubroGastoDto>.Fail("El nombre es obligatorio.");
        r.Nombre = dto.Nombre.Trim();
        if (!r.EsSistema) r.Tipo = dto.Tipo;
        await _db.SaveChangesAsync(ct);
        return Result<RubroGastoDto>.Ok(new RubroGastoDto(r.Id, r.Nombre, r.Tipo, r.Orden, r.EsSistema));
    }

    public async Task<Result> EliminarRubroAsync(Guid consorcioId, Guid id, CancellationToken ct = default)
    {
        var r = await _db.RubrosGasto.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (r is null) return Result.Fail("Rubro no encontrado.");
        if (r.EsSistema) return Result.Fail("Los rubros del sistema no se pueden eliminar.");
        if (await _db.GastosFijos.AnyAsync(g => g.RubroGastoId == id, ct)
            || await _db.Empleados.AnyAsync(g => g.RubroGastoId == id, ct))
            return Result.Fail("El rubro está en uso por gastos o empleados.");
        _db.RubrosGasto.Remove(r);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ============ Proveedores ============

    public async Task<Result<ProveedoresListaDto>> ListarProveedoresAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var lista = await _db.Proveedores.Where(p => p.ConsorcioId == consorcioId)
            .OrderByDescending(p => p.Recomendado).ThenByDescending(p => p.Activo).ThenBy(p => p.Nombre)
            .Select(p => MapProveedor(p))
            .ToListAsync(ct);
        return Result<ProveedoresListaDto>.Ok(new ProveedoresListaDto(
            lista, lista.Count, lista.Count(p => p.Activo), lista.Count(p => !p.Activo), lista.Count(p => p.Recomendado)));
    }

    public async Task<Result<ProveedorDto>> CrearProveedorAsync(Guid consorcioId, GuardarProveedorDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<ProveedorDto>.Fail("El nombre es obligatorio.");
        var adminId = await AdminIdAsync(consorcioId, ct);
        var p = new Proveedor { AdministradorId = adminId, ConsorcioId = consorcioId };
        AplicarProveedor(p, dto);
        _db.Proveedores.Add(p);
        await _db.SaveChangesAsync(ct);
        return Result<ProveedorDto>.Ok(MapProveedor(p));
    }

    public async Task<Result<ProveedorDto>> ActualizarProveedorAsync(Guid consorcioId, Guid id, GuardarProveedorDto dto, CancellationToken ct = default)
    {
        var p = await _db.Proveedores.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (p is null) return Result<ProveedorDto>.Fail("Proveedor no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<ProveedorDto>.Fail("El nombre es obligatorio.");
        AplicarProveedor(p, dto);
        await _db.SaveChangesAsync(ct);
        return Result<ProveedorDto>.Ok(MapProveedor(p));
    }

    public async Task<Result> CambiarEstadoProveedorAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default)
    {
        var p = await _db.Proveedores.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (p is null) return Result.Fail("Proveedor no encontrado.");
        p.Activo = activo;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> CambiarRecomendadoProveedorAsync(Guid consorcioId, Guid id, bool recomendado, CancellationToken ct = default)
    {
        var p = await _db.Proveedores.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (p is null) return Result.Fail("Proveedor no encontrado.");
        p.Recomendado = recomendado;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static void AplicarProveedor(Proveedor p, GuardarProveedorDto d)
    {
        p.Nombre = d.Nombre.Trim();
        p.Empresa = Limpiar(d.Empresa);
        p.Rubro = Limpiar(d.Rubro);
        p.Cuit = Limpiar(d.Cuit);
        p.Email = Limpiar(d.Email);
        p.Telefono = Limpiar(d.Telefono);
        p.TelefonoAlt = Limpiar(d.TelefonoAlt);
        p.Direccion = Limpiar(d.Direccion);
        p.SitioWeb = Limpiar(d.SitioWeb);
        p.Cbu = Limpiar(d.Cbu);
        p.Alias = Limpiar(d.Alias);
        p.Horario = Limpiar(d.Horario);
        p.Notas = Limpiar(d.Notas);
        p.Recomendado = d.Recomendado;
    }

    private static ProveedorDto MapProveedor(Proveedor p) => new(
        p.Id, p.Nombre, p.Empresa, p.Rubro, p.Cuit, p.Email, p.Telefono, p.TelefonoAlt,
        p.Direccion, p.SitioWeb, p.Cbu, p.Alias, p.Horario, p.Notas, p.Activo, p.Recomendado);

    // ============ Gastos fijos + empleados ============

    public async Task<Result<GastosFijosResumenDto>> ResumenGastosFijosAsync(Guid consorcioId, CancellationToken ct = default)
    {
        await CargarConfigAsync(consorcioId, ct);

        var empleados = await _db.Empleados.Where(e => e.ConsorcioId == consorcioId)
            .OrderByDescending(e => e.Activo).ThenBy(e => e.Apellido).ToListAsync(ct);

        var gastos = await _db.GastosFijos.Where(g => g.ConsorcioId == consorcioId)
            .OrderByDescending(g => g.Activo).ThenBy(g => g.Descripcion).ToListAsync(ct);

        var rubros = await _db.RubrosGasto.Where(r => r.ConsorcioId == consorcioId)
            .ToDictionaryAsync(r => r.Id, r => r.Nombre, ct);
        var provs = await _db.Proveedores.Where(p => p.ConsorcioId == consorcioId)
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, ct);

        var empDto = empleados.Select(MapEmpleado).ToList();
        var gasDto = gastos.Select(g => new GastoFijoDto(
            g.Id, g.Descripcion, g.RubroGastoId, rubros.GetValueOrDefault(g.RubroGastoId, "—"),
            g.ProveedorId, g.ProveedorId is { } pid ? provs.GetValueOrDefault(pid) : null,
            g.MontoEstimado, g.CriterioDistribucion, g.Activo, g.Notas)).ToList();

        var totalEmp = empDto.Where(e => e.Activo).Sum(e => e.CostoMensualTotal);
        var totalGas = gasDto.Where(g => g.Activo).Sum(g => g.MontoEstimado);

        return Result<GastosFijosResumenDto>.Ok(new GastosFijosResumenDto(
            empDto, gasDto, totalEmp, totalGas, totalEmp + totalGas));
    }

    public async Task<Result<EmpleadoDto>> CrearEmpleadoAsync(Guid consorcioId, GuardarEmpleadoDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            return Result<EmpleadoDto>.Fail("Nombre y apellido son obligatorios.");
        var adminId = await AdminIdAsync(consorcioId, ct);
        var e = new Empleado { AdministradorId = adminId, ConsorcioId = consorcioId };
        AplicarEmpleado(e, dto);
        _db.Empleados.Add(e);
        await _db.SaveChangesAsync(ct);
        return Result<EmpleadoDto>.Ok(MapEmpleado(e));
    }

    public async Task<Result<EmpleadoDto>> ActualizarEmpleadoAsync(Guid consorcioId, Guid id, GuardarEmpleadoDto dto, CancellationToken ct = default)
    {
        var e = await _db.Empleados.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result<EmpleadoDto>.Fail("Empleado no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            return Result<EmpleadoDto>.Fail("Nombre y apellido son obligatorios.");
        AplicarEmpleado(e, dto);
        await _db.SaveChangesAsync(ct);
        return Result<EmpleadoDto>.Ok(MapEmpleado(e));
    }

    public async Task<Result> CambiarEstadoEmpleadoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default)
    {
        var e = await _db.Empleados.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (e is null) return Result.Fail("Empleado no encontrado.");
        e.Activo = activo;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static void AplicarEmpleado(Empleado e, GuardarEmpleadoDto d)
    {
        e.Nombre = d.Nombre.Trim();
        e.Apellido = d.Apellido.Trim();
        e.Cuil = Limpiar(d.Cuil);
        e.Categoria = Limpiar(d.Categoria);
        e.SueldoBasico = Math.Max(0, d.SueldoBasico);
        e.CargasSocialesPct = Math.Max(0, d.CargasSocialesPct);
        e.ProvisionaAguinaldo = d.ProvisionaAguinaldo;
        e.OtrosConceptosMensuales = Math.Max(0, d.OtrosConceptosMensuales);
        e.RubroGastoId = d.RubroGastoId;
        e.FechaIngreso = d.FechaIngreso;
        e.Notas = Limpiar(d.Notas);
    }

    private static EmpleadoDto MapEmpleado(Empleado e) => new(
        e.Id, e.Nombre, e.Apellido, e.Cuil, e.Categoria, e.SueldoBasico, e.CargasSocialesPct,
        e.ProvisionaAguinaldo, e.OtrosConceptosMensuales, e.RubroGastoId, e.FechaIngreso,
        e.Activo, e.Notas, e.CostoMensualTotal);

    public async Task<Result<GastoFijoDto>> CrearGastoFijoAsync(Guid consorcioId, GuardarGastoFijoDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Descripcion)) return Result<GastoFijoDto>.Fail("La descripción es obligatoria.");
        if (!await _db.RubrosGasto.AnyAsync(r => r.Id == dto.RubroGastoId && r.ConsorcioId == consorcioId, ct))
            return Result<GastoFijoDto>.Fail("Elegí un rubro válido.");
        var adminId = await AdminIdAsync(consorcioId, ct);
        var g = new GastoFijo { AdministradorId = adminId, ConsorcioId = consorcioId };
        AplicarGastoFijo(g, dto);
        _db.GastosFijos.Add(g);
        await _db.SaveChangesAsync(ct);
        return await MapGastoFijoAsync(consorcioId, g, ct);
    }

    public async Task<Result<GastoFijoDto>> ActualizarGastoFijoAsync(Guid consorcioId, Guid id, GuardarGastoFijoDto dto, CancellationToken ct = default)
    {
        var g = await _db.GastosFijos.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (g is null) return Result<GastoFijoDto>.Fail("Gasto fijo no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Descripcion)) return Result<GastoFijoDto>.Fail("La descripción es obligatoria.");
        AplicarGastoFijo(g, dto);
        await _db.SaveChangesAsync(ct);
        return await MapGastoFijoAsync(consorcioId, g, ct);
    }

    public async Task<Result> CambiarEstadoGastoFijoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default)
    {
        var g = await _db.GastosFijos.FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (g is null) return Result.Fail("Gasto fijo no encontrado.");
        g.Activo = activo;
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    private static void AplicarGastoFijo(GastoFijo g, GuardarGastoFijoDto d)
    {
        g.Descripcion = d.Descripcion.Trim();
        g.RubroGastoId = d.RubroGastoId;
        g.ProveedorId = d.ProveedorId;
        g.MontoEstimado = Math.Max(0, d.MontoEstimado);
        g.CriterioDistribucion = d.CriterioDistribucion;
        g.Notas = Limpiar(d.Notas);
    }

    private async Task<Result<GastoFijoDto>> MapGastoFijoAsync(Guid consorcioId, GastoFijo g, CancellationToken ct)
    {
        var rubro = await _db.RubrosGasto.Where(r => r.Id == g.RubroGastoId).Select(r => r.Nombre).FirstOrDefaultAsync(ct) ?? "—";
        string? prov = g.ProveedorId is { } pid
            ? await _db.Proveedores.Where(p => p.Id == pid).Select(p => p.Nombre).FirstOrDefaultAsync(ct)
            : null;
        return Result<GastoFijoDto>.Ok(new GastoFijoDto(
            g.Id, g.Descripcion, g.RubroGastoId, rubro, g.ProveedorId, prov,
            g.MontoEstimado, g.CriterioDistribucion, g.Activo, g.Notas));
    }

    // ============ helpers ============

    private async Task<Guid> AdminIdAsync(Guid consorcioId, CancellationToken ct) =>
        await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => c.AdministradorId).FirstOrDefaultAsync(ct);

    private static string? Limpiar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
