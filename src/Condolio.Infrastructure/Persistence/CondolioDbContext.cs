using Condolio.Application.Common;
using Condolio.Domain.Amenidades;
using Condolio.Domain.Archivos;
using Condolio.Domain.Billing;
using Condolio.Domain.Calendario;
using Condolio.Domain.Comunicaciones;
using Condolio.Domain.Documentos;
using Condolio.Domain.Common;
using Condolio.Domain.Consorcios;
using Condolio.Domain.Residentes;
using Condolio.Domain.Tenancy;
using Condolio.Domain.Tickets;
using Condolio.Domain.Unidades;
using Condolio.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Condolio.Infrastructure.Persistence;

public class CondolioDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ITenantContext _tenant;

    public CondolioDbContext(DbContextOptions<CondolioDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    // ---- Control plane ----
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<PlanTramo> PlanTramos => Set<PlanTramo>();

    // ---- Application plane (tenant-owned) ----
    public DbSet<Consorcio> Consorcios => Set<Consorcio>();
    public DbSet<Unidad> Unidades => Set<Unidad>();
    public DbSet<UnidadPersona> UnidadPersonas => Set<UnidadPersona>();
    public DbSet<UnidadNota> UnidadNotas => Set<UnidadNota>();
    public DbSet<UnidadActividad> UnidadActividades => Set<UnidadActividad>();
    public DbSet<UnidadIncidencia> UnidadIncidencias => Set<UnidadIncidencia>();
    public DbSet<IncidenciaComentario> IncidenciaComentarios => Set<IncidenciaComentario>();
    public DbSet<Adjunto> Adjuntos => Set<Adjunto>();
    public DbSet<Invitacion> Invitaciones => Set<Invitacion>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComentario> TicketComentarios => Set<TicketComentario>();
    public DbSet<Amenidad> Amenidades => Set<Amenidad>();
    public DbSet<AmenidadHorario> AmenidadHorarios => Set<AmenidadHorario>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Anuncio> Anuncios => Set<Anuncio>();
    public DbSet<AnuncioComentario> AnuncioComentarios => Set<AnuncioComentario>();
    public DbSet<AnuncioLike> AnuncioLikes => Set<AnuncioLike>();
    public DbSet<EventoCalendario> EventosCalendario => Set<EventoCalendario>();
    public DbSet<CarpetaDocumento> CarpetasDocumento => Set<CarpetaDocumento>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DocumentoAcceso> DocumentosAcceso => Set<DocumentoAcceso>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Administrador>(e =>
        {
            e.Property(x => x.RazonSocial).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Suscripcion).WithOne(x => x.Administrador)
                .HasForeignKey<Suscripcion>(x => x.AdministradorId);
        });

        builder.Entity<Suscripcion>(e =>
        {
            e.Property(x => x.ImporteMensual).HasPrecision(18, 2);
            e.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Plan>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Moneda).HasMaxLength(3).IsRequired();
            e.Property(x => x.CargoBaseMensual).HasPrecision(18, 2);
            e.HasMany(x => x.Tramos).WithOne(x => x.Plan).HasForeignKey(x => x.PlanId);
        });

        builder.Entity<PlanTramo>(e => e.Property(x => x.PrecioPorUnidad).HasPrecision(18, 4));

        // ---- Query filter global de multi-tenancy ----
        // SuperAdmin (TenantIdActual == null) ve todo; un administrador ve solo sus filas.
        // EF re-evalúa TenantIdActual por query porque es un miembro de instancia del DbContext.
        builder.Entity<Consorcio>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Direccion).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.AdministradorId);
            e.HasMany(x => x.Unidades).WithOne(x => x.Consorcio).HasForeignKey(x => x.ConsorcioId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Unidad>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            e.Property(x => x.Seccion).HasMaxLength(80);
            e.Property(x => x.CuotaMantenimiento).HasPrecision(18, 2);
            e.Property(x => x.AreaM2).HasPrecision(10, 2);
            e.Property(x => x.Coeficiente).HasPrecision(9, 6);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId });
            e.HasMany(x => x.Personas).WithOne(x => x.Unidad).HasForeignKey(x => x.UnidadId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<UnidadPersona>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(120);
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => new { x.AdministradorId, x.UnidadId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<UnidadNota>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(4000).IsRequired();
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.UnidadId });
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<UnidadActividad>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(160).IsRequired();
            e.Property(x => x.Detalle).HasMaxLength(600);
            e.Property(x => x.ActorUsuarioId).HasMaxLength(450);
            e.HasIndex(x => new { x.AdministradorId, x.UnidadId });
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<UnidadIncidencia>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200);
            e.Property(x => x.Descripcion).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Etiquetas).HasMaxLength(500);
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.UnidadId });
            e.HasOne(x => x.Unidad).WithMany().HasForeignKey(x => x.UnidadId);
            e.HasMany(x => x.Comentarios).WithOne(x => x.Incidencia).HasForeignKey(x => x.IncidenciaId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<IncidenciaComentario>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.IncidenciaId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Adjunto>(e =>
        {
            e.Property(x => x.NombreArchivo).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.RutaRelativa).HasMaxLength(400).IsRequired();
            e.Property(x => x.SubidoPorUsuarioId).HasMaxLength(450);
            e.Ignore(x => x.EsImagen);
            e.HasIndex(x => new { x.AdministradorId, x.OwnerTipo, x.OwnerId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Invitacion>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Token).HasMaxLength(64).IsRequired();
            e.Property(x => x.InvitadoPorUsuarioId).HasMaxLength(450);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Estado });
            e.HasOne(x => x.Consorcio).WithMany().HasForeignKey(x => x.ConsorcioId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Ticket>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200);
            e.Property(x => x.Descripcion).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Etiquetas).HasMaxLength(500);
            e.Property(x => x.Ubicacion).HasMaxLength(200);
            e.Property(x => x.ReportadoPorUsuarioId).HasMaxLength(450);
            e.Property(x => x.ReportadoPorNombre).HasMaxLength(200);
            e.Property(x => x.AsignadoAUsuarioId).HasMaxLength(450);
            e.Property(x => x.AsignadoANombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Estado });
            e.HasIndex(x => new { x.ConsorcioId, x.Numero }).IsUnique();
            e.HasMany(x => x.Comentarios).WithOne(x => x.Ticket).HasForeignKey(x => x.TicketId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<TicketComentario>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.TicketId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Amenidad>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(2000);
            e.Property(x => x.Tarifa).HasPrecision(12, 2);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId });
            e.HasMany(x => x.Horarios).WithOne(x => x.Amenidad).HasForeignKey(x => x.AmenidadId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<AmenidadHorario>(e =>
        {
            e.HasIndex(x => new { x.AdministradorId, x.AmenidadId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Reserva>(e =>
        {
            e.Property(x => x.SolicitanteUsuarioId).HasMaxLength(450);
            e.Property(x => x.SolicitanteNombre).HasMaxLength(200);
            e.Property(x => x.Nota).HasMaxLength(1000);
            e.Property(x => x.Importe).HasPrecision(12, 2);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Estado });
            e.HasIndex(x => new { x.AmenidadId, x.Inicio });
            e.HasOne(x => x.Amenidad).WithMany().HasForeignKey(x => x.AmenidadId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Anuncio>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Cuerpo).HasMaxLength(8000).IsRequired();
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450);
            e.Property(x => x.AutorNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Fijado, x.PublicadoUtc });
            e.HasMany(x => x.Comentarios).WithOne(x => x.Anuncio).HasForeignKey(x => x.AnuncioId);
            e.HasMany(x => x.Likes).WithOne(x => x.Anuncio).HasForeignKey(x => x.AnuncioId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<AnuncioComentario>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(4000).IsRequired();
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450);
            e.Property(x => x.AutorNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.AnuncioId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<AnuncioLike>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.Property(x => x.UsuarioNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AnuncioId, x.UsuarioId }).IsUnique();
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<EventoCalendario>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(4000);
            e.Property(x => x.Ubicacion).HasMaxLength(200);
            e.Property(x => x.CreadoPorUsuarioId).HasMaxLength(450);
            e.Property(x => x.CreadoPorNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.InicioUtc });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<CarpetaDocumento>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.CarpetaPadreId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Documento>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.RutaRelativa).HasMaxLength(400).IsRequired();
            e.Property(x => x.SubidoPorUsuarioId).HasMaxLength(450);
            e.Property(x => x.SubidoPorNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.CarpetaId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<DocumentoAcceso>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.DocumentoId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });
    }

    /// <summary>Tenant efectivo para el query filter (null = sin filtro, p. ej. SuperAdmin).</summary>
    public Guid? TenantIdActual => _tenant.EsSuperAdmin ? null : _tenant.AdministradorId;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreadoUtc = ahora;
            if (entry.State == EntityState.Modified) entry.Entity.ActualizadoUtc = ahora;
        }

        // Completa AdministradorId en altas del application plane.
        if (_tenant.AdministradorId is { } tenantId)
        {
            foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
            {
                if (entry.State == EntityState.Added && entry.Entity.AdministradorId == Guid.Empty)
                    entry.Entity.AdministradorId = tenantId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
