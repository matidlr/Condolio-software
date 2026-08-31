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

        // ---- Plan por defecto: $700 ARS por unidad por mes, sin cargo fijo ----
        var planId = await db.Planes.AsNoTracking()
            .Where(p => p.Activo).Select(p => (Guid?)p.Id).FirstOrDefaultAsync();
        if (planId is null)
        {
            db.Planes.Add(new Plan
            {
                Nombre = "Estándar",
                Moneda = "ARS",
                CargoBaseMensual = 0m,
                Tramos = { new PlanTramo { DesdeUnidad = 0, HastaUnidad = null, PrecioPorUnidad = 700m } },
            });
            await db.SaveChangesAsync();
        }
        else
        {
            var tramos = await db.PlanTramos.AsNoTracking().Where(t => t.PlanId == planId).ToListAsync();
            var yaEsCanonico = tramos is [{ PrecioPorUnidad: 700m, HastaUnidad: null, DesdeUnidad: 0 }];
            if (!yaEsCanonico)
            {
                await db.PlanTramos.Where(t => t.PlanId == planId).ExecuteDeleteAsync();
                await db.Planes.Where(p => p.Id == planId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.CargoBaseMensual, 0m).SetProperty(p => p.Moneda, "ARS"));
                db.PlanTramos.Add(new PlanTramo { PlanId = planId.Value, DesdeUnidad = 0, HastaUnidad = null, PrecioPorUnidad = 700m });
                await db.SaveChangesAsync();
            }
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
