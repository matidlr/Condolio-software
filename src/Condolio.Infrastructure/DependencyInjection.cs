using Condolio.Application.Accesos;
using Condolio.Application.Archivos;
using Condolio.Application.Billing;
using Condolio.Application.Common;
using Condolio.Application.Consorcios;
using Condolio.Application.Contactos;
using Condolio.Application.Amenidades;
using Condolio.Application.Calendario;
using Condolio.Application.Comunicaciones;
using Condolio.Application.Documentos;
using Condolio.Application.Encuestas;
using Condolio.Application.Notificaciones;
using Condolio.Application.Panel;
using Condolio.Application.Residentes;
using Condolio.Application.Tickets;
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
                o.Password.RequiredLength = 6;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireDigit = true;
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
        services.AddScoped<Condolio.Application.Consorcios.IPreferenciasConsorcioService, Condolio.Infrastructure.Consorcios.PreferenciasConsorcioService>();
        services.AddScoped<Condolio.Application.Expensas.IExpensasConfigService, Condolio.Infrastructure.Expensas.ExpensasConfigService>();
        services.AddScoped<Condolio.Application.Expensas.IPeriodosExpensasService, Condolio.Infrastructure.Expensas.PeriodosExpensasService>();
        services.AddScoped<IActividadUnidadService, ActividadUnidadService>();
        services.AddScoped<IUnidadService, UnidadService>();
        services.AddScoped<INotaUnidadService, NotaUnidadService>();
        services.AddScoped<IIncidenciaUnidadService, IncidenciaUnidadService>();
        services.AddScoped<IResidenteService, Condolio.Infrastructure.Residentes.ResidenteService>();
        services.AddScoped<IInvitacionPublicaService, Condolio.Infrastructure.Residentes.InvitacionPublicaService>();
        services.AddScoped<IVistaResidenteService, Condolio.Infrastructure.Residentes.VistaResidenteService>();
        services.AddScoped<IMiPortalService, Condolio.Infrastructure.Residentes.MiPortalService>();
        services.AddScoped<IPaseAccesoService, Condolio.Infrastructure.Accesos.PaseAccesoService>();
        services.AddScoped<IAccesoAdminService, Condolio.Infrastructure.Accesos.AccesoAdminService>();
        services.AddScoped<Condolio.Application.Paqueteria.IPaqueteriaService, Condolio.Infrastructure.Paqueteria.PaqueteriaService>();
        services.AddScoped<Condolio.Infrastructure.Personal.PersonalService>();
        services.AddScoped<Condolio.Application.Personal.IPersonalService>(sp => sp.GetRequiredService<Condolio.Infrastructure.Personal.PersonalService>());
        services.AddScoped<Condolio.Application.Personal.ICredencialCasetaService>(sp => sp.GetRequiredService<Condolio.Infrastructure.Personal.PersonalService>());
        services.AddScoped<IContactoService, Condolio.Infrastructure.Contactos.ContactoService>();
        services.AddScoped<ITicketService, Condolio.Infrastructure.Tickets.TicketService>();
        services.AddScoped<IAmenidadService, Condolio.Infrastructure.Amenidades.AmenidadService>();
        services.AddScoped<IReservaService, Condolio.Infrastructure.Amenidades.ReservaService>();
        services.AddScoped<IAnuncioService, Condolio.Infrastructure.Comunicaciones.AnuncioService>();
        services.AddScoped<IEventoService, Condolio.Infrastructure.Calendario.EventoService>();
        services.AddScoped<IDocumentoService, Condolio.Infrastructure.Documentos.DocumentoService>();
        services.AddScoped<IEncuestaService, Condolio.Infrastructure.Encuestas.EncuestaService>();
        services.AddScoped<INotificacionService, Condolio.Infrastructure.Notificaciones.NotificacionService>();
        services.AddScoped<IPanelService, Condolio.Infrastructure.Panel.PanelService>();

        // ---- Archivos ----
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IAdjuntoService, AdjuntoService>();
        services.AddScoped<Condolio.Application.Tenancy.IAdminMiembroService, Condolio.Infrastructure.Tenancy.AdminMiembroService>();

        return services;
    }
}
