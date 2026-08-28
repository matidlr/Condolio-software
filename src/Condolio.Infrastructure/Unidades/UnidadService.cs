using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Application.Unidades;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Unidades;

public class UnidadService : IUnidadService
{
    private readonly CondolioDbContext _db;
    private readonly ISuscripcionService _suscripciones;
    private readonly IActividadUnidadService _actividad;

    public UnidadService(CondolioDbContext db, ISuscripcionService suscripciones, IActividadUnidadService actividad)
    {
        _db = db;
        _suscripciones = suscripciones;
        _actividad = actividad;
    }

    public async Task<Result<IReadOnlyList<UnidadDto>>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        if (!await ConsorcioExiste(consorcioId, ct))
            return Result<IReadOnlyList<UnidadDto>>.Fail("Consorcio no encontrado.");

        var unidades = await _db.Unidades
            .Where(u => u.ConsorcioId == consorcioId)
            .Include(u => u.Personas)
            .OrderBy(u => u.Piso).ThenBy(u => u.Nombre)
            .ToListAsync(ct);

        var lista = unidades.Select(Proyectar).ToList();
        return Result<IReadOnlyList<UnidadDto>>.Ok(lista);
    }

    public async Task<Result<UnidadDetalleDto>> ObtenerAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Consorcio)
            .Include(u => u.Personas)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result<UnidadDetalleDto>.Fail("Unidad no encontrada.");

        var personas = unidad.Personas
            .OrderByDescending(p => p.EsContactoPrincipal)
            .ThenBy(p => p.Rol)
            .Select(p => new PersonaUnidadDto(
                p.Id, p.Nombre, p.Apellido, p.Email, p.Telefono, p.Rol,
                p.EsContactoPrincipal, p.UsuarioId != null))
            .ToList();

        var ocupacionEfectiva = unidad.Personas.Any(p => p.Rol == RolUnidad.Inquilino) ? "Inquilino"
            : unidad.Personas.Any(p => p.Rol == RolUnidad.Propietario) ? "Propietario"
            : "Vacante";

        return Result<UnidadDetalleDto>.Ok(new UnidadDetalleDto(
            unidad.Id,
            unidad.ConsorcioId,
            unidad.Consorcio.Nombre,
            unidad.Nombre,
            unidad.Piso,
            unidad.Tipo,
            unidad.Ocupacion,
            unidad.InquilinosVenFinanzas,
            unidad.AreaM2,
            unidad.Seccion,
            unidad.CuotaMantenimiento,
            unidad.Coeficiente,
            unidad.Facturable,
            ocupacionEfectiva,
            unidad.Personas.Count(p => p.Rol != RolUnidad.Gestor),
            0m, // TODO: saldo real cuando exista el módulo de expensas
            unidad.CuotaMantenimiento is null
                || !unidad.Personas.Any(p => p.Rol != RolUnidad.Gestor),
            personas));
    }

    /// <summary>
    /// Importación por CSV: reemplazo total. Borra todas las unidades del consorcio (y sus datos
    /// asociados) y las recrea desde el archivo. Coincide con el comportamiento de Koti.
    /// </summary>
    public async Task<Result<ImportarUnidadesResultado>> ImportarAsync(Guid consorcioId, ImportarUnidadesDto dto, CancellationToken ct = default)
    {
        if (!await ConsorcioExiste(consorcioId, ct))
            return Result<ImportarUnidadesResultado>.Fail("Consorcio no encontrado.");

        var entrantes = dto.Unidades
            .Where(u => !string.IsNullOrWhiteSpace(u.Nombre))
            .GroupBy(u => u.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        if (entrantes.Count == 0)
            return Result<ImportarUnidadesResultado>.Fail("El archivo no tiene unidades válidas.");

        var actuales = await _db.Unidades.Where(u => u.ConsorcioId == consorcioId).ToListAsync(ct);
        _db.Unidades.RemoveRange(actuales);

        _db.Unidades.AddRange(entrantes.Select(src => new Unidad
        {
            ConsorcioId = consorcioId,
            Nombre = src.Nombre.Trim(),
            Piso = src.Piso,
            Tipo = src.Tipo,
            AreaM2 = src.AreaM2,
            Seccion = string.IsNullOrWhiteSpace(src.Seccion) ? null : src.Seccion.Trim(),
            CuotaMantenimiento = src.CuotaMantenimiento,
            Coeficiente = src.Coeficiente,
            Facturable = src.Facturable,
        }));

        await _db.SaveChangesAsync(ct);
        await RecalcularSuscripcion(ct);

        return Result<ImportarUnidadesResultado>.Ok(
            new ImportarUnidadesResultado(entrantes.Count, 0, actuales.Count, entrantes.Count));
    }

    public async Task<Result<int>> EditarMasivoAsync(Guid consorcioId, EdicionMasivaDto dto, CancellationToken ct = default)
    {
        if (dto.Items.Count == 0) return Result<int>.Ok(0);

        var ids = dto.Items.Select(i => i.Id).ToList();
        var unidades = await _db.Unidades
            .Where(u => u.ConsorcioId == consorcioId && ids.Contains(u.Id))
            .ToListAsync(ct);
        var porId = unidades.ToDictionary(u => u.Id);

        // Validación de nombres duplicados dentro del consorcio tras aplicar los cambios.
        var nombresFinal = await _db.Unidades
            .Where(u => u.ConsorcioId == consorcioId && !ids.Contains(u.Id))
            .Select(u => u.Nombre)
            .ToListAsync(ct);
        var set = new HashSet<string>(nombresFinal, StringComparer.OrdinalIgnoreCase);

        var cambiadas = 0;
        foreach (var item in dto.Items)
        {
            if (!porId.TryGetValue(item.Id, out var u)) continue;
            var nombre = item.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return Result<int>.Fail("Todas las unidades necesitan un nombre.");
            if (!set.Add(nombre))
                return Result<int>.Fail($"El nombre \"{nombre}\" está repetido.");

            var seccion = string.IsNullOrWhiteSpace(item.Seccion) ? null : item.Seccion.Trim();
            if (u.Nombre != nombre || u.Tipo != item.Tipo || u.Ocupacion != item.Ocupacion
                || u.Piso != item.Piso || u.AreaM2 != item.AreaM2
                || u.CuotaMantenimiento != item.CuotaMantenimiento
                || u.Coeficiente != item.Coeficiente
                || (u.Seccion ?? "") != (seccion ?? ""))
            {
                u.Nombre = nombre;
                u.Tipo = item.Tipo;
                u.Ocupacion = item.Ocupacion;
                u.Piso = item.Piso;
                u.AreaM2 = item.AreaM2;
                u.CuotaMantenimiento = item.CuotaMantenimiento;
                u.Coeficiente = item.Coeficiente;
                u.Seccion = seccion;
                cambiadas++;
            }
        }

        if (cambiadas == 0) return Result<int>.Ok(0);

        await _db.SaveChangesAsync(ct);
        await RecalcularSuscripcion(ct);
        return Result<int>.Ok(cambiadas);
    }

    public async Task<Result<UnidadDto>> CrearAsync(Guid consorcioId, CrearUnidadDto dto, CancellationToken ct = default)
    {
        if (!await ConsorcioExiste(consorcioId, ct))
            return Result<UnidadDto>.Fail("Consorcio no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<UnidadDto>.Fail("El nombre de la unidad es obligatorio.");

        var nombre = dto.Nombre.Trim();
        if (await _db.Unidades.AnyAsync(u => u.ConsorcioId == consorcioId && u.Nombre == nombre, ct))
            return Result<UnidadDto>.Fail($"Ya existe una unidad \"{nombre}\" en el consorcio.");

        var unidad = new Unidad
        {
            ConsorcioId = consorcioId,
            Nombre = nombre,
            Piso = dto.Piso,
            Tipo = dto.Tipo,
            AreaM2 = dto.AreaM2,
            Seccion = string.IsNullOrWhiteSpace(dto.Seccion) ? null : dto.Seccion.Trim(),
            CuotaMantenimiento = dto.CuotaMantenimiento,
            Coeficiente = dto.Coeficiente,
            Facturable = dto.Facturable,
        };
        _db.Unidades.Add(unidad);
        await _db.SaveChangesAsync(ct);
        await RecalcularSuscripcion(ct);
        await _actividad.RegistrarAsync(unidad.Id, TipoActividad.UnidadCreada, "Unidad creada", unidad.Nombre, ct);

        return Result<UnidadDto>.Ok(Proyectar(unidad));
    }

    public async Task<Result<int>> CrearLoteAsync(Guid consorcioId, CrearUnidadesLoteDto dto, CancellationToken ct = default)
    {
        if (!await ConsorcioExiste(consorcioId, ct))
            return Result<int>.Fail("Consorcio no encontrado.");

        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (dto.Reemplazar)
        {
            var previas = await _db.Unidades.Where(u => u.ConsorcioId == consorcioId).ToListAsync(ct);
            _db.Unidades.RemoveRange(previas);
        }
        else
        {
            var existentes = await _db.Unidades
                .Where(u => u.ConsorcioId == consorcioId)
                .Select(u => u.Nombre)
                .ToListAsync(ct);
            foreach (var n in existentes) vistos.Add(n);
        }

        var nuevas = new List<Unidad>();
        foreach (var item in dto.Unidades)
        {
            var nombre = item.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre) || !vistos.Add(nombre)) continue;

            nuevas.Add(new Unidad
            {
                ConsorcioId = consorcioId,
                Nombre = nombre,
                Piso = item.Piso,
                Tipo = item.Tipo,
                AreaM2 = item.AreaM2,
                Seccion = string.IsNullOrWhiteSpace(item.Seccion) ? null : item.Seccion!.Trim(),
                CuotaMantenimiento = item.CuotaMantenimiento,
                Coeficiente = item.Coeficiente,
                Facturable = item.Facturable,
            });
        }

        if (nuevas.Count == 0 && !dto.Reemplazar)
            return Result<int>.Fail("No hay unidades nuevas para agregar.");

        _db.Unidades.AddRange(nuevas);
        await _db.SaveChangesAsync(ct);
        await RecalcularSuscripcion(ct);

        return Result<int>.Ok(nuevas.Count);
    }

    public async Task<Result<UnidadDto>> ActualizarAsync(Guid consorcioId, Guid unidadId, ActualizarUnidadDto dto, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Personas)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result<UnidadDto>.Fail("Unidad no encontrada.");

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return Result<UnidadDto>.Fail("El nombre de la unidad es obligatorio.");
        if (await _db.Unidades.AnyAsync(u =>
                u.ConsorcioId == consorcioId && u.Id != unidadId && u.Nombre == nombre, ct))
            return Result<UnidadDto>.Fail($"Ya existe una unidad \"{nombre}\" en el consorcio.");

        unidad.Nombre = nombre;
        unidad.Piso = dto.Piso;
        unidad.Tipo = dto.Tipo;
        unidad.AreaM2 = dto.AreaM2;
        unidad.Seccion = string.IsNullOrWhiteSpace(dto.Seccion) ? null : dto.Seccion.Trim();
        unidad.CuotaMantenimiento = dto.CuotaMantenimiento;
        unidad.Coeficiente = dto.Coeficiente;
        unidad.Facturable = dto.Facturable;
        await _db.SaveChangesAsync(ct);
        await RecalcularSuscripcion(ct);
        await _actividad.RegistrarAsync(unidad.Id, TipoActividad.DetallesActualizados, "Detalles actualizados", null, ct);

        return Result<UnidadDto>.Ok(Proyectar(unidad));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid unidadId, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result.Fail("Unidad no encontrada.");

        _db.Unidades.Remove(unidad);
        await _db.SaveChangesAsync(ct);
        await RecalcularSuscripcion(ct);
        return Result.Ok();
    }

    public async Task<Result> CambiarOcupacionAsync(Guid consorcioId, Guid unidadId, TipoOcupacion ocupacion, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Personas)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result.Fail("Unidad no encontrada.");

        unidad.Ocupacion = ocupacion;

        // Desocupada => se quitan propietarios e inquilinos (los gestores quedan).
        if (ocupacion == TipoOcupacion.Desocupado)
        {
            var aQuitar = unidad.Personas.Where(p => p.Rol != RolUnidad.Gestor).ToList();
            if (aQuitar.Count > 0) _db.UnidadPersonas.RemoveRange(aQuitar);
        }

        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.OcupacionActualizada,
            "Ocupación actualizada", LabelOcupacion(ocupacion), ct);
        return Result.Ok();
    }

    public async Task<Result> CambiarInquilinosVenFinanzasAsync(Guid consorcioId, Guid unidadId, bool permitir, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result.Fail("Unidad no encontrada.");

        unidad.InquilinosVenFinanzas = permitir;
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.FinanzasInquilinosActualizado,
            permitir ? "Inquilinos pueden ver finanzas" : "Inquilinos ya no ven finanzas", null, ct);
        return Result.Ok();
    }

    public async Task<Result<PersonaUnidadDto>> AgregarPersonaAsync(Guid consorcioId, Guid unidadId, GuardarPersonaDto dto, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Personas)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result<PersonaUnidadDto>.Fail("Unidad no encontrada.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<PersonaUnidadDto>.Fail("El nombre es obligatorio.");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
        if (email is not null && unidad.Personas.Any(p => p.Rol == dto.Rol && p.Email == email))
            return Result<PersonaUnidadDto>.Fail("Esa persona ya está registrada con ese rol en la unidad.");

        // Primer ocupante (no gestor) => contacto principal automático.
        var esPrimerOcupante = dto.Rol != RolUnidad.Gestor
            && !unidad.Personas.Any(p => p.Rol != RolUnidad.Gestor);
        var contactoPrincipal = dto.EsContactoPrincipal || esPrimerOcupante;

        if (contactoPrincipal)
            foreach (var p in unidad.Personas) p.EsContactoPrincipal = false;

        var persona = new UnidadPersona
        {
            UnidadId = unidadId,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Email = email,
            Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
            Rol = dto.Rol,
            EsContactoPrincipal = contactoPrincipal && dto.Rol != RolUnidad.Gestor,
            UsuarioId = string.IsNullOrWhiteSpace(dto.UsuarioId) ? null : dto.UsuarioId,
        };
        _db.UnidadPersonas.Add(persona);
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.PersonaAgregada,
            $"{persona.Rol} agregado", $"{persona.Nombre} {persona.Apellido}".Trim(), ct);

        return Result<PersonaUnidadDto>.Ok(new PersonaUnidadDto(
            persona.Id, persona.Nombre, persona.Apellido, persona.Email, persona.Telefono,
            persona.Rol, persona.EsContactoPrincipal, persona.UsuarioId != null));
    }

    public async Task<Result> MarcarContactoPrincipalAsync(Guid consorcioId, Guid unidadId, Guid personaId, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Personas)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result.Fail("Unidad no encontrada.");

        var persona = unidad.Personas.FirstOrDefault(p => p.Id == personaId);
        if (persona is null) return Result.Fail("Persona no encontrada.");
        if (persona.Rol == RolUnidad.Gestor) return Result.Fail("Un gestor no puede ser el contacto principal.");

        foreach (var p in unidad.Personas) p.EsContactoPrincipal = p.Id == personaId;
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.ContactoPrincipalActualizado,
            "Contacto principal actualizado", $"{persona.Nombre} {persona.Apellido}".Trim(), ct);
        return Result.Ok();
    }

    public async Task<Result> CambiarRolPersonaAsync(Guid consorcioId, Guid unidadId, Guid personaId, RolUnidad rol, CancellationToken ct = default)
    {
        var persona = await _db.UnidadPersonas
            .Include(p => p.Unidad)
            .FirstOrDefaultAsync(p => p.Id == personaId && p.UnidadId == unidadId
                && p.Unidad.ConsorcioId == consorcioId, ct);
        if (persona is null) return Result.Fail("Persona no encontrada.");

        persona.Rol = rol;
        if (rol == RolUnidad.Gestor) persona.EsContactoPrincipal = false;
        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.RolActualizado,
            $"Ahora es {rol}", $"{persona.Nombre} {persona.Apellido}".Trim(), ct);
        return Result.Ok();
    }

    public async Task<Result> EliminarPersonaAsync(Guid consorcioId, Guid unidadId, Guid personaId, CancellationToken ct = default)
    {
        var unidad = await _db.Unidades
            .Include(u => u.Personas)
            .FirstOrDefaultAsync(u => u.Id == unidadId && u.ConsorcioId == consorcioId, ct);
        if (unidad is null) return Result.Fail("Unidad no encontrada.");

        var persona = unidad.Personas.FirstOrDefault(p => p.Id == personaId);
        if (persona is null) return Result.Fail("Persona no encontrada.");

        var eraPrincipal = persona.EsContactoPrincipal;
        _db.UnidadPersonas.Remove(persona);

        // Si era el contacto principal, promover al próximo ocupante.
        if (eraPrincipal)
        {
            var sucesor = unidad.Personas
                .Where(p => p.Id != personaId && p.Rol != RolUnidad.Gestor)
                .OrderBy(p => p.CreadoUtc)
                .FirstOrDefault();
            if (sucesor is not null) sucesor.EsContactoPrincipal = true;
        }

        await _db.SaveChangesAsync(ct);
        await _actividad.RegistrarAsync(unidadId, TipoActividad.PersonaQuitada,
            $"{persona.Rol} quitado", $"{persona.Nombre} {persona.Apellido}".Trim(), ct);
        return Result.Ok();
    }

    private static string LabelOcupacion(TipoOcupacion o) => o switch
    {
        TipoOcupacion.HabitadoPorPropietario => "Habitado por propietario",
        TipoOcupacion.Alquiler => "Alquiler",
        TipoOcupacion.Desocupado => "Desocupado",
        _ => o.ToString(),
    };

    private Task<bool> ConsorcioExiste(Guid consorcioId, CancellationToken ct) =>
        _db.Consorcios.AnyAsync(c => c.Id == consorcioId, ct);

    private async Task RecalcularSuscripcion(CancellationToken ct)
    {
        var adminId = await _db.Consorcios.Select(c => c.AdministradorId).FirstOrDefaultAsync(ct);
        if (adminId != Guid.Empty)
            await _suscripciones.RecalcularImporteAsync(adminId, ct);
    }

    private static UnidadDto Proyectar(Unidad u)
    {
        IReadOnlyList<PersonaMiniDto> PorRol(RolUnidad rol) => u.Personas
            .Where(p => p.Rol == rol)
            .OrderByDescending(p => p.EsContactoPrincipal)
            .Select(p => new PersonaMiniDto($"{p.Nombre} {p.Apellido}".Trim(), p.EsContactoPrincipal))
            .ToList();

        return new UnidadDto(
            u.Id, u.ConsorcioId, u.Nombre, u.Piso, u.Tipo, u.Ocupacion, u.AreaM2, u.Seccion,
            u.CuotaMantenimiento, u.Coeficiente, u.Facturable,
            PorRol(RolUnidad.Propietario), PorRol(RolUnidad.Inquilino), PorRol(RolUnidad.Gestor));
    }
}
