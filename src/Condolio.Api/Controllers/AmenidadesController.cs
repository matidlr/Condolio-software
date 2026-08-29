using Condolio.Application.Amenidades;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios/{consorcioId:guid}/amenidades")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class AmenidadesController : ApiControllerBase
{
    private readonly IAmenidadService _amenidades;

    public AmenidadesController(IAmenidadService amenidades) => _amenidades = amenidades;

    [HttpGet]
    public async Task<IActionResult> Listar(Guid consorcioId, CancellationToken ct) =>
        ToResult(await _amenidades.ListarAsync(consorcioId, ct));

    [HttpGet("{amenidadId:guid}")]
    public async Task<IActionResult> Obtener(Guid consorcioId, Guid amenidadId, CancellationToken ct) =>
        ToResult(await _amenidades.ObtenerAsync(consorcioId, amenidadId, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(Guid consorcioId, GuardarAmenidadDto dto, CancellationToken ct) =>
        ToResult(await _amenidades.CrearAsync(consorcioId, dto, ct));

    [HttpPut("{amenidadId:guid}")]
    public async Task<IActionResult> Actualizar(Guid consorcioId, Guid amenidadId, GuardarAmenidadDto dto, CancellationToken ct) =>
        ToResult(await _amenidades.ActualizarAsync(consorcioId, amenidadId, dto, ct));

    [HttpDelete("{amenidadId:guid}")]
    public async Task<IActionResult> Eliminar(Guid consorcioId, Guid amenidadId, CancellationToken ct) =>
        ToResult(await _amenidades.EliminarAsync(consorcioId, amenidadId, ct));
}
