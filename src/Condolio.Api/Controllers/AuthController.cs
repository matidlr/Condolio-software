using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Google.Apis.Auth;
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
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> users,
        IJwtTokenGenerator tokens,
        CondolioDbContext db,
        ISuscripcionService suscripciones,
        IInvitacionPublicaService invitaciones,
        IEmailSender email,
        IConfiguration config)
    {
        _users = users;
        _tokens = tokens;
        _db = db;
        _suscripciones = suscripciones;
        _invitaciones = invitaciones;
        _email = email;
        _config = config;
    }

    public record LoginRequest(string Email, string Password);
    public record RegistroRequest(string Nombre, string Apellido, string Email, string Password);
    public record GoogleLoginRequest(string IdToken);
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

        await CrearTenantAsync(user, req.Nombre, req.Apellido, email);

        await GenerarYEnviarCodigo(user);
        await _users.UpdateAsync(user);

        return Ok(new RegistroResponse(true, email));
    }

    /// <summary>
    /// Acceso con Google. Valida el ID token, y según el correo: inicia sesión,
    /// acepta una invitación pendiente, o crea un administrador nuevo con trial.
    /// Google ya verificó el correo, así que no se pide código.
    /// </summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest req)
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return BadRequest(new { message = "El acceso con Google no está configurado." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "El token de Google no es válido." });
        }

        if (!payload.EmailVerified)
            return BadRequest(new { message = "Tu cuenta de Google no tiene el correo verificado." });

        var email = payload.Email.Trim().ToLowerInvariant();
        var nombre = string.IsNullOrWhiteSpace(payload.GivenName) ? "Usuario" : payload.GivenName.Trim();
        var apellido = payload.FamilyName?.Trim() ?? string.Empty;

        var user = await _users.FindByEmailAsync(email);
        if (user is not null)
        {
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                user.CodigoVerificacion = null;
                user.CodigoVerificacionExpiraUtc = null;
                await _users.UpdateAsync(user);
            }
            return Ok(await ConstruirRespuesta(user));
        }

        // Usuario nuevo: ¿invitación pendiente? -> residente; si no -> administrador.
        var invitacion = await _db.Invitaciones
            .IgnoreQueryFilters()
            .Where(i => i.Email == email && i.Estado == Condolio.Domain.Residentes.EstadoInvitacion.Pendiente
                && i.ExpiraUtc > DateTime.UtcNow)
            .OrderByDescending(i => i.CreadoUtc)
            .FirstOrDefaultAsync();

        if (invitacion is not null)
        {
            var res = await _invitaciones.AceptarAsync(invitacion.Token,
                new AceptarInvitacionDto(nombre, apellido, null, GenerarPasswordAleatoria()));
            if (!res.Exito) return BadRequest(new { message = res.Error });

            var residente = await _users.FindByEmailAsync(email);
            return Ok(await ConstruirRespuesta(residente!));
        }

        var nuevo = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Nombre = nombre,
            Apellido = apellido,
            EmailConfirmed = true,
        };
        var creado = await _users.CreateAsync(nuevo);
        if (!creado.Succeeded)
            return BadRequest(new { message = string.Join(" ", creado.Errors.Select(e => e.Description)) });

        await CrearTenantAsync(nuevo, nombre, apellido, email);
        await _users.UpdateAsync(nuevo);

        return Ok(await ConstruirRespuesta(nuevo));
    }

    /// <summary>Crea el Administrador (tenant) para <paramref name="user"/>, lo asocia y arranca el trial.</summary>
    private async Task CrearTenantAsync(ApplicationUser user, string nombre, string apellido, string email)
    {
        await _users.AddToRoleAsync(user, Roles.Administrador);

        var administrador = new Administrador
        {
            RazonSocial = $"{nombre} {apellido}".Trim(),
            Email = email,
            UsuarioId = user.Id,
        };
        _db.Administradores.Add(administrador);
        await _db.SaveChangesAsync();

        user.AdministradorId = administrador.Id;
        await _suscripciones.IniciarTrialAsync(administrador.Id);
    }

    public record CambiarClaveRequest(string Actual, string Nueva);

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("cambiar-clave")]
    public async Task<IActionResult> CambiarClave(CambiarClaveRequest req)
    {
        var uid = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Nueva) || req.Nueva.Length < 6)
            return BadRequest(new { message = "La nueva contraseña debe tener al menos 6 caracteres." });

        var user = await _users.FindByIdAsync(uid);
        if (user is null) return Unauthorized();

        var res = await _users.ChangePasswordAsync(user, req.Actual, req.Nueva);
        if (!res.Succeeded)
        {
            var msg = res.Errors.Any(e => e.Code.Contains("Password"))
                ? "La contraseña actual no es correcta."
                : string.Join(" ", res.Errors.Select(e => e.Description));
            return BadRequest(new { message = msg });
        }
        return NoContent();
    }

    private static string GenerarPasswordAleatoria() =>
        $"G{Guid.NewGuid():N}A9!";

    private async Task<LoginResponse> ConstruirRespuesta(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        var (token, expira) = _tokens.Generar(
            new TokenRequest(user.Id, user.Email!, roles, user.AdministradorId));
        return new LoginResponse(token, expira, user.Email!, user.NombreCompleto, roles);
    }
}
