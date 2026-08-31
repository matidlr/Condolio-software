using Condolio.Application.Common;
using Condolio.Application.Tenancy;
using Condolio.Domain.Tenancy;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Tenancy;

public class AdminMiembroService : IAdminMiembroService
{
    private readonly CondolioDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AdminMiembroService(CondolioDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<AdminMiembroDto>>> ListarAsync(Guid administradorId, CancellationToken ct = default)
    {
        await GarantizarDuenoAsync(administradorId, ct);

        var miembros = await _db.AdminMiembros
            .Where(m => m.AdministradorId == administradorId)
            .ToListAsync(ct);

        var ids = miembros.Select(m => m.UsuarioId).ToList();
        var users = await _db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Apellido, u.Email })
            .ToDictionaryAsync(u => u.Id, ct);

        var lista = miembros
            .Select(m =>
            {
                users.TryGetValue(m.UsuarioId, out var u);
                return new AdminMiembroDto(
                    m.UsuarioId,
                    u is null ? "—" : $"{u.Nombre} {u.Apellido}".Trim(),
                    u?.Email ?? "—",
                    m.EsGeneral, m.EsDueno, ParseAreas(m.AreasCsv));
            })
            .OrderByDescending(m => m.EsDueno).ThenBy(m => m.Nombre)
            .ToList();

        return Result<IReadOnlyList<AdminMiembroDto>>.Ok(lista);
    }

    public async Task<Result<AdminMiembroDto>> AgregarAsync(Guid administradorId, AgregarAdminDto dto, CancellationToken ct = default)
    {
        var email = dto.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<AdminMiembroDto>.Fail("Ingresá un correo válido.");

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
            return Result<AdminMiembroDto>.Fail("Ese correo no tiene una cuenta en Condolio. Pediles que se registren primero.");

        if (await _db.AdminMiembros.AnyAsync(m => m.AdministradorId == administradorId && m.UsuarioId == user.Id, ct))
            return Result<AdminMiembroDto>.Fail("Esa persona ya es administradora de esta cuenta.");

        // No permitir tomar al dueño de otro tenant.
        var esDuenoDeOtro = await _db.Administradores.AnyAsync(a => a.UsuarioId == user.Id && a.Id != administradorId, ct);
        if (esDuenoDeOtro)
            return Result<AdminMiembroDto>.Fail("Esa persona ya tiene su propia cuenta de administrador.");

        // Solo cuentas libres o ya vinculadas a este mismo tenant (residentes/staff del consorcio).
        if (user.AdministradorId is { } actual && actual != administradorId)
            return Result<AdminMiembroDto>.Fail("Esa persona ya pertenece a otra comunidad. Tiene que usar un correo sin cuenta.");

        var miembro = new AdminMiembro
        {
            AdministradorId = administradorId,
            UsuarioId = user.Id,
            EsGeneral = dto.EsGeneral,
            AreasCsv = dto.EsGeneral ? string.Empty : SerializeAreas(dto.Areas),
            EsDueno = false,
        };
        _db.AdminMiembros.Add(miembro);

        user.AdministradorId ??= administradorId;
        await _db.SaveChangesAsync(ct);

        if (!await _users.IsInRoleAsync(user, Roles.Administrador))
            await _users.AddToRoleAsync(user, Roles.Administrador);

        return Result<AdminMiembroDto>.Ok(new AdminMiembroDto(
            user.Id, user.NombreCompleto, user.Email ?? email, miembro.EsGeneral, false, ParseAreas(miembro.AreasCsv)));
    }

    public async Task<Result<AdminMiembroDto>> CambiarRolAsync(
        Guid administradorId, string usuarioId, GuardarRolAdminDto dto, CancellationToken ct = default)
    {
        await GarantizarDuenoAsync(administradorId, ct);
        var miembro = await _db.AdminMiembros
            .FirstOrDefaultAsync(m => m.AdministradorId == administradorId && m.UsuarioId == usuarioId, ct);
        if (miembro is null) return Result<AdminMiembroDto>.Fail("Administrador no encontrado.");

        if (!dto.EsGeneral)
        {
            if (miembro.EsDueno)
                return Result<AdminMiembroDto>.Fail("El dueño de la cuenta no puede tener acceso limitado.");
            var generales = await _db.AdminMiembros
                .CountAsync(m => m.AdministradorId == administradorId && m.EsGeneral, ct);
            if (miembro.EsGeneral && generales <= 1)
                return Result<AdminMiembroDto>.Fail("Tiene que quedar al menos un administrador general. Nombrá a otro primero.");
        }

        miembro.EsGeneral = dto.EsGeneral;
        miembro.AreasCsv = dto.EsGeneral ? string.Empty : SerializeAreas(dto.Areas);
        await _db.SaveChangesAsync(ct);

        var u = await _db.Users.Where(x => x.Id == usuarioId)
            .Select(x => new { x.Nombre, x.Apellido, x.Email }).FirstOrDefaultAsync(ct);
        return Result<AdminMiembroDto>.Ok(new AdminMiembroDto(
            usuarioId, u is null ? "—" : $"{u.Nombre} {u.Apellido}".Trim(), u?.Email ?? "—",
            miembro.EsGeneral, miembro.EsDueno, ParseAreas(miembro.AreasCsv)));
    }

    public async Task<Result> QuitarAsync(Guid administradorId, string usuarioId, CancellationToken ct = default)
    {
        var miembro = await _db.AdminMiembros
            .FirstOrDefaultAsync(m => m.AdministradorId == administradorId && m.UsuarioId == usuarioId, ct);
        if (miembro is null) return Result.Fail("Administrador no encontrado.");
        if (miembro.EsDueno) return Result.Fail("No podés quitar al dueño de la cuenta.");
        if (miembro.EsGeneral)
        {
            var generales = await _db.AdminMiembros
                .CountAsync(m => m.AdministradorId == administradorId && m.EsGeneral, ct);
            if (generales <= 1) return Result.Fail("Tiene que quedar al menos un administrador general.");
        }

        _db.AdminMiembros.Remove(miembro);
        await _db.SaveChangesAsync(ct);

        // Le sacamos el rol Administrador solo si no es admin en otro lado.
        var user = await _users.FindByIdAsync(usuarioId);
        if (user is not null)
        {
            var otras = await _db.AdminMiembros.AnyAsync(m => m.UsuarioId == usuarioId && m.AdministradorId != administradorId, ct);
            var esDuenoPropio = await _db.Administradores.AnyAsync(a => a.UsuarioId == usuarioId, ct);
            var sigueVinculado = await _db.UnidadPersonas.IgnoreQueryFilters().AnyAsync(p => p.UsuarioId == usuarioId, ct)
                || await _db.Personal.IgnoreQueryFilters().AnyAsync(p => p.UsuarioId == usuarioId, ct);

            if (!otras && !esDuenoPropio && await _users.IsInRoleAsync(user, Roles.Administrador))
                await _users.RemoveFromRoleAsync(user, Roles.Administrador);

            // Solo desvinculamos el tenant si no tiene ningún otro vínculo.
            if (!otras && !esDuenoPropio && !sigueVinculado)
            {
                user.AdministradorId = null;
                await _users.UpdateAsync(user);
            }
        }
        return Result.Ok();
    }

    /// <summary>Si el dueño del tenant no tiene fila en AdminMiembros, la crea.</summary>
    private async Task GarantizarDuenoAsync(Guid administradorId, CancellationToken ct)
    {
        var duenoId = await _db.Administradores.Where(a => a.Id == administradorId)
            .Select(a => a.UsuarioId).FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(duenoId)) return;

        if (!await _db.AdminMiembros.AnyAsync(m => m.AdministradorId == administradorId && m.UsuarioId == duenoId, ct))
        {
            _db.AdminMiembros.Add(new AdminMiembro
            {
                AdministradorId = administradorId,
                UsuarioId = duenoId,
                EsGeneral = true,
                EsDueno = true,
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    private static IReadOnlyList<AreaAdmin> ParseAreas(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<AreaAdmin>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => Enum.TryParse<AreaAdmin>(s, out _))
                .Select(Enum.Parse<AreaAdmin>)
                .Distinct()
                .ToArray();

    private static string SerializeAreas(IReadOnlyList<AreaAdmin>? areas) =>
        areas is null || areas.Count == 0 ? string.Empty : string.Join(',', areas.Distinct());
}
