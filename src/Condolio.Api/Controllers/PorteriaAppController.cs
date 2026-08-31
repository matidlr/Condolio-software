using System.Security.Claims;
using Condolio.Application.Accesos;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Api.Controllers;

/// <summary>Endpoints de la app de portería (rol Personal): contexto + verificación de QR.</summary>
[ApiController]
[Route("api/porteria")]
[Authorize(Roles = Roles.Personal)]
public class PorteriaAppController : ApiControllerBase
{
    private readonly CondolioDbContext _db;
    private readonly IPaseAccesoService _pases;

    public PorteriaAppController(CondolioDbContext db, IPaseAccesoService pases)
    {
        _db = db;
        _pases = pases;
    }

    private string Uid => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public record ContextoDto(Guid ConsorcioId, string ConsorcioNombre, string CasetaNombre);
    public record VerificarBody(string Token);

    [HttpGet("contexto")]
    public async Task<IActionResult> Contexto(CancellationToken ct)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == Uid)
            .Select(p => new { p.ConsorcioId, p.Nombre, Consorcio = p.ConsorcioId })
            .FirstOrDefaultAsync(ct);
        if (m is null) return NotFound(new { message = "Esta cuenta no está vinculada a ninguna caseta." });

        var nombre = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == m.ConsorcioId).Select(c => c.Nombre).FirstOrDefaultAsync(ct) ?? "—";
        return Ok(new ContextoDto(m.ConsorcioId, nombre, m.Nombre));
    }

    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar(VerificarBody body, CancellationToken ct)
    {
        var ctx = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.UsuarioId == Uid).Select(p => new { p.ConsorcioId, p.Nombre }).FirstOrDefaultAsync(ct);
        if (ctx is null) return NotFound(new { message = "Esta cuenta no está vinculada a ninguna caseta." });

        var token = (body.Token ?? "").Trim();
        // Acepta el token pelado o la URL completa .../verificar-acceso/<token>
        var idx = token.LastIndexOf('/');
        if (idx >= 0) token = token[(idx + 1)..];

        return ToResult(await _pases.VerificarAsync(ctx.ConsorcioId, token, Uid, ctx.Nombre, ct));
    }
}
