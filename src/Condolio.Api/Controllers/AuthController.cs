using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IJwtTokenGenerator _tokens;
    private readonly CondolioDbContext _db;
    private readonly ISuscripcionService _suscripciones;
    private readonly IInvitacionPublicaService _invitaciones;
    private readonly IEmailSender _email;

    public AuthController(
        UserManager<ApplicationUser> users,
        IJwtTokenGenerator tokens,
        CondolioDbContext db,
        ISuscripcionService suscripciones,
        IInvitacionPublicaService invitaciones,
        IEmailSender email)
    {
        _users = users;
        _tokens = tokens;
        _db = db;
        _suscripciones = suscripciones;
        _invitaciones = invitaciones;
        _email = email;
    }

    public record LoginRequest(string Email, string Password);
    public record RegistroRequest(string Nombre, string Apellido, string Email, string Password);
    public record LoginResponse(string Token, DateTime ExpiraUtc, string Email, string Nombre, IEnumerable<string> Roles);
    public record RegistroResponse(bool RequiereVerificacion, string Email);
    public record VerificarRequest(string Email, string Codigo);
    public record ReenviarCodigoRequest(string Email);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { message = "Credenciales inválidas." });
        if (!user.EmailConfirmed)
            return StatusCode(StatusCodes.Status403Forbidden, new RegistroResponse(true, user.Email!));

        return Ok(await ConstruirRespuesta(user));
    }

    [HttpPost("verificar")]
    public async Task<IActionResult> Verificar(VerificarRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null) return NotFound(new { message = "No encontramos esa cuenta." });
        if (user.EmailConfirmed) return Ok(await ConstruirRespuesta(user));

        if (user.CodigoVerificacion != req.Codigo?.Trim()
            || user.CodigoVerificacionExpiraUtc < DateTime.UtcNow)
            return BadRequest(new { message = "El código es incorrecto o venció." });

        user.EmailConfirmed = true;
        user.CodigoVerificacion = null;
        user.CodigoVerificacionExpiraUtc = null;
        await _users.UpdateAsync(user);
        return Ok(await ConstruirRespuesta(user));
    }

    [HttpPost("reenviar-codigo")]
    public async Task<IActionResult> ReenviarCodigo(ReenviarCodigoRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            await GenerarYEnviarCodigo(user);
            await _users.UpdateAsync(user);
        }
        return Ok(); // no revelamos si el correo existe
    }

    private async Task GenerarYEnviarCodigo(ApplicationUser user)
    {
        var codigo = Random.Shared.Next(0, 10000).ToString("D4");
        user.CodigoVerificacion = codigo;
        user.CodigoVerificacionExpiraUtc = DateTime.UtcNow.AddHours(24);
        var saludo = string.IsNullOrWhiteSpace(user.Nombre) ? "Hola" : $"Hola {user.Nombre}";
        var cuerpo = $"""
            <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;max-width:520px;margin:0 auto;color:#1f2937">
              <h1 style="font-size:22px;margin:0 0 16px">Verificá tu dirección de correo</h1>
              <p>{saludo},</p>
              <p>Usá el código de verificación a continuación para completar el registro de tu cuenta.</p>
              <p style="font-size:34px;font-weight:700;letter-spacing:.25em;margin:24px 0;text-align:center">{codigo}</p>
              <p style="font-size:14px;color:#6b7280">Este código expira en 24 horas.</p>
              <p style="font-size:14px;color:#6b7280">Ingresá este código en la aplicación para verificar tu correo electrónico.
                Si no solicitaste este código, podés ignorar este correo.</p>
              <p style="font-size:13px;color:#6b7280;margin-top:24px">— El equipo de Condolio</p>
            </div>
            """;
        await _email.EnviarAsync(user.Email!, "Verificá tu dirección de correo", cuerpo);
    }

    /// <summary>
    /// Alta self-service. Si el correo tiene una invitación pendiente, se registra como
    /// residente y se acepta la invitación. Si no, crea un administrador (tenant) con trial de 2 meses.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistroRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) is not null)
            return Conflict(new { message = "Ya existe una cuenta con ese correo." });

        // ¿Hay una invitación pendiente para este correo? -> alta como residente.
        var invitacion = await _db.Invitaciones
            .IgnoreQueryFilters()
            .Where(i => i.Email == email && i.Estado == Condolio.Domain.Residentes.EstadoInvitacion.Pendiente
                && i.ExpiraUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.CreadoUtc)
            .FirstOrDefaultAsync();

        if (invitacion is not null)
        {
            var res = await _invitaciones.AceptarAsync(invitacion.Token,
                new AceptarInvitacionDto(req.Nombre, req.Apellido, null, req.Password));
            if (!res.Exito) return BadRequest(new { message = res.Error });

            // Registro por formulario => pide verificación por código igual que un admin.
            var residente = await _users.FindByEmailAsync(email);
            residente!.EmailConfirmed = false;
            await GenerarYEnviarCodigo(residente);
            await _users.UpdateAsync(residente);
            return Ok(new RegistroResponse(true, email));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Nombre = req.Nombre.Trim(),
            Apellido = req.Apellido.Trim(),
            EmailConfirmed = false,
        };

        var creado = await _users.CreateAsync(user, req.Password);
        if (!creado.Succeeded)
            return BadRequest(new { message = string.Join(" ", creado.Errors.Select(e => e.Description)) });

        await _users.AddToRoleAsync(user, Roles.Administrador);

        var administrador = new Administrador
        {
            RazonSocial = $"{req.Nombre} {req.Apellido}".Trim(),
            Email = req.Email,
            UsuarioId = user.Id,
        };
        _db.Administradores.Add(administrador);
        await _db.SaveChangesAsync();

        user.AdministradorId = administrador.Id;
        await _suscripciones.IniciarTrialAsync(administrador.Id);

        await GenerarYEnviarCodigo(user);
        await _users.UpdateAsync(user);

        return Ok(new RegistroResponse(true, email));
    }

    private async Task<LoginResponse> ConstruirRespuesta(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        var (token, expira) = _tokens.Generar(
            new TokenRequest(user.Id, user.Email!, roles, user.AdministradorId));
        return new LoginResponse(token, expira, user.Email!, user.NombreCompleto, roles);
    }
}
