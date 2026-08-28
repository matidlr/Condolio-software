using Condolio.Application.Consorcios;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/consorcios")]
[Authorize(Roles = $"{Roles.Administrador},{Roles.SuperAdmin}")]
public class ConsorciosController : ApiControllerBase
{
    private readonly IConsorcioService _consorcios;

    public ConsorciosController(IConsorcioService consorcios) => _consorcios = consorcios;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) =>
        Ok(await _consorcios.ListarAsync(ct));

    [HttpGet("{id:guid}", Name = "ObtenerConsorcio")]
    public async Task<IActionResult> Obtener(Guid id, CancellationToken ct) =>
        ToResult(await _consorcios.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Crear(CrearConsorcioDto dto, CancellationToken ct) =>
        ToResult(await _consorcios.CrearAsync(dto, ct));
}
