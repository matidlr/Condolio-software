using Condolio.Application.Archivos;
using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Application.Consorcios;
using Condolio.Application.Residentes;
using Condolio.Application.Unidades;
using Condolio.Infrastructure.Archivos;
using Condolio.Infrastructure.Auth;
using Condolio.Infrastructure.Billing;
using Condolio.Infrastructure.Consorcios;
using Condolio.Infrastructure.Email;
using Condolio.Infrastructure.Unidades;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Condolio.Infrastructure.Tenancy;
using Condolio.Infrastructure.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Condolio.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");
        // Versión fija para que `dotnet ef` no necesite un MySQL vivo en tiempo de diseño.
        var serverVersion = new MySqlServerVersion(new Version(8, 4, 0));
        services.AddDbContext<CondolioDbContext>(options =>
            options.UseMySql(connectionString, serverVersion));

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddSingleton<IClock, SystemClock>();
        if (SmtpEmailSender.Configurado(config))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddScoped<IEmailSender, LogEmailSender>();

        services
            .AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.User.RequireUniqueEmail = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<CondolioDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(config.GetSection("Jwt"));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // ---- Billing (control plane) ----
        services.AddScoped<ISuscripcionService, SuscripcionService>();

        // ---- Application plane ----
        services.AddScoped<IConsorcioService, ConsorcioService>();
        services.AddScoped<IActividadUnidadService, ActividadUnidadService>();
        services.AddScoped<IUnidadService, UnidadService>();
        services.AddScoped<INotaUnidadService, NotaUnidadService>();
        services.AddScoped<IIncidenciaUnidadService, IncidenciaUnidadService>();
        services.AddScoped<IResidenteService, Condolio.Infrastructure.Residentes.ResidenteService>();
        services.AddScoped<IInvitacionPublicaService, Condolio.Infrastructure.Residentes.InvitacionPublicaService>();
        services.AddScoped<IVistaResidenteService, Condolio.Infrastructure.Residentes.VistaResidenteService>();

        // ---- Archivos ----
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IAdjuntoService, AdjuntoService>();

        return services;
    }
}
