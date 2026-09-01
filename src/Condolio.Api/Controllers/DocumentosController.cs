using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using Condolio.Application.Documentos;
using Condolio.Domain.Documentos;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/documentos")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Comunicacion)]
public class DocumentosController : ApiControllerBase
{
    private readonly IDocumentoService _docs;

    public DocumentosController(IDocumentoService docs) => _docs = docs;

    public record RenombrarBody(string Nombre);
    public record DestacarBody(bool Destacar);
    public record MoverBody(Guid? DestinoId);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, [FromQuery] Guid? carpetaId, CancellationToken ct) =>
        ToResult(await _docs.ListarAsync(consorcioId, carpetaId, ct));

    [HttpGet("recientes")]
    public async Task<IActionResult> Recientes(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _docs.RecientesAsync(consorcioId, ct));

    [HttpGet("destacados")]
    public async Task<IActionResult> Destacados(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _docs.DestacadosAsync(consorcioId, ct));

    [HttpGet("nivel/{nivel}")]
    public async Task<IActionResult> PorNivel(Guid consorcioId, NivelAcceso nivel, CancellationToken ct) =>
        ToResult(await _docs.PorNivelAsync(consorcioId, nivel, ct));

    [HttpGet("analiticas")]
    public async Task<IActionResult> Analiticas(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _docs.AnaliticasAsync(consorcioId, ct));

    [HttpPost("carpetas")]
    public async Task<IActionResult> CrearCarpeta(Guid consorcioId, CrearCarpetaDto dto, CancellationToken ct) =>
        ToResult(await _docs.CrearCarpetaAsync(consorcioId, dto, ct));

    [HttpPut("carpetas/{carpetaId:guid}")]
    public async Task<IActionResult> RenombrarCarpeta(Guid consorcioId, Guid carpetaId, RenombrarBody body, CancellationToken ct) =>
        ToResult(await _docs.RenombrarCarpetaAsync(consorcioId, carpetaId, body.Nombre, ct));

    [HttpPost("carpetas/{carpetaId:guid}/mover")]
    public async Task<IActionResult> MoverCarpeta(Guid consorcioId, Guid carpetaId, MoverBody body, CancellationToken ct) =>
        ToResult(await _docs.MoverCarpetaAsync(consorcioId, carpetaId, body.DestinoId, ct));

    [HttpGet("carpetas/todas")]
    public async Task<IActionResult> TodasLasCarpetas(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _docs.TodasLasCarpetasAsync(consorcioId, ct));

    [HttpDelete("carpetas/{carpetaId:guid}")]
    public async Task<IActionResult> EliminarCarpeta(Guid consorcioId, Guid carpetaId, CancellationToken ct) =>
        ToResult(await _docs.EliminarCarpetaAsync(consorcioId, carpetaId, ct));

    [HttpPost]
    [RequestSizeLimit(52 * 1024 * 1024)]
    public async Task<IActionResult> Subir(
        Guid consorcioId,
        [FromForm] IFormFile archivo,
        [FromForm] Guid? carpetaId,
        [FromForm] NivelAcceso nivel,
        [FromForm] CategoriaDocumento categoria,
        CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0) return BadRequest(new { message = "Archivo vacío." });
        await using var stream = archivo.OpenReadStream();
        var res = await _docs.SubirAsync(consorcioId,
            new NuevoDocumento(archivo.FileName, archivo.ContentType ?? "application/octet-stream", archivo.Length, stream, carpetaId, nivel, categoria), ct);
        return ToResult(res);
    }

    [HttpPut("{documentoId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid documentoId, ActualizarDocumentoDto dto, CancellationToken ct) =>
        ToResult(await _docs.ActualizarAsync(consorcioId, documentoId, dto, ct));

    [HttpPost("{documentoId:guid}/destacar")]
    public async Task<IActionResult> Destacar(Guid consorcioId, Guid documentoId, DestacarBody body, CancellationToken ct) =>
        ToResult(await _docs.DestacarAsync(consorcioId, documentoId, body.Destacar, ct));

    [HttpGet("{documentoId:guid}/descargar")]
    public async Task<IActionResult> Descargar(Guid consorcioId, Guid documentoId, [FromQuery] bool descarga, CancellationToken ct)
    {
        var r = await _docs.DescargarAsync(consorcioId, documentoId, descarga, ct);
        if (!r.Exito) return NotFound(new { message = r.Error });
        return File(r.Valor!.Contenido, r.Valor.ContentType, r.Valor.Nombre);
    }

    [HttpDelete("{documentoId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid documentoId, CancellationToken ct) =>
        ToResult(await _docs.EliminarAsync(consorcioId, documentoId, ct));
}
