import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AreaAdmin, Rol } from '../models/auth.models';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.autenticado() ? true : router.createUrlTree(['/login']);
};

/** Uso: `canActivate: [rolGuard], data: { roles: ['Administrador'] }` */
export const rolGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const requeridos = (route.data?.['roles'] as Rol[] | undefined) ?? [];

  if (!auth.autenticado()) return router.createUrlTree(['/login']);
  if (requeridos.length && !auth.tieneRol(...requeridos)) {
    return router.createUrlTree([auth.rutaInicio()]);
  }
  return true;
};

/**
 * Restringe una ruta del panel a administradores con acceso a un área
 * (o generales). Uso: `canActivate: [areaGuard], data: { area: 'Operacion' }`.
 * Con `data: { soloGeneral: true }` exige acceso general.
 */
export const areaGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (route.data?.['soloGeneral'] && !auth.adminGeneral()) {
    return router.createUrlTree(['/panel/inicio']);
  }
  const area = route.data?.['area'] as AreaAdmin | undefined;
  if (area && !auth.puedeArea(area)) {
    return router.createUrlTree(['/panel/inicio']);
  }
  return true;
};
