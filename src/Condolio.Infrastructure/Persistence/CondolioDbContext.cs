using Condolio.Application.Common;
using Condolio.Domain.Accesos;
using Condolio.Domain.Amenidades;
using Condolio.Domain.Archivos;
using Condolio.Domain.Billing;
using Condolio.Domain.Calendario;
using Condolio.Domain.Comunicaciones;
using Condolio.Domain.Contactos;
using Condolio.Domain.Documentos;
using Condolio.Domain.Encuestas;
using Condolio.Domain.Notificaciones;
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
    public DbSet<TicketEvento> TicketEventos => Set<TicketEvento>();
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
    public DbSet<Encuesta> Encuestas => Set<Encuesta>();
    public DbSet<OpcionEncuesta> OpcionesEncuesta => Set<OpcionEncuesta>();
    public DbSet<VotoEncuesta> VotosEncuesta => Set<VotoEncuesta>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PreferenciasNotificacion> PreferenciasNotificacion => Set<PreferenciasNotificacion>();
    public DbSet<PaseAcceso> PasesAcceso => Set<PaseAcceso>();
    public DbSet<RegistroVisita> RegistrosVisita => Set<RegistroVisita>();
    public DbSet<Contacto> Contactos => Set<Contacto>();
    public DbSet<Condolio.Domain.Personal.MiembroPersonal> Personal => Set<Condolio.Domain.Personal.MiembroPersonal>();
    public DbSet<Condolio.Domain.Personal.TurnoPorteria> TurnosPorteria => Set<Condolio.Domain.Personal.TurnoPorteria>();
    public DbSet<Condolio.Domain.Paqueteria.Paquete> Paquetes => Set<Condolio.Domain.Paqueteria.Paquete>();
    public DbSet<Condolio.Domain.Residentes.MiembroJunta> MiembrosJunta => Set<Condolio.Domain.Residentes.MiembroJunta>();
    public DbSet<Condolio.Domain.Tenancy.AdminMiembro> AdminMiembros => Set<Condolio.Domain.Tenancy.AdminMiembro>();
    public DbSet<Condolio.Domain.Tenancy.InvitacionAdmin> InvitacionesAdmin => Set<Condolio.Domain.Tenancy.InvitacionAdmin>();
    public DbSet<Condolio.Domain.Consorcios.PreferenciasConsorcio> PreferenciasConsorcio => Set<Condolio.Domain.Consorcios.PreferenciasConsorcio>();

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
            e.HasMany(x => x.Eventos).WithOne(x => x.Ticket).HasForeignKey(x => x.TicketId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<TicketComentario>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.TicketId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<TicketEvento>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(500).IsRequired();
            e.Property(x => x.ActorUsuarioId).HasMaxLength(450);
            e.Property(x => x.ActorNombre).HasMaxLength(200);
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

        builder.Entity<Encuesta>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(4000);
            e.Property(x => x.AutorUsuarioId).HasMaxLength(450);
            e.Property(x => x.AutorNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Estado });
            e.HasMany(x => x.Opciones).WithOne(x => x.Encuesta).HasForeignKey(x => x.EncuestaId);
            e.HasMany(x => x.Votos).WithOne(x => x.Encuesta).HasForeignKey(x => x.EncuestaId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<OpcionEncuesta>(e =>
        {
            e.Property(x => x.Texto).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.AdministradorId, x.EncuestaId });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<VotoEncuesta>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.Property(x => x.UsuarioNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.EncuestaId, x.UsuarioId, x.OpcionId }).IsUnique();
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Notificacion>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            e.Property(x => x.Cuerpo).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Enlace).HasMaxLength(400);
            e.Property(x => x.DestinatarioUsuarioId).HasMaxLength(450);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.LeidaUtc });
            e.HasIndex(x => new { x.DestinatarioUsuarioId, x.LeidaUtc });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<PreferenciasNotificacion>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => x.UsuarioId).IsUnique();
        });

        builder.Entity<PaseAcceso>(e =>
        {
            e.Property(x => x.CreadoPorUsuarioId).HasMaxLength(450);
            e.Property(x => x.CreadoPorNombre).HasMaxLength(200);
            e.Property(x => x.VisitanteNombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Patente).HasMaxLength(20);
            e.Property(x => x.Token).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Estado });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<RegistroVisita>(e =>
        {
            e.Property(x => x.VisitanteNombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Patente).HasMaxLength(20);
            e.Property(x => x.RegistradoPorUsuarioId).HasMaxLength(450);
            e.Property(x => x.RegistradoPorNombre).HasMaxLength(200);
            e.Property(x => x.Nota).HasMaxLength(500);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.UnidadId, x.IngresoUtc });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Contacto>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
            e.Property(x => x.Categoria).HasMaxLength(80).IsRequired();
            e.Property(x => x.Telefono).HasMaxLength(40).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Empresa).HasMaxLength(200);
            e.Property(x => x.Notas).HasMaxLength(500);
            e.Property(x => x.CreadoPorUsuarioId).HasMaxLength(450);
            e.Property(x => x.CreadoPorNombre).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId, x.Categoria });
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Condolio.Domain.Personal.MiembroPersonal>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Apellido).HasMaxLength(120).IsRequired();
            e.Property(x => x.UsuarioId).HasMaxLength(450);
            e.Property(x => x.Email).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.ConsorcioId });
            e.HasIndex(x => x.UsuarioId);
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Condolio.Domain.Personal.TurnoPorteria>(e =>
        {
            e.Property(x => x.CredencialUsuarioId).HasMaxLength(450).IsRequired();
            e.Property(x => x.PersonalNombre).HasMaxLength(240).IsRequired();
            e.Property(x => x.NotasCierre).HasMaxLength(1000);
            e.HasIndex(x => new { x.CredencialUsuarioId, x.FinUtc });
            e.HasIndex(x => new { x.ConsorcioId, x.InicioUtc });
        });

        builder.Entity<Condolio.Domain.Residentes.MiembroJunta>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.HasIndex(x => new { x.ConsorcioId, x.UsuarioId, x.Cargo }).IsUnique();
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Condolio.Domain.Tenancy.AdminMiembro>(e =>
        {
            e.Property(x => x.UsuarioId).HasMaxLength(450).IsRequired();
            e.Property(x => x.AreasCsv).HasMaxLength(200);
            e.HasIndex(x => new { x.AdministradorId, x.UsuarioId }).IsUnique();
        });

        builder.Entity<Condolio.Domain.Tenancy.InvitacionAdmin>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.AreasCsv).HasMaxLength(200);
            e.Property(x => x.InvitadoPorUsuarioId).HasMaxLength(450);
            e.HasIndex(x => new { x.Email, x.Estado });
        });

        builder.Entity<Condolio.Domain.Consorcios.PreferenciasConsorcio>(e =>
        {
            e.HasIndex(x => x.ConsorcioId).IsUnique();
            e.HasQueryFilter(x => TenantIdActual == null || x.AdministradorId == TenantIdActual);
        });

        builder.Entity<Condolio.Domain.Paqueteria.Paquete>(e =>
        {
            e.Property(x => x.UnidadNombre).HasMaxLength(120).IsRequired();
            e.Property(x => x.Transportista).HasMaxLength(120);
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.RegistradoPorNombre).HasMaxLength(240);
            e.Property(x => x.EntregadoPorNombre).HasMaxLength(240);
            e.Property(x => x.RetiradoPorNombre).HasMaxLength(240);
            e.Property(x => x.FotoRuta).HasMaxLength(300);
            e.HasIndex(x => new { x.ConsorcioId, x.Estado, x.LlegadaUtc });
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
