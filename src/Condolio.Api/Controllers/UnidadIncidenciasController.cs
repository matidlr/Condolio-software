using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using Condolio.Application.Unidades;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/unidades/{unidadId:guid}/incidencias")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Operacion)]
public class UnidadIncidenciasController : ApiControllerBase
{
    private readonly IIncidenciaUnidadService _incidencias;

    public UnidadIncidenciasController(IIncidenciaUnidadService incidencias) => _incidencias = incidencias;

    public record ComentarioBody(string Texto);

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, Guid unidadId, CancellationToken ct) =>
        ToResult(await _incidencias.ListarAsync(consorcioId, unidadId, ct));

    [HttpGet("{incidenciaId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct) =>
        ToResult(await _incidencias.ObtenerAsync(consorcioId, unidadId, incidenciaId, ct));

    [HttpPost]
    public async Task<IActionResult> Registrar(Guid consorcioId, Guid unidadId, GuardarIncidenciaDto dto, CancellationToken ct) =>
        ToResult(await _incidencias.RegistrarAsync(consorcioId, unidadId, dto, ct));

    [HttpPut("{incidenciaId:guid}")]
    public async Task<IActionResult> Editar(Guid consorcioId, Guid unidadId, Guid incidenciaId, GuardarIncidenciaDto dto, CancellationToken ct) =>
        ToResult(await _incidencias.EditarAsync(consorcioId, unidadId, incidenciaId, dto, ct));

    [HttpPost("{incidenciaId:guid}/comentarios")]
    public async Task<IActionResult> Comentar(Guid consorcioId, Guid unidadId, Guid incidenciaId, ComentarioBody body, CancellationToken ct) =>
        ToResult(await _incidencias.AgregarComentarioAsync(consorcioId, unidadId, incidenciaId, body.Texto, ct));

    [HttpPost("{incidenciaId:guid}/escalar")]
    public async Task<IActionResult> Escalar(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct) =>
        ToResult(await _incidencias.EscalarAsync(consorcioId, unidadId, incidenciaId, ct));

    [HttpDelete("{incidenciaId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid unidadId, Guid incidenciaId, CancellationToken ct) =>
        ToResult(await _incidencias.EliminarAsync(consorcioId, unidadId, incidenciaId, ct));
}
