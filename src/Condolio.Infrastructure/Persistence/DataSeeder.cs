using Condolio.Domain.Billing;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(
        CondolioDbContext db,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        SuperAdminSeed superAdmin)
    {
        await db.Database.MigrateAsync();

        foreach (var rol in Roles.Todos)
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));

        // ---- Plan por defecto (editable luego desde el panel del super admin) ----
        if (!await db.Planes.AnyAsync())
        {
            var plan = new Plan
            {
                Nombre = "Estándar",
                Moneda = "ARS",
                CargoBaseMensual = 0m,
                Tramos =
                {
                    new PlanTramo { DesdeUnidad = 0, HastaUnidad = 20, PrecioPorUnidad = 900m },
                    new PlanTramo { DesdeUnidad = 20, HastaUnidad = 50, PrecioPorUnidad = 750m },
                    new PlanTramo { DesdeUnidad = 50, HastaUnidad = null, PrecioPorUnidad = 600m },
                },
            };
            db.Planes.Add(plan);
            await db.SaveChangesAsync();
        }

        // ---- Super admin ----
        if (!string.IsNullOrWhiteSpace(superAdmin.Email) &&
            await userManager.FindByEmailAsync(superAdmin.Email) is null)
        {
            var user = new ApplicationUser
            {
                UserName = superAdmin.Email,
                Email = superAdmin.Email,
                EmailConfirmed = true,
                Nombre = superAdmin.Nombre,
                Apellido = superAdmin.Apellido,
            };
            var res = await userManager.CreateAsync(user, superAdmin.Password);
            if (res.Succeeded)
                await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        }
    }
}

public record SuperAdminSeed(string Email, string Password, string Nombre, string Apellido);
