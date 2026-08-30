import { Routes } from '@angular/router';
import { authGuard, rolGuard } from './core/guards/auth.guard';

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
      ...[
        'muro', 'notificaciones', 'config', 'amenidades', 'calendario', 'contactos',
        'documentos', 'finanzas', 'qr', 'historial-visitas', 'encuestas', 'informacion',
        'incidencias', 'paquetes',
      ].map((p) => ({
        path: p,
        loadComponent: () =>
          import('./features/portal/portal-proximamente.component').then((m) => m.PortalProximamenteComponent),
      })),
    ],
  },
  {
    path: 'panel',
    canActivate: [authGuard, rolGuard],
    data: { roles: ['Administrador', 'SuperAdmin'] },
    loadComponent: () =>
      import('./layout/admin-shell.component').then((m) => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'unidades' },
      {
        path: 'inicio',
        loadComponent: () =>
          import('./features/panel/panel-inicio.component').then((m) => m.PanelInicioComponent),
      },
      {
        path: 'unidades',
        loadComponent: () =>
          import('./features/unidades/unidades.component').then((m) => m.UnidadesComponent),
      },
      {
        path: 'unidades/:id',
        loadComponent: () =>
          import('./features/unidades/unidad-ficha.component').then((m) => m.UnidadFichaComponent),
      },
      { path: 'residentes', pathMatch: 'full', redirectTo: 'residentes/directorio' },
      {
        path: 'residentes/directorio',
        loadComponent: () =>
          import('./features/residentes/directorio.component').then((m) => m.DirectorioComponent),
      },
      {
        path: 'residentes/por-asignar',
        loadComponent: () =>
          import('./features/residentes/por-asignar.component').then((m) => m.PorAsignarComponent),
      },
      {
        path: 'residentes/invitaciones',
        loadComponent: () =>
          import('./features/residentes/invitaciones.component').then((m) => m.InvitacionesComponent),
      },
      { path: 'tickets', pathMatch: 'full', redirectTo: 'tickets/lista' },
      {
        path: 'tickets/lista',
        loadComponent: () =>
          import('./features/tickets/tickets-lista.component').then((m) => m.TicketsListaComponent),
      },
      {
        path: 'tickets/panel',
        loadComponent: () =>
          import('./features/tickets/tickets-panel.component').then((m) => m.TicketsPanelComponent),
      },
      {
        path: 'tickets/metricas',
        loadComponent: () =>
          import('./features/tickets/tickets-metricas.component').then((m) => m.TicketsMetricasComponent),
      },
      {
        path: 'tickets/:id',
        loadComponent: () =>
          import('./features/tickets/ticket-detalle.component').then((m) => m.TicketDetalleComponent),
      },
      {
        path: 'anuncios',
        loadComponent: () =>
          import('./features/anuncios/anuncios.component').then((m) => m.AnunciosComponent),
      },
      {
        path: 'calendario',
        loadComponent: () =>
          import('./features/calendario/calendario.component').then((m) => m.CalendarioComponent),
      },
      {
        path: 'documentos',
        loadComponent: () =>
          import('./features/documentos/documentos.component').then((m) => m.DocumentosComponent),
      },
      {
        path: 'encuestas',
        loadComponent: () =>
          import('./features/encuestas/encuestas.component').then((m) => m.EncuestasComponent),
      },
      { path: 'amenidades', pathMatch: 'full', redirectTo: 'amenidades/directorio' },
      {
        path: 'amenidades/directorio',
        loadComponent: () =>
          import('./features/amenidades/amenidades-directorio.component').then((m) => m.AmenidadesDirectorioComponent),
      },
      {
        path: 'amenidades/nueva',
        loadComponent: () =>
          import('./features/amenidades/nueva-amenidad.component').then((m) => m.NuevaAmenidadComponent),
      },
      {
        path: 'amenidades/reservaciones',
        loadComponent: () =>
          import('./features/amenidades/reservaciones.component').then((m) => m.ReservacionesComponent),
      },
      {
        path: 'amenidades/estadisticas',
        loadComponent: () =>
          import('./features/amenidades/amenidades-estadisticas.component').then((m) => m.AmenidadesEstadisticasComponent),
      },
      {
        path: 'amenidades/:id',
        loadComponent: () =>
          import('./features/amenidades/amenidad-detalle.component').then((m) => m.AmenidadDetalleComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
