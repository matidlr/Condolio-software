using Condolio.Api.Authorization;
using Condolio.Application.Expensas;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

/// <summary>Períodos mensuales de expensas y sus gastos.</summary>
[ApiController]
[Route("api/consorcios/{consorcioId:guid}/expensas/periodos")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Finanzas)]
public class PeriodosExpensasController : ApiControllerBase
{
    private readonly IPeriodosExpensasService _svc;

    public PeriodosExpensasController(IPeriodosExpensasService svc) => _svc = svc;

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _svc.ListarAsync(consorcioId, ct));

    [HttpPost]
    public async Task<IActionResult> Abrir(Guid consorcioId, AbrirPeriodoDto dto, CancellationToken ct) =>
        ToResult(await _svc.AbrirAsync(consorcioId, dto, ct));

    [HttpGet("{periodoId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid periodoId, CancellationToken ct) =>
        ToResult(await _svc.ObtenerAsync(consorcioId, periodoId, ct));

    [HttpPost("{periodoId:guid}/reabrir")]
    public async Task<IActionResult> Reabrir(Guid consorcioId, Guid periodoId, CancellationToken ct) =>
        ToResult(await _svc.ReabrirAsync(consorcioId, periodoId, ct));

    // ---- Gastos ----

    [HttpPost("{periodoId:guid}/gastos")]
    public async Task<IActionResult> CrearGasto(Guid consorcioId, Guid periodoId, GuardarGastoPeriodoDto dto, CancellationToken ct) =>
        ToResult(await _svc.CrearGastoAsync(consorcioId, periodoId, dto, ct));

    [HttpPut("{periodoId:guid}/gastos/{gastoId:guid}")]
    public async Task<IActionResult> ActualizarGasto(Guid consorcioId, Guid periodoId, Guid gastoId, GuardarGastoPeriodoDto dto, CancellationToken ct) =>
        ToResult(await _svc.ActualizarGastoAsync(consorcioId, periodoId, gastoId, dto, ct));

    [HttpDelete("{periodoId:guid}/gastos/{gastoId:guid}")]
    public async Task<IActionResult> EliminarGasto(Guid consorcioId, Guid periodoId, Guid gastoId, CancellationToken ct) =>
        ToResult(await _svc.EliminarGastoAsync(consorcioId, periodoId, gastoId, ct));

    [HttpPost("{periodoId:guid}/gastos/{gastoId:guid}/comprobante")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> SubirComprobante(Guid consorcioId, Guid periodoId, Guid gastoId, [FromForm] IFormFile archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0) return BadRequest(new { message = "Archivo vacío." });
        await using var stream = archivo.OpenReadStream();
        var ext = Path.GetExtension(archivo.FileName);
        return ToResult(await _svc.GuardarComprobanteGastoAsync(consorcioId, periodoId, gastoId, stream, ext, ct));
    }

    [HttpGet("{periodoId:guid}/gastos/{gastoId:guid}/comprobante")]
    public async Task<IActionResult> VerComprobante(Guid consorcioId, Guid periodoId, Guid gastoId, CancellationToken ct)
    {
        var r = await _svc.AbrirComprobanteGastoAsync(consorcioId, periodoId, gastoId, ct);
        if (!r.Exito) return NotFound(new { message = r.Error });
        return File(r.Valor.Contenido, r.Valor.ContentType);
    }
}
