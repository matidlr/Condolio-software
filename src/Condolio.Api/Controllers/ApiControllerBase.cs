using Condolio.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Condolio.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToResult(Result r) =>
        r.Exito ? NoContent() : BadRequest(new { message = r.Error });

    protected IActionResult ToResult<T>(Result<T> r) =>
        r.Exito ? Ok(r.Valor) : BadRequest(new { message = r.Error });

    protected IActionResult ToCreated<T>(Result<T> r, string routeName, object routeValues) =>
        r.Exito ? CreatedAtRoute(routeName, routeValues, r.Valor) : BadRequest(new { message = r.Error });
}
