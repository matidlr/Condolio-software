using System.Security.Claims;
using Condolio.Application.Accesos;
using Condolio.Application.Comunicaciones;
using Condolio.Application.Paqueteria;
using Condolio.Domain.Paqueteria;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Api.Controllers;

/// <summary>App de portería (rol Personal): contexto, verificación de QR, entradas/salidas manuales, bitácora y alertas.</summary>
[ApiController]
[Route("api/porteria")]
[Authorize(Roles = Roles.Personal)]
public class PorteriaAppController : ApiControllerBase
{
    private readonly CondolioDbContext _db;
    private readonly IPaseAccesoService _pases;
    private readonly IAccesoAdminService _accesos;
    private readonly IAnuncioService _anuncios;
    private readonly IPaqueteriaService _paquetes;

    public PorteriaAppController(CondolioDbContext db, IPaseAccesoService pases, IAccesoAdminService accesos,
        IAnuncioService anuncios, IPaqueteriaService paquetes)
    {
        _db = db;
        _pases = pases;
        _accesos = accesos;
        _anuncios = anuncios;
        _paquetes = paquetes;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>consorcioId, nombre de la caseta y nombre a atribuir en los registros
    /// (la persona del turno abierto si existe, si no la caseta).</summary>
    private async Task<(Guid consorcioId, string casetaNombre, string registradoPor)?> CtxAsync(CancellationToken ct)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == Uid && p.EsDispositivo)
            .Select(p => new { p.ConsorcioId, p.Nombre })
            .FirstOrDefaultAsync(ct);
        if (m is null) return null;
        var turno = await _db.TurnosPorteria
            .Where(t => t.CredencialUsuarioId == Uid && t.FinUtc == null)
            .OrderByDescending(t => t.InicioUtc)
            .Select(t => t.PersonalNombre)
            .FirstOrDefaultAsync(ct);
        return (m.ConsorcioId, m.Nombre, string.IsNullOrWhiteSpace(turno) ? m.Nombre : turno!);
    }

    public record ContextoDto(Guid ConsorcioId, string ConsorcioNombre, string CasetaNombre);
    public record VerificarBody(string Token);
    public record ConfirmarBody(string Token, string? Documento, string? Patente);
    public record PersonalTurnoDto(Guid Id, string Nombre, string Apellido, string Tipo);
    public record TurnoActualDto(Guid Id, Guid MiembroPersonalId, string PersonalNombre, DateTime InicioUtc);
    public record IniciarTurnoBody(Guid MiembroPersonalId);
    public record FinalizarTurnoBody(string? Notas);
    public record ResidenteUnidadDto(string Nombre, string Rol);
    public record UnidadDetalleDto(Guid Id, string Nombre, int Piso, IReadOnlyList<ResidenteUnidadDto> Residentes);

    [HttpGet("contexto")]
    public async Task<IActionResult> Contexto(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound(new { message = "Esta cuenta no está vinculada a ninguna caseta." });
        var nombre = await _db.Consorcios.IgnoreQueryFilters()
            .Where(x => x.Id == c.consorcioId).Select(x => x.Nombre).FirstOrDefaultAsync(ct) ?? "—";
        return Ok(new ContextoDto(c.consorcioId, nombre, c.casetaNombre));
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _accesos.ResumenAsync(c.consorcioId, ct));
    }

    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar(VerificarBody body, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound(new { message = "Esta cuenta no está vinculada a ninguna caseta." });

        return ToResult(await _pases.VerificarAsync(c.consorcioId, Limpiar(body.Token), Uid, c.casetaNombre, ct));
    }

    [HttpPost("confirmar-ingreso")]
    public async Task<IActionResult> ConfirmarIngreso(ConfirmarBody body, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound(new { message = "Esta cuenta no está vinculada a ninguna caseta." });
        return ToResult(await _pases.ConfirmarIngresoAsync(
            c.consorcioId, Limpiar(body.Token), body.Documento, body.Patente, Uid, c.registradoPor, ct));
    }

    private static string Limpiar(string? token)
    {
        var t = (token ?? "").Trim();
        var idx = t.LastIndexOf('/');
        return idx >= 0 ? t[(idx + 1)..] : t;
    }

    [HttpPost("entrada-manual")]
    public async Task<IActionResult> EntradaManual(EntradaManualDto dto, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _accesos.RegistrarEntradaManualAsync(c.consorcioId, dto, Uid, c.registradoPor, ct));
    }

    [HttpGet("adentro")]
    public async Task<IActionResult> Adentro(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _accesos.AdentroAhoraAsync(c.consorcioId, ct));
    }

    [HttpPost("salida/{registroId:guid}")]
    public async Task<IActionResult> Salida(Guid registroId, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _accesos.RegistrarEgresoAsync(c.consorcioId, registroId, ct));
    }

    [HttpGet("bitacora")]
    public async Task<IActionResult> Bitacora(
        [FromQuery] int anio, [FromQuery] int mes, [FromQuery] string? q, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();

        var ahora = DateTime.UtcNow;
        var y = anio == 0 ? ahora.Year : anio;
        var m = mes == 0 ? ahora.Month : mes;
        var diasMes = DateTime.DaysInMonth(y, m);
        // fecha = último día del mes (acotado a hoy si es el mes actual), dias = para cubrir desde el 1
        var ultimoDia = (y == ahora.Year && m == ahora.Month)
            ? DateOnly.FromDateTime(ahora)
            : new DateOnly(y, m, diasMes);
        var dias = ultimoDia.Day;
        return ToResult(await _accesos.BitacoraAsync(c.consorcioId, ultimoDia, dias, null, q, ct));
    }

    [HttpGet("unidades")]
    public async Task<IActionResult> Unidades(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        var us = await _db.Unidades.IgnoreQueryFilters()
            .Where(u => u.ConsorcioId == c.consorcioId)
            .OrderBy(u => u.Nombre)
            .Select(u => new
            {
                u.Id,
                u.Nombre,
                Contacto = _db.UnidadPersonas.IgnoreQueryFilters()
                    .Where(p => p.UnidadId == u.Id)
                    .OrderByDescending(p => p.EsContactoPrincipal)
                    .Select(p => (p.Nombre + " " + p.Apellido).Trim())
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);
        return Ok(us);
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _anuncios.ListarAsync(c.consorcioId, ct));
    }

    // ---- Turno ----

    /// <summary>Personal del staff habilitado en esta caseta (comparte la credencial del dispositivo).</summary>
    [HttpGet("personal")]
    public async Task<IActionResult> PersonalDeLaCaseta(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        var lista = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == Uid && !p.EsDispositivo && p.Activo)
            .OrderBy(p => p.Nombre).ThenBy(p => p.Apellido)
            .Select(p => new PersonalTurnoDto(p.Id, p.Nombre, p.Apellido, p.Tipo.ToString()))
            .ToListAsync(ct);
        return Ok(lista);
    }

    [HttpGet("turno")]
    public async Task<IActionResult> TurnoActual(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        var t = await _db.TurnosPorteria
            .Where(x => x.CredencialUsuarioId == Uid && x.FinUtc == null)
            .OrderByDescending(x => x.InicioUtc)
            .Select(x => new TurnoActualDto(x.Id, x.MiembroPersonalId, x.PersonalNombre, x.InicioUtc))
            .FirstOrDefaultAsync(ct);
        return Ok(t);
    }

    [HttpPost("turno")]
    public async Task<IActionResult> IniciarTurno(IniciarTurnoBody body, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();

        var persona = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == body.MiembroPersonalId && p.UsuarioId == Uid && !p.EsDispositivo, ct);
        if (persona is null) return BadRequest(new { message = "Ese empleado no pertenece a esta caseta." });

        var abiertos = await _db.TurnosPorteria
            .Where(x => x.CredencialUsuarioId == Uid && x.FinUtc == null).ToListAsync(ct);
        var ahora = DateTime.UtcNow;
        foreach (var a in abiertos) a.FinUtc = ahora;

        var turno = new Condolio.Domain.Personal.TurnoPorteria
        {
            ConsorcioId = c.consorcioId,
            CredencialUsuarioId = Uid,
            MiembroPersonalId = persona.Id,
            PersonalNombre = persona.NombreCompleto,
            InicioUtc = ahora,
        };
        _db.TurnosPorteria.Add(turno);
        await _db.SaveChangesAsync(ct);
        return Ok(new TurnoActualDto(turno.Id, turno.MiembroPersonalId, turno.PersonalNombre, turno.InicioUtc));
    }

    [HttpPost("turno/fin")]
    public async Task<IActionResult> FinalizarTurno(FinalizarTurnoBody body, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        var abiertos = await _db.TurnosPorteria
            .Where(x => x.CredencialUsuarioId == Uid && x.FinUtc == null).ToListAsync(ct);
        if (abiertos.Count == 0) return NoContent();
        var ahora = DateTime.UtcNow;
        var nota = string.IsNullOrWhiteSpace(body.Notas) ? null : body.Notas.Trim();
        foreach (var a in abiertos) { a.FinUtc = ahora; a.NotasCierre = nota; }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ---- Paquetería ----

    [HttpGet("paquetes/resumen")]
    public async Task<IActionResult> ResumenPaquetes(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _paquetes.ResumenAsync(c.consorcioId, ct));
    }

    [HttpGet("paquetes")]
    public async Task<IActionResult> ListarPaquetes(
        [FromQuery] EstadoPaquete? estado, [FromQuery] string? q,
        [FromQuery] int anio, [FromQuery] int mes, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _paquetes.ListarAsync(c.consorcioId, estado, q, anio, mes, ct));
    }

    [HttpGet("paquetes/{id:guid}")]
    public async Task<IActionResult> ObtenerPaquete(Guid id, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _paquetes.ObtenerAsync(c.consorcioId, id, ct));
    }

    [HttpPost("paquetes")]
    public async Task<IActionResult> RegistrarPaquete(RegistrarPaqueteDto dto, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _paquetes.RegistrarAsync(c.consorcioId, dto, c.registradoPor, ct));
    }

    [HttpPost("paquetes/{id:guid}/entregar")]
    public async Task<IActionResult> EntregarPaquete(Guid id, EntregarPaqueteDto dto, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _paquetes.EntregarAsync(c.consorcioId, id, dto, c.registradoPor, ct));
    }

    [HttpGet("unidades/{unidadId:guid}")]
    public async Task<IActionResult> UnidadDetalle(Guid unidadId, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        var u = await _db.Unidades.IgnoreQueryFilters()
            .Where(x => x.Id == unidadId && x.ConsorcioId == c.consorcioId)
            .Select(x => new { x.Id, x.Nombre, x.Piso })
            .FirstOrDefaultAsync(ct);
        if (u is null) return NotFound();
        var residentes = await _db.UnidadPersonas.IgnoreQueryFilters()
            .Where(p => p.UnidadId == unidadId)
            .OrderByDescending(p => p.EsContactoPrincipal)
            .Select(p => new ResidenteUnidadDto((p.Nombre + " " + p.Apellido).Trim(), p.Rol.ToString()))
            .ToListAsync(ct);
        return Ok(new UnidadDetalleDto(u.Id, u.Nombre, u.Piso, residentes));
    }
}
