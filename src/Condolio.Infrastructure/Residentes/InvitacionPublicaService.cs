using Condolio.Application.Common;
using Condolio.Application.Residentes;
using Condolio.Domain.Residentes;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Residentes;

public class InvitacionPublicaService : IInvitacionPublicaService
{
    private readonly CondolioDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IJwtTokenGenerator _tokens;

    public InvitacionPublicaService(CondolioDbContext db, UserManager<ApplicationUser> users, IJwtTokenGenerator tokens)
    {
        _db = db;
        _users = users;
        _tokens = tokens;
    }

    public async Task<Result<InvitacionPublicaDto>> VerAsync(string token, CancellationToken ct = default)
    {
        var inv = await BuscarAsync(token, ct);
        if (inv is null) return Result<InvitacionPublicaDto>.Fail("La invitación no existe.");

        var (valida, motivo) = Validar(inv);
        var unidadNombre = inv.UnidadId is { } uid
            ? await _db.Unidades.IgnoreQueryFilters().Where(u => u.Id == uid).Select(u => u.Nombre).FirstOrDefaultAsync(ct)
            : null;
        var consorcio = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == inv.ConsorcioId).Select(c => c.Nombre).FirstAsync(ct);

        return Result<InvitacionPublicaDto>.Ok(new InvitacionPublicaDto(
            consorcio, inv.Email, inv.Nombre, unidadNombre, inv.Rol, valida, motivo));
    }

    public async Task<Result<AceptarInvitacionResultado>> AceptarAsync(string token, AceptarInvitacionDto dto, CancellationToken ct = default)
    {
        var inv = await BuscarAsync(token, ct);
        if (inv is null) return Result<AceptarInvitacionResultado>.Fail("La invitación no existe.");

        var (valida, motivo) = Validar(inv);
        if (!valida) return Result<AceptarInvitacionResultado>.Fail(motivo!);

        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Apellido))
            return Result<AceptarInvitacionResultado>.Fail("Nombre y apellido son obligatorios.");

        var user = await _users.FindByEmailAsync(inv.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = inv.Email,
                Email = inv.Email,
                EmailConfirmed = true,
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                AdministradorId = inv.AdministradorId,
            };
            var creado = await _users.CreateAsync(user, dto.Password);
            if (!creado.Succeeded)
                return Result<AceptarInvitacionResultado>.Fail(
                    string.Join(" ", creado.Errors.Select(e => e.Description)));
            await _users.AddToRoleAsync(user, Roles.Residente);
        }

        // Vincular a la unidad si la invitación la trae.
        if (inv.UnidadId is { } unidadId)
        {
            var persona = await _db.UnidadPersonas.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UnidadId == unidadId
                    && p.Email == inv.Email && p.Rol == inv.Rol, ct);

            if (persona is null)
            {
                var esPrimero = !await _db.UnidadPersonas.IgnoreQueryFilters()
                    .AnyAsync(p => p.UnidadId == unidadId && p.Rol != RolUnidad.Gestor, ct);
                _db.UnidadPersonas.Add(new UnidadPersona
                {
                    AdministradorId = inv.AdministradorId,
                    UnidadId = unidadId,
                    Nombre = dto.Nombre.Trim(),
                    Apellido = dto.Apellido.Trim(),
                    Email = inv.Email,
                    Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                    Rol = inv.Rol,
                    EsContactoPrincipal = esPrimero && inv.Rol != RolUnidad.Gestor,
                    UsuarioId = user.Id,
                });
            }
            else
            {
                persona.UsuarioId = user.Id;
                if (!string.IsNullOrWhiteSpace(dto.Telefono)) persona.Telefono = dto.Telefono.Trim();
            }
        }

        inv.Estado = EstadoInvitacion.Aceptada;
        await _db.SaveChangesAsync(ct);

        var roles = await _users.GetRolesAsync(user);
        var (jwt, expira) = _tokens.Generar(new TokenRequest(user.Id, user.Email!, roles, user.AdministradorId));
        return Result<AceptarInvitacionResultado>.Ok(new AceptarInvitacionResultado(
            jwt, expira, user.Email!, user.NombreCompleto, roles.ToList()));
    }

    private Task<Invitacion?> BuscarAsync(string token, CancellationToken ct) =>
        _db.Invitaciones.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Token == token, ct);

    private static (bool, string?) Validar(Invitacion inv) => inv.Estado switch
    {
        EstadoInvitacion.Aceptada => (false, "Esta invitación ya fue aceptada."),
        EstadoInvitacion.Cancelada => (false, "Esta invitación fue cancelada."),
        EstadoInvitacion.Expirada => (false, "Esta invitación venció."),
        _ when inv.ExpiraUtc < DateTime.UtcNow => (false, "Esta invitación venció."),
        _ => (true, null),
    };
}
