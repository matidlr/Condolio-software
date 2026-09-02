import { Routes } from '@angular/router';
import { authGuard, rolGuard, areaGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/landing/landing.component').then((m) => m.LandingComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'registro',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'verificar',
    loadComponent: () =>
      import('./features/auth/verificar.component').then((m) => m.VerificarComponent),
  },
  {
    path: 'invitacion/:token',
    loadComponent: () =>
      import('./features/auth/invitacion.component').then((m) => m.InvitacionComponent),
  },
  { path: 'mi-unidad', pathMatch: 'full', redirectTo: 'portal/inicio' },
  {
    path: 'porteria',
    canActivate: [authGuard, rolGuard],
    data: { roles: ['Personal'] },
    loadComponent: () =>
      import('./features/porteria/porteria.component').then((m) => m.PorteriaComponent),
  },
  {
    path: 'portal',
    canActivate: [authGuard, rolGuard],
    data: { roles: ['Residente'] },
    loadComponent: () =>
      import('./layout/portal-shell.component').then((m) => m.PortalShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'casa' },
      { path: 'inicio', pathMatch: 'full', redirectTo: 'casa' },
      {
        path: 'casa',
        loadComponent: () =>
          import('./features/portal/portal-casa.component').then((m) => m.PortalCasaComponent),
      },
      {
        path: 'menu',
        loadComponent: () =>
          import('./features/portal/portal-menu.component').then((m) => m.PortalMenuComponent),
      },
      {
        path: 'mi-unidad',
        loadComponent: () =>
          import('./features/residente-portal/mi-unidad.component').then((m) => m.MiUnidadComponent),
      },
      {
        path: 'qr',
        loadComponent: () =>
          import('./features/portal/portal-qr.component').then((m) => m.PortalQrComponent),
      },
      {
        path: 'amenidades',
        loadComponent: () =>
          import('./features/portal/portal-amenidades.component').then((m) => m.PortalAmenidadesComponent),
      },
      {
        path: 'reservas/:id',
        loadComponent: () =>
          import('./features/portal/portal-reserva-detalle.component').then((m) => m.PortalReservaDetalleComponent),
      },
      {
        path: 'calendario',
        loadComponent: () =>
          import('./features/portal/portal-calendario.component').then((m) => m.PortalCalendarioComponent),
      },
      {
        path: 'documentos',
        loadComponent: () =>
          import('./features/portal/portal-documentos.component').then((m) => m.PortalDocumentosComponent),
      },
      {
        path: 'contactos',
        loadComponent: () =>
          import('./features/portal/portal-contactos.component').then((m) => m.PortalContactosComponent),
      },
      {
        path: 'encuestas',
        loadComponent: () =>
          import('./features/portal/portal-encuestas.component').then((m) => m.PortalEncuestasComponent),
      },
      {
        path: 'incidencias',
        loadComponent: () =>
          import('./features/portal/portal-incidencias.component').then((m) => m.PortalIncidenciasComponent),
      },
      {
        path: 'incidencias/:id',
        loadComponent: () =>
          import('./features/portal/portal-incidencias.component').then((m) => m.PortalIncidenciasComponent),
      },
      {
        path: 'muro',
        loadComponent: () =>
          import('./features/portal/portal-muro.component').then((m) => m.PortalMuroComponent),
      },
      {
        path: 'historial-visitas',
        loadComponent: () =>
          import('./features/portal/portal-visitas.component').then((m) => m.PortalVisitasComponent),
      },
      {
        path: 'informacion',
        loadComponent: () =>
          import('./features/portal/portal-informacion.component').then((m) => m.PortalInformacionComponent),
      },
      {
        path: 'notificaciones',
        loadComponent: () =>
          import('./features/portal/portal-notificaciones.component').then((m) => m.PortalNotificacionesComponent),
      },
      {
        path: 'config',
        loadComponent: () =>
          import('./features/portal/portal-config.component').then((m) => m.PortalConfigComponent),
      },
      {
        path: 'config/notificaciones',
        loadComponent: () =>
          import('./features/portal/portal-notif-preferencias.component').then((m) => m.PortalNotifPreferenciasComponent),
      },
      {
        path: 'config/mi-info',
        loadComponent: () =>
          import('./features/portal/portal-mi-info.component').then((m) => m.PortalMiInfoComponent),
      },
      {
        path: 'config/seguridad',
        loadComponent: () =>
          import('./features/portal/portal-seguridad.component').then((m) => m.PortalSeguridadComponent),
      },
      ...[
        'finanzas', 'paquetes',
      ].map((p) => ({
        path: p,
        loadComponent: () =>
          import('./features/portal/portal-proximamente.component').then((m) => m.PortalProximamenteComponent),
      })),
    ],
  },
  {
    path: 'nueva-sociedad',
    canActivate: [authGuard, rolGuard],
    data: { roles: ['Administrador', 'SuperAdmin'] },
    loadComponent: () =>
      import('./features/consorcios/nueva-sociedad.component').then((m) => m.NuevaSociedadComponent),
  },
  {
    path: 'panel',
    canActivate: [authGuard, rolGuard],
    data: { roles: ['Administrador', 'SuperAdmin'] },
    loadComponent: () =>
      import('./layout/admin-shell.component').then((m) => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'inicio' },
      {
        path: 'inicio',
        loadComponent: () =>
          import('./features/panel/panel-inicio.component').then((m) => m.PanelInicioComponent),
      },
      {
        path: 'unidades',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/unidades/unidades.component').then((m) => m.UnidadesComponent),
      },
      {
        path: 'unidades/:id',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/unidades/unidad-ficha.component').then((m) => m.UnidadFichaComponent),
      },
      { path: 'residentes', pathMatch: 'full', redirectTo: 'residentes/directorio' },
      {
        path: 'residentes/directorio',
        canActivate: [areaGuard],
        data: { area: 'Residentes' },
        loadComponent: () =>
          import('./features/residentes/directorio.component').then((m) => m.DirectorioComponent),
      },
      {
        path: 'residentes/por-asignar',
        canActivate: [areaGuard],
        data: { area: 'Residentes' },
        loadComponent: () =>
          import('./features/residentes/por-asignar.component').then((m) => m.PorAsignarComponent),
      },
      {
        path: 'residentes/invitaciones',
        canActivate: [areaGuard],
        data: { area: 'Residentes' },
        loadComponent: () =>
          import('./features/residentes/invitaciones.component').then((m) => m.InvitacionesComponent),
      },
      { path: 'tickets', pathMatch: 'full', redirectTo: 'tickets/lista' },
      {
        path: 'tickets/lista',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/tickets/tickets-lista.component').then((m) => m.TicketsListaComponent),
      },
      {
        path: 'tickets/panel',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/tickets/tickets-panel.component').then((m) => m.TicketsPanelComponent),
      },
      {
        path: 'tickets/metricas',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/tickets/tickets-metricas.component').then((m) => m.TicketsMetricasComponent),
      },
      {
        path: 'tickets/:id',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/tickets/ticket-detalle.component').then((m) => m.TicketDetalleComponent),
      },
      {
        path: 'anuncios',
        canActivate: [areaGuard],
        data: { area: 'Comunicacion' },
        loadComponent: () =>
          import('./features/anuncios/anuncios.component').then((m) => m.AnunciosComponent),
      },
      {
        path: 'paqueteria',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/paqueteria/paqueteria.component').then((m) => m.PaqueteriaComponent),
      },
      {
        path: 'configuracion',
        canActivate: [areaGuard],
        data: { soloGeneral: true },
        loadComponent: () =>
          import('./features/configuracion/configuracion.component').then((m) => m.ConfiguracionComponent),
      },
      { path: 'expensas', pathMatch: 'full', redirectTo: 'expensas/proveedores' },
      {
        path: 'expensas/proveedores',
        canActivate: [areaGuard],
        data: { area: 'Finanzas' },
        loadComponent: () =>
          import('./features/expensas/proveedores.component').then((m) => m.ProveedoresComponent),
      },
      {
        path: 'expensas/gastos-fijos',
        canActivate: [areaGuard],
        data: { area: 'Finanzas' },
        loadComponent: () =>
          import('./features/expensas/gastos-fijos.component').then((m) => m.GastosFijosComponent),
      },
      {
        path: 'expensas/extraordinarias',
        canActivate: [areaGuard],
        data: { area: 'Finanzas' },
        loadComponent: () =>
          import('./features/expensas/extraordinarias.component').then((m) => m.ExtraordinariasComponent),
      },
      {
        path: 'expensas/configuracion',
        canActivate: [areaGuard],
        data: { area: 'Finanzas' },
        loadComponent: () =>
          import('./features/expensas/expensas-config.component').then((m) => m.ExpensasConfigComponent),
      },
      {
        path: 'calendario',
        canActivate: [areaGuard],
        data: { area: 'Comunicacion' },
        loadComponent: () =>
          import('./features/calendario/calendario.component').then((m) => m.CalendarioComponent),
      },
      {
        path: 'documentos',
        canActivate: [areaGuard],
        data: { area: 'Comunicacion' },
        loadComponent: () =>
          import('./features/documentos/documentos.component').then((m) => m.DocumentosComponent),
      },
      {
        path: 'encuestas',
        canActivate: [areaGuard],
        data: { area: 'Comunicacion' },
        loadComponent: () =>
          import('./features/encuestas/encuestas.component').then((m) => m.EncuestasComponent),
      },
      { path: 'seguridad', pathMatch: 'full', redirectTo: 'seguridad/bitacora' },
      {
        path: 'seguridad/bitacora',
        canActivate: [areaGuard],
        data: { area: 'Seguridad' },
        loadComponent: () =>
          import('./features/seguridad/bitacora.component').then((m) => m.BitacoraComponent),
      },
      {
        path: 'seguridad/qr',
        canActivate: [areaGuard],
        data: { area: 'Seguridad' },
        loadComponent: () =>
          import('./features/seguridad/qr-admin.component').then((m) => m.QrAdminComponent),
      },
      {
        path: 'seguridad/staff',
        canActivate: [areaGuard],
        data: { area: 'Seguridad' },
        loadComponent: () =>
          import('./features/seguridad/staff.component').then((m) => m.StaffComponent),
      },
      {
        path: 'seguridad/credenciales',
        canActivate: [areaGuard],
        data: { area: 'Seguridad' },
        loadComponent: () =>
          import('./features/seguridad/credenciales.component').then((m) => m.CredencialesCasetaComponent),
      },
      {
        path: 'encuestas/:id',
        canActivate: [areaGuard],
        data: { area: 'Comunicacion' },
        loadComponent: () =>
          import('./features/encuestas/encuesta-detalle.component').then((m) => m.EncuestaDetalleComponent),
      },
      { path: 'amenidades', pathMatch: 'full', redirectTo: 'amenidades/directorio' },
      {
        path: 'amenidades/directorio',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/amenidades/amenidades-directorio.component').then((m) => m.AmenidadesDirectorioComponent),
      },
      {
        path: 'amenidades/nueva',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/amenidades/nueva-amenidad.component').then((m) => m.NuevaAmenidadComponent),
      },
      {
        path: 'amenidades/reservaciones',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/amenidades/reservaciones.component').then((m) => m.ReservacionesComponent),
      },
      {
        path: 'amenidades/estadisticas',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/amenidades/amenidades-estadisticas.component').then((m) => m.AmenidadesEstadisticasComponent),
      },
      {
        path: 'amenidades/:id',
        canActivate: [areaGuard],
        data: { area: 'Operacion' },
        loadComponent: () =>
          import('./features/amenidades/amenidad-detalle.component').then((m) => m.AmenidadDetalleComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
