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
  {
    path: 'mi-unidad',
    canActivate: [authGuard, rolGuard],
    data: { roles: ['Residente'] },
    loadComponent: () =>
      import('./features/residente-portal/mi-unidad.component').then((m) => m.MiUnidadComponent),
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
    ],
  },
  { path: '**', redirectTo: '' },
];
