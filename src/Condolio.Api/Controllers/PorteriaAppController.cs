using System.Security.Claims;
using Condolio.Application.Accesos;
using Condolio.Application.Comunicaciones;
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

    public PorteriaAppController(CondolioDbContext db, IPaseAccesoService pases, IAccesoAdminService accesos, IAnuncioService anuncios)
    {
        _db = db;
        _pases = pases;
        _accesos = accesos;
        _anuncios = anuncios;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private async Task<(Guid consorcioId, string casetaNombre)?> CtxAsync(CancellationToken ct)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == Uid && p.EsDispositivo)
            .Select(p => new { p.ConsorcioId, p.Nombre })
            .FirstOrDefaultAsync(ct);
        return m is null ? null : (m.ConsorcioId, m.Nombre);
    }

    public record ContextoDto(Guid ConsorcioId, string ConsorcioNombre, string CasetaNombre);
    public record VerificarBody(string Token);

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

        var token = (body.Token ?? "").Trim();
        var idx = token.LastIndexOf('/');
        if (idx >= 0) token = token[(idx + 1)..];

        return ToResult(await _pases.VerificarAsync(c.consorcioId, token, Uid, c.casetaNombre, ct));
    }

    [HttpPost("entrada-manual")]
    public async Task<IActionResult> EntradaManual(EntradaManualDto dto, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _accesos.RegistrarEntradaManualAsync(c.consorcioId, dto, Uid, c.casetaNombre, ct));
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
    public async Task<IActionResult> Bitacora([FromQuery] int dias, [FromQuery] string? q, CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        return ToResult(await _accesos.BitacoraAsync(
            c.consorcioId, DateOnly.FromDateTime(DateTime.UtcNow), dias <= 0 ? 7 : dias, null, q, ct));
    }

    [HttpGet("unidades")]
    public async Task<IActionResult> Unidades(CancellationToken ct)
    {
        var ctx = await CtxAsync(ct);
        if (ctx is not { } c) return NotFound();
        var us = await _db.Unidades.IgnoreQueryFilters()
            .Where(u => u.ConsorcioId == c.consorcioId)
            .OrderBy(u => u.Nombre)
            .Select(u => new { u.Id, u.Nombre })
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
}
