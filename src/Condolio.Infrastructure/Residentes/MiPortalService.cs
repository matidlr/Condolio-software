using Condolio.Application.Amenidades;
using Condolio.Application.Calendario;
using Condolio.Application.Common;
using Condolio.Application.Comunicaciones;
using Condolio.Application.Documentos;
using Condolio.Application.Encuestas;
using Condolio.Application.Residentes;
using Condolio.Domain.Comunicaciones;
using Condolio.Domain.Amenidades;
using Condolio.Domain.Archivos;
using Condolio.Domain.Calendario;
using Condolio.Domain.Documentos;
using Condolio.Domain.Encuestas;
using Condolio.Domain.Tickets;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Archivos;
using Condolio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Residentes;

public class MiPortalService : IMiPortalService
{
    /// <summary>Los datetime del sistema se guardan en hora local de Argentina (UTC-3, sin DST).</summary>
    private static DateTime AhoraLocal => DateTime.UtcNow.AddHours(-3);

    private readonly CondolioDbContext _db;
    private readonly IAmenidadService _amenidades;
    private readonly IEventoService _eventos;
    private readonly IDocumentoService _documentos;
    private readonly IEncuestaService _encuestas;
    private readonly IFileStorage _storage;
    private readonly IAnuncioService _anuncios;

    public MiPortalService(CondolioDbContext db, IAmenidadService amenidades, IEventoService eventos,
        IDocumentoService documentos, IEncuestaService encuestas, IFileStorage storage, IAnuncioService anuncios)
    {
        _db = db;
        _amenidades = amenidades;
        _eventos = eventos;
        _documentos = documentos;
        _encuestas = encuestas;
        _storage = storage;
        _anuncios = anuncios;
    }

    private static string EstadoIncidencia(EstadoTicket e) => e switch
    {
        EstadoTicket.Nuevo => "Pendiente",
        EstadoTicket.EnProgreso => "En progreso",
        EstadoTicket.EsperandoInformacion => "Necesita info",
        EstadoTicket.PendienteAprobacion => "En revisión",
        EstadoTicket.Resuelto => "Resuelto",
        _ => e.ToString(),
    };

    private async Task<Origen?> OrigenAsync(string usuarioId, CancellationToken ct) =>
        await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new Origen(
                p.AdministradorId,
                p.UnidadId,
                p.Unidad.Nombre,
                p.Unidad.ConsorcioId,
                p.Unidad.Consorcio.Nombre,
                p.Unidad.Consorcio.Localidad,
                (p.Nombre + " " + p.Apellido).Trim()))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<PortalCasaDto>> CasaAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<PortalCasaDto>.Fail("Todavía no tenés una unidad asignada.");

        var ahora = AhoraLocal;

        var votadas = await _db.VotosEncuesta.IgnoreQueryFilters()
            .Where(v => v.UsuarioId == usuarioId).Select(v => v.EncuestaId).Distinct().ToListAsync(ct);

        var pendientes = await _db.Encuestas.IgnoreQueryFilters()
            .Where(e => e.ConsorcioId == o.ConsorcioId && e.Estado == EstadoEncuesta.Activa && !votadas.Contains(e.Id))
            .OrderBy(e => e.CierreUtc ?? DateTime.MaxValue)
            .Select(e => new { e.Id, e.Titulo, e.CierreUtc })
            .ToListAsync(ct);

        var encuestasPendientes = pendientes
            .Select(e => new EncuestaPendienteDto(e.Id, e.Titulo,
                e.CierreUtc is { } c ? Math.Max(0, (int)Math.Ceiling((c - ahora).TotalDays)) : (int?)null))
            .ToList();

        var reservas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SolicitanteUsuarioId == usuarioId && r.Fin >= ahora
                && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .OrderBy(r => r.Inicio).Take(5)
            .Select(r => new ReservaResumenDto(r.Id, r.Amenidad.Nombre, r.Inicio, r.Fin, r.Estado.ToString()))
            .ToListAsync(ct);

        return Result<PortalCasaDto>.Ok(new PortalCasaDto(
            o.ConsorcioId, o.ConsorcioNombre, o.Localidad, o.UnidadNombre,
            encuestasPendientes, reservas, 0, 0));
    }

    public async Task<Result<ComunidadInfoDto>> ComunidadAsync(string usuarioId, CancellationToken ct = default)
    {
        var info = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId)
            .Select(p => new ComunidadInfoDto(
                p.Unidad.Consorcio.Nombre,
                p.Unidad.Consorcio.Direccion,
                p.Unidad.Consorcio.Localidad,
                p.Unidad.Consorcio.Provincia,
                p.Unidad.Consorcio.Pais,
                p.Unidad.Consorcio.CodigoPostal,
                p.Unidad.Nombre,
                p.Rol.ToString()))
            .FirstOrDefaultAsync(ct);
        return info is null
            ? Result<ComunidadInfoDto>.Fail("No tenés una unidad asignada.")
            : Result<ComunidadInfoDto>.Ok(info);
    }

    public async Task<Result<MiUnidadDetalleDto>> UnidadAsync(string usuarioId, CancellationToken ct = default)
    {
        var unidadId = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == usuarioId).Select(p => (Guid?)p.UnidadId).FirstOrDefaultAsync(ct);
        if (unidadId is not { } uid) return Result<MiUnidadDetalleDto>.Fail("No tenés una unidad asignada.");

        var u = await _db.Unidades.IgnoreQueryFilters()
            .Where(x => x.Id == uid)
            .Select(x => new { x.Nombre, x.Piso, x.Tipo, x.Ocupacion, Consorcio = x.Consorcio.Nombre })
            .FirstAsync(ct);

        var personas = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UnidadId == uid)
            .Select(p => new { Nombre = (p.Nombre + " " + p.Apellido).Trim(), p.Rol, p.EsContactoPrincipal })
            .ToListAsync(ct);

        var propietarios = personas.Where(p => p.Rol == RolUnidad.Propietario)
            .Select(p => new MiUnidadPersonaDto(p.Nombre, "Propietario", p.EsContactoPrincipal)).ToList();
        var inquilinos = personas.Where(p => p.Rol == RolUnidad.Inquilino)
            .Select(p => new MiUnidadPersonaDto(p.Nombre, "Inquilino", p.EsContactoPrincipal)).ToList();

        var ocup = u.Ocupacion switch
        {
            TipoOcupacion.HabitadoPorPropietario => "Ocupado por propietario",
            TipoOcupacion.Alquiler => "Alquilada",
            _ => "Desocupada",
        };
        var tipo = u.Tipo switch
        {
            TipoUnidad.Local => "Local",
            TipoUnidad.Cochera => "Cochera",
            TipoUnidad.Baulera => "Baulera",
            _ => "Departamento",
        };

        return Result<MiUnidadDetalleDto>.Ok(new MiUnidadDetalleDto(
            u.Nombre, u.Piso, tipo, ocup,
            personas.Count(p => p.Rol != RolUnidad.Gestor), u.Consorcio,
            propietarios, inquilinos));
    }

    public async Task<Result<IReadOnlyList<AmenidadDto>>> AmenidadesAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<AmenidadDto>>.Fail("No tenés una unidad asignada.");

        var res = await _amenidades.ListarAsync(o.ConsorcioId, ct);
        if (!res.Exito) return Result<IReadOnlyList<AmenidadDto>>.Fail(res.Error!);

        var reservables = res.Valor!.Amenidades.Where(a => a.Reservable).ToList();
        return Result<IReadOnlyList<AmenidadDto>>.Ok(reservables);
    }

    public async Task<Result<AmenidadDto>> AmenidadAsync(string usuarioId, Guid amenidadId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<AmenidadDto>.Fail("No tenés una unidad asignada.");
        return await _amenidades.ObtenerAsync(o.ConsorcioId, amenidadId, ct);
    }

    public async Task<Result<IReadOnlyList<SlotDto>>> SlotsAsync(string usuarioId, Guid amenidadId, DateOnly fecha, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<SlotDto>>.Fail("No tenés una unidad asignada.");

        var amenidad = await _db.Amenidades.IgnoreQueryFilters()
            .Include(a => a.Horarios)
            .FirstOrDefaultAsync(a => a.Id == amenidadId && a.ConsorcioId == o.ConsorcioId, ct);
        if (amenidad is null) return Result<IReadOnlyList<SlotDto>>.Fail("Amenidad no encontrada.");

        var dia = fecha.ToDateTime(TimeOnly.MinValue).DayOfWeek;
        var horario = amenidad.Horarios.FirstOrDefault(h => h.Dia == dia);
        if (horario is null || horario.Cerrado)
            return Result<IReadOnlyList<SlotDto>>.Ok(Array.Empty<SlotDto>());

        var dur = amenidad.IntervaloMinutos <= 0 ? 60 : amenidad.IntervaloMinutos;
        // La grilla de horarios y las reservas se manejan en hora local (naive).
        var medianoche = fecha.ToDateTime(TimeOnly.MinValue);
        var ahora = AhoraLocal;

        var ocupadas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.AmenidadId == amenidadId
                && r.Estado != EstadoReserva.Rechazada && r.Estado != EstadoReserva.Cancelada
                && r.Fin > medianoche && r.Inicio < medianoche.AddDays(1))
            .Select(r => new { r.Inicio, r.Fin })
            .ToListAsync(ct);

        var slots = new List<SlotDto>();
        for (var m = horario.AbreMin; m + dur <= horario.CierraMin; m += dur)
        {
            var inicio = medianoche.AddMinutes(m);
            var fin = inicio.AddMinutes(dur);
            if (fin <= ahora) continue;
            if (ocupadas.Any(x => x.Inicio < fin && x.Fin > inicio)) continue;
            slots.Add(new SlotDto(inicio, fin));
        }
        return Result<IReadOnlyList<SlotDto>>.Ok(slots);
    }

    public async Task<Result<MiReservaDto>> SolicitarReservaAsync(
        string usuarioId, Guid amenidadId, DateTime inicio, DateTime fin, string? nota, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<MiReservaDto>.Fail("No tenés una unidad asignada.");
        if (fin <= inicio) return Result<MiReservaDto>.Fail("El horario no es válido.");

        var amenidad = await _db.Amenidades.IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == amenidadId && a.ConsorcioId == o.ConsorcioId, ct);
        if (amenidad is null) return Result<MiReservaDto>.Fail("Amenidad no encontrada.");
        if (!amenidad.Reservable) return Result<MiReservaDto>.Fail("Esta amenidad no admite reservas.");

        var activasPropias = await _db.Reservas.IgnoreQueryFilters().CountAsync(r =>
            r.AmenidadId == amenidadId && r.SolicitanteUsuarioId == usuarioId
            && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada)
            && r.Fin >= AhoraLocal, ct);
        if (amenidad.MaxReservasPorUnidad > 0 && activasPropias >= amenidad.MaxReservasPorUnidad && !amenidad.LimiteMensual)
            return Result<MiReservaDto>.Fail($"Alcanzaste el máximo de {amenidad.MaxReservasPorUnidad} reserva(s) activa(s).");

        var solapa = await _db.Reservas.IgnoreQueryFilters().AnyAsync(r => r.AmenidadId == amenidadId
            && r.Estado != EstadoReserva.Rechazada && r.Estado != EstadoReserva.Cancelada
            && r.Inicio < fin && r.Fin > inicio, ct);
        if (solapa) return Result<MiReservaDto>.Fail("Ese horario ya está reservado.");

        var horas = (decimal)(fin - inicio).TotalHours;
        var reserva = new Reserva
        {
            AdministradorId = o.AdministradorId,
            ConsorcioId = o.ConsorcioId,
            AmenidadId = amenidadId,
            UnidadId = o.UnidadId,
            SolicitanteUsuarioId = usuarioId,
            SolicitanteNombre = string.IsNullOrWhiteSpace(o.Nombre) ? "Residente" : o.Nombre,
            Inicio = inicio,
            Fin = fin,
            Estado = amenidad.RequiereAprobacion ? EstadoReserva.Pendiente : EstadoReserva.Confirmada,
            Importe = amenidad.TieneCosto ? amenidad.Tarifa * Math.Max(1, Math.Ceiling(horas)) : null,
            Nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim(),
        };
        if (reserva.Estado == EstadoReserva.Confirmada) reserva.ResueltaUtc = DateTime.UtcNow;

        _db.Reservas.Add(reserva);
        await _db.SaveChangesAsync(ct);

        return Result<MiReservaDto>.Ok(new MiReservaDto(
            reserva.Id, amenidadId, amenidad.Nombre, Adjuntos(amenidad.ImagenesIds),
            reserva.Inicio, reserva.Fin, reserva.Estado.ToString(), reserva.Nota, reserva.CreadoUtc));
    }

    public async Task<Result<MisReservasDto>> MisReservasAsync(string usuarioId, CancellationToken ct = default)
    {
        var reservas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SolicitanteUsuarioId == usuarioId)
            .OrderByDescending(r => r.Inicio)
            .Select(r => new
            {
                r.Id, r.AmenidadId, Amenidad = r.Amenidad.Nombre, r.Amenidad.ImagenesIds,
                r.Inicio, r.Fin, r.Estado, r.Nota, r.CreadoUtc,
            })
            .ToListAsync(ct);

        var ahora = AhoraLocal;
        var mapped = reservas.Select(r => new
        {
            Activa = r.Fin >= ahora && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada),
            r.Inicio,
            Dto = new MiReservaDto(r.Id, r.AmenidadId, r.Amenidad, Adjuntos(r.ImagenesIds),
                r.Inicio, r.Fin, r.Estado.ToString(), r.Nota, r.CreadoUtc),
        }).ToList();

        var activas = mapped.Where(x => x.Activa).OrderBy(x => x.Inicio).Select(x => x.Dto).ToList();
        var previas = mapped.Where(x => !x.Activa).Select(x => x.Dto).ToList();

        return Result<MisReservasDto>.Ok(new MisReservasDto(activas, previas));
    }

    public async Task<Result<MiReservaDto>> MiReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default)
    {
        var r = await _db.Reservas.IgnoreQueryFilters()
            .Where(x => x.Id == reservaId && x.SolicitanteUsuarioId == usuarioId)
            .Select(x => new
            {
                x.Id, x.AmenidadId, Amenidad = x.Amenidad.Nombre, x.Amenidad.ImagenesIds,
                x.Inicio, x.Fin, x.Estado, x.Nota, x.CreadoUtc,
            })
            .FirstOrDefaultAsync(ct);
        if (r is null) return Result<MiReservaDto>.Fail("Reserva no encontrada.");

        return Result<MiReservaDto>.Ok(new MiReservaDto(
            r.Id, r.AmenidadId, r.Amenidad, Adjuntos(r.ImagenesIds),
            r.Inicio, r.Fin, r.Estado.ToString(), r.Nota, r.CreadoUtc));
    }

    public async Task<Result<IReadOnlyList<CalendarioItemDto>>> CalendarioAsync(
        string usuarioId, DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<CalendarioItemDto>>.Fail("No tenés una unidad asignada.");

        var eventos = await _db.EventosCalendario.IgnoreQueryFilters()
            .Where(e => e.ConsorcioId == o.ConsorcioId && e.InicioUtc < hasta && e.FinUtc > desde)
            .Select(e => new CalendarioItemDto(
                e.Id, e.Titulo, e.Descripcion, e.Ubicacion,
                e.InicioUtc, e.FinUtc, e.TodoElDia, "Evento", e.Categoria.ToString()))
            .ToListAsync(ct);

        var reservas = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.SolicitanteUsuarioId == usuarioId
                && r.Estado != EstadoReserva.Rechazada && r.Estado != EstadoReserva.Cancelada
                && r.Inicio < hasta && r.Fin > desde)
            .Select(r => new CalendarioItemDto(
                r.Id, r.Amenidad.Nombre, r.Nota, r.Amenidad.Nombre,
                r.Inicio, r.Fin, false, "Reserva", r.Estado.ToString()))
            .ToListAsync(ct);

        var items = eventos.Concat(reservas).OrderBy(i => i.Inicio).ToList();
        return Result<IReadOnlyList<CalendarioItemDto>>.Ok(items);
    }

    public async Task<Result<CalendarioItemDto>> CrearEventoAsync(
        string usuarioId, CrearEventoResidenteDto dto, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<CalendarioItemDto>.Fail("No tenés una unidad asignada.");
        if (string.IsNullOrWhiteSpace(dto.Titulo)) return Result<CalendarioItemDto>.Fail("El título es obligatorio.");

        Enum.TryParse<CategoriaEvento>(dto.Categoria, out var cat);
        var res = await _eventos.CrearAsync(o.ConsorcioId, new GuardarEventoDto(
            dto.Titulo.Trim(),
            string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            string.IsNullOrWhiteSpace(dto.Ubicacion) ? null : dto.Ubicacion.Trim(),
            cat, dto.Inicio, dto.Fin, dto.TodoElDia, NotificarComunidad: false), ct);
        if (!res.Exito) return Result<CalendarioItemDto>.Fail(res.Error!);

        var e = res.Valor!;
        return Result<CalendarioItemDto>.Ok(new CalendarioItemDto(
            e.Id, e.Titulo, e.Descripcion, e.Ubicacion, e.InicioUtc, e.FinUtc, e.TodoElDia, "Evento", e.Categoria.ToString()));
    }

    public async Task<Result> CancelarReservaAsync(string usuarioId, Guid reservaId, CancellationToken ct = default)
    {
        var n = await _db.Reservas.IgnoreQueryFilters()
            .Where(r => r.Id == reservaId && r.SolicitanteUsuarioId == usuarioId
                && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Confirmada))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Estado, EstadoReserva.Cancelada)
                .SetProperty(r => r.ResueltaUtc, DateTime.UtcNow), ct);
        return n == 0 ? Result.Fail("Reserva no encontrada.") : Result.Ok();
    }

    public async Task<Result<ContenidoDto>> DocumentosAsync(string usuarioId, Guid? carpetaId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<ContenidoDto>.Fail("No tenés una unidad asignada.");

        var esPropietario = await _db.UnidadPersonas.IgnoreQueryFilters()
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Rol == RolUnidad.Propietario, ct);

        bool Permitido(NivelAcceso n) =>
            n == NivelAcceso.Todos || (esPropietario && n == NivelAcceso.Propietarios);

        var res = await _documentos.ListarAsync(o.ConsorcioId, carpetaId, ct);
        if (!res.Exito) return Result<ContenidoDto>.Fail(res.Error!);

        var c = res.Valor!;
        var carpetas = c.Carpetas.Where(x => Permitido(x.Nivel)).ToList();
        var docs = c.Documentos.Where(x => Permitido(x.Nivel)).ToList();
        return Result<ContenidoDto>.Ok(new ContenidoDto(
            c.CarpetaActualId, c.CarpetaActualNombre, carpetas, docs, c.AlmacenamientoUsado, c.AlmacenamientoTotal));
    }

    public async Task<Result<ArchivoDocumento>> DescargarDocumentoAsync(
        string usuarioId, Guid documentoId, bool registrarDescarga, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<ArchivoDocumento>.Fail("No tenés una unidad asignada.");

        var esPropietario = await _db.UnidadPersonas.IgnoreQueryFilters()
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Rol == RolUnidad.Propietario, ct);

        var doc = await _db.Documentos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.ConsorcioId == o.ConsorcioId, ct);
        if (doc is null) return Result<ArchivoDocumento>.Fail("Documento no encontrado.");
        if (doc.Nivel == NivelAcceso.Admin || doc.Nivel == NivelAcceso.Junta
            || (doc.Nivel == NivelAcceso.Propietarios && !esPropietario))
            return Result<ArchivoDocumento>.Fail("No tenés acceso a este documento.");

        return await _documentos.DescargarAsync(o.ConsorcioId, documentoId, registrarDescarga, ct);
    }

    public async Task<Result<IReadOnlyList<EncuestaDto>>> EncuestasAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<EncuestaDto>>.Fail("No tenés una unidad asignada.");

        var res = await _encuestas.ListarAsync(o.ConsorcioId, ct);
        if (!res.Exito) return Result<IReadOnlyList<EncuestaDto>>.Fail(res.Error!);

        // El residente no ve borradores.
        var visibles = res.Valor!.Encuestas.Where(e => e.Estado != EstadoEncuesta.Borrador).ToList();
        return Result<IReadOnlyList<EncuestaDto>>.Ok(visibles);
    }

    public async Task<Result<EncuestaDetalleDto>> EncuestaAsync(string usuarioId, Guid encuestaId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<EncuestaDetalleDto>.Fail("No tenés una unidad asignada.");
        return await _encuestas.ObtenerAsync(o.ConsorcioId, encuestaId, ct);
    }

    public async Task<Result<EncuestaDto>> VotarAsync(string usuarioId, Guid encuestaId, IReadOnlyList<Guid> opcionesIds, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<EncuestaDto>.Fail("No tenés una unidad asignada.");
        return await _encuestas.VotarAsync(o.ConsorcioId, encuestaId, opcionesIds, ct);
    }

    public async Task<Result<IReadOnlyList<IncidenciaResidenteDto>>> IncidenciasAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<IncidenciaResidenteDto>>.Fail("No tenés una unidad asignada.");

        var lista = await _db.Tickets.IgnoreQueryFilters()
            .Where(t => t.ConsorcioId == o.ConsorcioId && t.ReportadoPorUsuarioId == usuarioId)
            .OrderByDescending(t => t.UltimaActividadUtc)
            .Select(t => new IncidenciaResidenteDto(
                t.Id, t.Numero, t.Titulo ?? t.Categoria.ToString(), t.Descripcion,
                t.Categoria.ToString(), EstadoIncidencia(t.Estado), t.Prioridad.ToString(),
                t.Ubicacion, t.CreadoUtc, t.UltimaActividadUtc))
            .ToListAsync(ct);
        return Result<IReadOnlyList<IncidenciaResidenteDto>>.Ok(lista);
    }

    public async Task<Result<IncidenciaDetalleResidenteDto>> IncidenciaAsync(string usuarioId, Guid ticketId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IncidenciaDetalleResidenteDto>.Fail("No tenés una unidad asignada.");

        var t = await _db.Tickets.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == ticketId && x.ConsorcioId == o.ConsorcioId && x.ReportadoPorUsuarioId == usuarioId, ct);
        if (t is null) return Result<IncidenciaDetalleResidenteDto>.Fail("Reporte no encontrado.");

        var dto = new IncidenciaResidenteDto(
            t.Id, t.Numero, t.Titulo ?? t.Categoria.ToString(), t.Descripcion,
            t.Categoria.ToString(), EstadoIncidencia(t.Estado), t.Prioridad.ToString(),
            t.Ubicacion, t.CreadoUtc, t.UltimaActividadUtc);

        var mensajes = await _db.TicketComentarios.IgnoreQueryFilters()
            .Where(c => c.TicketId == ticketId && !c.EsInterna)
            .OrderBy(c => c.CreadoUtc)
            .Select(c => new IncidenciaMensajeDto(
                c.Texto,
                c.AutorUsuarioId == usuarioId ? "Vos" : "Administración",
                c.AutorUsuarioId != usuarioId,
                c.CreadoUtc))
            .ToListAsync(ct);

        var adjuntos = await _db.Adjuntos.IgnoreQueryFilters()
            .Where(a => a.OwnerTipo == TipoAdjuntoOwner.Ticket && a.OwnerId == ticketId)
            .Select(a => new IncidenciaAdjuntoDto(a.Id, a.NombreArchivo, a.ContentType, a.ContentType.StartsWith("image/")))
            .ToListAsync(ct);

        return Result<IncidenciaDetalleResidenteDto>.Ok(new IncidenciaDetalleResidenteDto(dto, mensajes, adjuntos));
    }

    public async Task<Result<IncidenciaResidenteDto>> CrearIncidenciaAsync(string usuarioId, CrearIncidenciaResidenteDto dto, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IncidenciaResidenteDto>.Fail("No tenés una unidad asignada.");
        if (string.IsNullOrWhiteSpace(dto.Descripcion)) return Result<IncidenciaResidenteDto>.Fail("Describí el problema.");

        Enum.TryParse<CategoriaTicket>(dto.Categoria, true, out var cat);

        var numero = await _db.Tickets.IgnoreQueryFilters()
            .Where(t => t.ConsorcioId == o.ConsorcioId).Select(t => (int?)t.Numero).MaxAsync(ct) ?? 0;

        var t = new Ticket
        {
            AdministradorId = o.AdministradorId,
            ConsorcioId = o.ConsorcioId,
            Numero = numero + 1,
            Titulo = cat.ToString(),
            Descripcion = dto.Descripcion.Trim(),
            Categoria = cat,
            Estado = EstadoTicket.Nuevo,
            Prioridad = PrioridadTicket.Media,
            UnidadId = o.UnidadId,
            ReportadoPorUsuarioId = usuarioId,
            ReportadoPorNombre = string.IsNullOrWhiteSpace(o.Nombre) ? "Residente" : o.Nombre,
            ReportadoUtc = DateTime.UtcNow,
            UltimaActividadUtc = DateTime.UtcNow,
            EstadoDesdeUtc = DateTime.UtcNow,
        };
        _db.Tickets.Add(t);
        await _db.SaveChangesAsync(ct);

        foreach (var archivo in dto.Archivos ?? Array.Empty<ArchivoSubidaDto>())
        {
            if (archivo.Tamano <= 0 || archivo.Tamano > 25L * 1024 * 1024) continue;
            var ext = Path.GetExtension(archivo.Nombre);
            var ruta = $"incidencias/{o.ConsorcioId:N}/{Guid.CreateVersion7():N}{ext}";
            await _storage.GuardarAsync(ruta, archivo.Contenido, ct);
            _db.Adjuntos.Add(new Adjunto
            {
                AdministradorId = o.AdministradorId,
                OwnerTipo = TipoAdjuntoOwner.Ticket,
                OwnerId = t.Id,
                NombreArchivo = archivo.Nombre,
                ContentType = archivo.ContentType,
                Tamano = archivo.Tamano,
                RutaRelativa = ruta,
            });
        }
        await _db.SaveChangesAsync(ct);

        return Result<IncidenciaResidenteDto>.Ok(new IncidenciaResidenteDto(
            t.Id, t.Numero, t.Titulo!, t.Descripcion, t.Categoria.ToString(), EstadoIncidencia(t.Estado),
            t.Prioridad.ToString(), t.Ubicacion, t.CreadoUtc, t.UltimaActividadUtc));
    }

    public async Task<Result> ComentarIncidenciaAsync(string usuarioId, Guid ticketId, string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto)) return Result.Fail("El comentario no puede estar vacío.");
        var t = await _db.Tickets.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ticketId && x.ReportadoPorUsuarioId == usuarioId, ct);
        if (t is null) return Result.Fail("Reporte no encontrado.");

        _db.TicketComentarios.Add(new TicketComentario
        {
            TicketId = ticketId,
            AdministradorId = t.AdministradorId,
            Texto = texto.Trim(),
            AutorUsuarioId = usuarioId,
            EsInterna = false,
        });
        await _db.SaveChangesAsync(ct);
        await _db.Tickets.Where(x => x.Id == ticketId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UltimaActividadUtc, DateTime.UtcNow), ct);
        return Result.Ok();
    }

    public async Task<Result<ArchivoDocumento>> DescargarAdjuntoIncidenciaAsync(string usuarioId, Guid adjuntoId, CancellationToken ct = default)
    {
        var a = await _db.Adjuntos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == adjuntoId && x.OwnerTipo == TipoAdjuntoOwner.Ticket, ct);
        if (a is null) return Result<ArchivoDocumento>.Fail("Adjunto no encontrado.");

        var esMio = await _db.Tickets.IgnoreQueryFilters()
            .AnyAsync(t => t.Id == a.OwnerId && t.ReportadoPorUsuarioId == usuarioId, ct);
        if (!esMio) return Result<ArchivoDocumento>.Fail("No tenés acceso a este archivo.");

        return Result<ArchivoDocumento>.Ok(new ArchivoDocumento(a.NombreArchivo, a.ContentType, _storage.Abrir(a.RutaRelativa)));
    }

    // ---- Muro (anuncios) ----

    public async Task<Result<IReadOnlyList<AnuncioDto>>> MuroAsync(string usuarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<IReadOnlyList<AnuncioDto>>.Fail("No tenés una unidad asignada.");
        var res = await _anuncios.ListarAsync(o.ConsorcioId, ct);
        return res.Exito
            ? Result<IReadOnlyList<AnuncioDto>>.Ok(res.Valor!.Anuncios)
            : Result<IReadOnlyList<AnuncioDto>>.Fail(res.Error!);
    }

    public async Task<Result<AnuncioDetalleDto>> PublicacionAsync(string usuarioId, Guid anuncioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<AnuncioDetalleDto>.Fail("No tenés una unidad asignada.");
        return await _anuncios.ObtenerAsync(o.ConsorcioId, anuncioId, ct);
    }

    public async Task<Result<AnuncioDto>> PublicarAsync(
        string usuarioId, string cuerpo, IReadOnlyList<ArchivoSubidaDto>? imagenes, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<AnuncioDto>.Fail("No tenés una unidad asignada.");
        if (string.IsNullOrWhiteSpace(cuerpo) && (imagenes is null || imagenes.Count == 0))
            return Result<AnuncioDto>.Fail("Escribí algo o agregá una imagen.");

        var res = await _anuncios.CrearAsync(o.ConsorcioId,
            new GuardarAnuncioDto(null, string.IsNullOrWhiteSpace(cuerpo) ? " " : cuerpo.Trim(),
                CategoriaAnuncio.General, false, null, null, null), ct);
        if (!res.Exito) return res;

        var anuncioId = res.Valor!.Id;
        var ids = new List<Guid>();
        foreach (var img in imagenes ?? Array.Empty<ArchivoSubidaDto>())
        {
            if (img.Tamano <= 0 || img.Tamano > 15L * 1024 * 1024) continue;
            var ext = Path.GetExtension(img.Nombre);
            var ruta = $"anuncios/{o.ConsorcioId:N}/{Guid.CreateVersion7():N}{ext}";
            await _storage.GuardarAsync(ruta, img.Contenido, ct);
            var a = new Adjunto
            {
                AdministradorId = o.AdministradorId,
                OwnerTipo = TipoAdjuntoOwner.Anuncio,
                OwnerId = anuncioId,
                NombreArchivo = img.Nombre,
                ContentType = img.ContentType,
                Tamano = img.Tamano,
                RutaRelativa = ruta,
            };
            _db.Adjuntos.Add(a);
            ids.Add(a.Id);
        }
        if (ids.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            await _db.Anuncios.Where(x => x.Id == anuncioId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ImagenesIds, string.Join(",", ids)), ct);
        }

        return await _anuncios.ObtenerAsync(o.ConsorcioId, anuncioId, ct) is { Exito: true } det
            ? Result<AnuncioDto>.Ok(det.Valor!.Anuncio)
            : res;
    }

    public async Task<Result> ComentarMuroAsync(string usuarioId, Guid anuncioId, string texto, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result.Fail("No tenés una unidad asignada.");
        return await _anuncios.ComentarAsync(o.ConsorcioId, anuncioId, texto, ct);
    }

    public async Task<Result> EditarComentarioMuroAsync(string usuarioId, Guid anuncioId, Guid comentarioId, string texto, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result.Fail("No tenés una unidad asignada.");
        return await _anuncios.EditarComentarioAsync(o.ConsorcioId, anuncioId, comentarioId, texto, ct);
    }

    public async Task<Result> EliminarComentarioMuroAsync(string usuarioId, Guid anuncioId, Guid comentarioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result.Fail("No tenés una unidad asignada.");

        var mio = await _db.AnuncioComentarios.IgnoreQueryFilters()
            .AnyAsync(c => c.Id == comentarioId && c.AnuncioId == anuncioId && c.AutorUsuarioId == usuarioId, ct);
        if (!mio) return Result.Fail("Solo podés eliminar tus comentarios.");

        return await _anuncios.EliminarComentarioAsync(o.ConsorcioId, anuncioId, comentarioId, ct);
    }

    public async Task<Result<LikeResultadoDto>> ToggleLikeMuroAsync(string usuarioId, Guid anuncioId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<LikeResultadoDto>.Fail("No tenés una unidad asignada.");
        return await _anuncios.ToggleLikeAsync(o.ConsorcioId, anuncioId, ct);
    }

    public async Task<Result<ArchivoDocumento>> DescargarAdjuntoMuroAsync(string usuarioId, Guid adjuntoId, CancellationToken ct = default)
    {
        var o = await OrigenAsync(usuarioId, ct);
        if (o is null) return Result<ArchivoDocumento>.Fail("No tenés una unidad asignada.");

        var a = await _db.Adjuntos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == adjuntoId && x.OwnerTipo == TipoAdjuntoOwner.Anuncio, ct);
        if (a is null) return Result<ArchivoDocumento>.Fail("Adjunto no encontrado.");

        var delConsorcio = await _db.Anuncios.IgnoreQueryFilters()
            .AnyAsync(an => an.Id == a.OwnerId && an.ConsorcioId == o.ConsorcioId, ct);
        if (!delConsorcio) return Result<ArchivoDocumento>.Fail("No tenés acceso a este archivo.");

        return Result<ArchivoDocumento>.Ok(new ArchivoDocumento(a.NombreArchivo, a.ContentType, _storage.Abrir(a.RutaRelativa)));
    }

    private static IReadOnlyList<Guid> Adjuntos(string? ids) =>
        string.IsNullOrWhiteSpace(ids)
            ? Array.Empty<Guid>()
            : ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToArray();

    private sealed record Origen(
        Guid AdministradorId, Guid UnidadId, string UnidadNombre,
        Guid ConsorcioId, string ConsorcioNombre, string? Localidad, string Nombre);
}
