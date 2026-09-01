using Condolio.Api.Authorization;
using Condolio.Domain.Tenancy;
using Condolio.Application.Unidades;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/unidades/{unidadId:guid}/notas")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
[RequiereArea(AreaAdmin.Operacion)]
public class UnidadNotasController : ApiControllerBase
{
    private readonly INotaUnidadService _notas;

    public UnidadNotasController(INotaUnidadService notas) => _notas = notas;

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, Guid unidadId, CancellationToken ct) =>
        ToResult(await _notas.ListarAsync(consorcioId, unidadId, ct));

    [HttpPost]
    public async Task<IActionResult> Agregar(Guid consorcioId, Guid unidadId, GuardarNotaDto dto, CancellationToken ct) =>
        ToResult(await _notas.AgregarAsync(consorcioId, unidadId, dto, ct));

    [HttpPut("{notaId:guid}")]
    public async Task<IActionResult> Editar(Guid consorcioId, Guid unidadId, Guid notaId, GuardarNotaDto dto, CancellationToken ct) =>
        ToResult(await _notas.EditarAsync(consorcioId, unidadId, notaId, dto, ct));

    [HttpDelete("{notaId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid unidadId, Guid notaId, CancellationToken ct) =>
        ToResult(await _notas.EliminarAsync(consorcioId, unidadId, notaId, ct));
}
