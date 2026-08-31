using Condolio.Application.Common;
using Condolio.Application.Personal;
using Condolio.Domain.Personal;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Personal;

public class PersonalService : IPersonalService, ICredencialCasetaService
{
    private readonly CondolioDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public PersonalService(CondolioDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // ================= Staff =================

    public async Task<Result<PersonalListaDto>> ListarAsync(Guid consorcioId, string? busqueda, CancellationToken ct = default)
    {
        var q = (busqueda ?? "").Trim().ToLowerInvariant();
        var miembros = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId)
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

        var filtrados = miembros
            .Where(p => q.Length == 0 || (p.Nombre + " " + p.Apellido).ToLowerInvariant().Contains(q))
            .Select(ToDto).ToList();

        return Result<PersonalListaDto>.Ok(new PersonalListaDto(
            filtrados,
            miembros.Count,
            miembros.Count(p => p.Tipo == TipoPersonal.Seguridad),
            miembros.Count(p => p.UsuarioId != null)));
    }

    public async Task<Result<PersonalCreadoDto>> CrearAsync(Guid consorcioId, GuardarPersonalDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return Result<PersonalCreadoDto>.Fail("El nombre es obligatorio.");

        var admin = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => (Guid?)c.AdministradorId).FirstOrDefaultAsync(ct);
        if (admin is not { } adminId) return Result<PersonalCreadoDto>.Fail("Consorcio no encontrado.");

        var m = new MiembroPersonal
        {
            AdministradorId = adminId,
            ConsorcioId = consorcioId,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido?.Trim() ?? "",
            Tipo = dto.Tipo,
        };

        string? pass = null;
        if (!string.IsNullOrWhiteSpace(dto.EmailCuenta))
        {
            var (user, generada, error) = await CrearCuentaAsync(adminId, dto.EmailCuenta.Trim(), ct);
            if (error is not null) return Result<PersonalCreadoDto>.Fail(error);
            m.UsuarioId = user!.Id;
            m.Email = user.Email;
            pass = generada;
        }

        _db.Personal.Add(m);
        await _db.SaveChangesAsync(ct);
        return Result<PersonalCreadoDto>.Ok(new PersonalCreadoDto(ToDto(m), pass));
    }

    public async Task<Result<MiembroPersonalDto>> ActualizarAsync(Guid consorcioId, Guid id, GuardarPersonalDto dto, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (m is null) return Result<MiembroPersonalDto>.Fail("Personal no encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return Result<MiembroPersonalDto>.Fail("El nombre es obligatorio.");

        m.Nombre = dto.Nombre.Trim();
        m.Apellido = dto.Apellido?.Trim() ?? "";
        m.Tipo = dto.Tipo;
        await _db.SaveChangesAsync(ct);
        return Result<MiembroPersonalDto>.Ok(ToDto(m));
    }

    public async Task<Result> EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (m is null) return Result.Fail("Personal no encontrado.");

        if (m.UsuarioId is { } uid)
        {
            var user = await _users.FindByIdAsync(uid);
            if (user is not null) await _users.DeleteAsync(user);
        }
        _db.Personal.Remove(m);
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // ================= Credenciales de caseta =================

    public async Task<Result<CredencialesCasetaListaDto>> ListarAsync(Guid consorcioId, CancellationToken ct = default)
    {
        var disp = await _db.Personal.IgnoreQueryFilters()
            .Where(p => p.ConsorcioId == consorcioId && p.UsuarioId != null)
            .OrderByDescending(p => p.CreadoUtc)
            .ToListAsync(ct);

        var lista = disp.Select(p => new CredencialCasetaDto(p.Id, p.Nombre, p.Email ?? "—", p.Activo, p.CreadoUtc)).ToList();
        var ultimo = disp.OrderByDescending(p => p.CreadoUtc).FirstOrDefault();

        return Result<CredencialesCasetaListaDto>.Ok(new CredencialesCasetaListaDto(
            lista, disp.Count, disp.Count(p => p.Activo), ultimo?.CreadoUtc, ultimo?.Nombre));
    }

    public async Task<Result<CredencialGeneradaDto>> CrearAsync(Guid consorcioId, string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 2)
            return Result<CredencialGeneradaDto>.Fail("Ingresá un nombre para el dispositivo.");

        var datos = await _db.Consorcios.IgnoreQueryFilters()
            .Where(c => c.Id == consorcioId).Select(c => new { c.AdministradorId, c.Nombre }).FirstOrDefaultAsync(ct);
        if (datos is null) return Result<CredencialGeneradaDto>.Fail("Consorcio no encontrado.");

        var slug = Slug(datos.Nombre);
        var n = await _db.Personal.IgnoreQueryFilters().CountAsync(p => p.ConsorcioId == consorcioId && p.UsuarioId != null, ct) + 1;
        string email;
        do { email = $"{slug}-c{n}@caseta.condolio.app"; n++; }
        while (await _users.FindByEmailAsync(email) is not null);

        var (user, generada, error) = await CrearCuentaAsync(datos.AdministradorId, email, ct);
        if (error is not null) return Result<CredencialGeneradaDto>.Fail(error);

        var m = new MiembroPersonal
        {
            AdministradorId = datos.AdministradorId,
            ConsorcioId = consorcioId,
            Nombre = nombre.Trim(),
            Apellido = "",
            Tipo = TipoPersonal.Seguridad,
            UsuarioId = user!.Id,
            Email = email,
        };
        _db.Personal.Add(m);
        await _db.SaveChangesAsync(ct);

        return Result<CredencialGeneradaDto>.Ok(new CredencialGeneradaDto(
            new CredencialCasetaDto(m.Id, m.Nombre, email, true, m.CreadoUtc), email, generada!));
    }

    public async Task<Result<CredencialGeneradaDto>> RegenerarClaveAsync(Guid consorcioId, Guid id, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId && x.UsuarioId != null, ct);
        if (m is null) return Result<CredencialGeneradaDto>.Fail("Dispositivo no encontrado.");

        var user = await _users.FindByIdAsync(m.UsuarioId!);
        if (user is null) return Result<CredencialGeneradaDto>.Fail("Cuenta no encontrada.");

        var nueva = GenerarClave();
        var token = await _users.GeneratePasswordResetTokenAsync(user);
        var res = await _users.ResetPasswordAsync(user, token, nueva);
        if (!res.Succeeded)
            return Result<CredencialGeneradaDto>.Fail(string.Join(" ", res.Errors.Select(e => e.Description)));

        return Result<CredencialGeneradaDto>.Ok(new CredencialGeneradaDto(
            new CredencialCasetaDto(m.Id, m.Nombre, m.Email ?? "—", m.Activo, m.CreadoUtc), m.Email ?? "—", nueva));
    }

    public async Task<Result> CambiarEstadoAsync(Guid consorcioId, Guid id, bool activo, CancellationToken ct = default)
    {
        var m = await _db.Personal.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.ConsorcioId == consorcioId, ct);
        if (m is null) return Result.Fail("Dispositivo no encontrado.");
        m.Activo = activo;
        if (m.UsuarioId is { } uid)
        {
            var user = await _users.FindByIdAsync(uid);
            if (user is not null)
            {
                await _users.SetLockoutEnabledAsync(user, !activo);
                await _users.SetLockoutEndDateAsync(user, activo ? null : DateTimeOffset.MaxValue);
            }
        }
        await _db.SaveChangesAsync(ct);
        return Result.Ok();
    }

    // Eliminar credencial = eliminar miembro (reusa el de Staff)
    Task<Result> ICredencialCasetaService.EliminarAsync(Guid consorcioId, Guid id, CancellationToken ct) =>
        EliminarAsync(consorcioId, id, ct);

    // ---- helpers ----

    private async Task<(ApplicationUser? user, string? password, string? error)> CrearCuentaAsync(
        Guid adminId, string email, CancellationToken ct)
    {
        if (await _users.FindByEmailAsync(email) is not null)
            return (null, null, "Ya existe una cuenta con ese email.");

        var pass = GenerarClave();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = "Caseta",
            Apellido = "",
            AdministradorId = adminId,
        };
        var res = await _users.CreateAsync(user, pass);
        if (!res.Succeeded)
            return (null, null, string.Join(" ", res.Errors.Select(e => e.Description)));
        await _users.AddToRoleAsync(user, Roles.Personal);
        return (user, pass, null);
    }

    /// <summary>Clave temporal legible que cumple la política de Identity (mayús + minús + dígitos).</summary>
    private static string GenerarClave() => "Ac" + Random.Shared.Next(100_000, 999_999);

    private static string Slug(string s)
    {
        var limpio = new string(s.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        limpio = string.Join("-", limpio.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length == 0 ? "caseta" : limpio[..Math.Min(limpio.Length, 20)];
    }

    private static MiembroPersonalDto ToDto(MiembroPersonal m) => new(
        m.Id, m.Nombre, m.Apellido, m.Tipo, m.UsuarioId != null, m.Email, m.Activo);
}
