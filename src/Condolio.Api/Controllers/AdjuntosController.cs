using Condolio.Application.Archivos;
using Condolio.Domain.Archivos;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/adjuntos")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class AdjuntosController : ApiControllerBase
{
    private readonly IAdjuntoService _adjuntos;

    public AdjuntosController(IAdjuntoService adjuntos) => _adjuntos = adjuntos;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] TipoAdjuntoOwner ownerTipo, [FromQuery] Guid ownerId, CancellationToken ct) =>
        ToResult(await _adjuntos.ListarAsync(ownerTipo, ownerId, ct));

    [HttpPost]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Subir(
        [FromForm] TipoAdjuntoOwner ownerTipo,
        [FromForm] Guid ownerId,
        IFormFile archivo,
        CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { message = "Archivo vacío." });

        await using var stream = archivo.OpenReadStream();
        var resultado = await _adjuntos.SubirAsync(ownerTipo, ownerId,
            new NuevoAdjunto(archivo.FileName, archivo.ContentType, archivo.Length, stream), ct);
        return ToResult(resultado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Descargar(Guid id, CancellationToken ct)
    {
        var r = await _adjuntos.DescargarAsync(id, ct);
        if (!r.Exito) return NotFound(new { message = r.Error });
        var a = r.Valor!;
        return File(a.Contenido, a.ContentType, a.NombreArchivo);
    }

    public record RenombrarBody(string Nombre);

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Renombrar(Guid id, RenombrarBody body, CancellationToken ct) =>
        ToResult(await _adjuntos.RenombrarAsync(id, body.Nombre, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct) =>
        ToResult(await _adjuntos.EliminarAsync(id, ct));
}
