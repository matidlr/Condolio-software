import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PortalService } from '../../core/services/portal.service';

interface Tile {
  label: string;
  ruta: string;
  icon: string;
  dot?: () => boolean;
}

@Component({
  selector: 'app-portal-casa',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './portal-casa.component.html',
  styleUrl: './portal-casa.component.scss',
})
export class PortalCasaComponent {
  portal = inject(PortalService);
  cargando = signal(true);

  tiles: Tile[] = [
    { label: 'QR', ruta: '/portal/qr', icon: '▦' },
    { label: 'Finanzas', ruta: '/portal/finanzas', icon: '💳' },
    { label: 'Amenidades', ruta: '/portal/amenidades', icon: '◈' },
    { label: 'Calendario', ruta: '/portal/calendario', icon: '📅' },
    { label: 'Documentos', ruta: '/portal/documentos', icon: '📄' },
    { label: 'Contactos', ruta: '/portal/contactos', icon: '👥' },
    { label: 'Encuestas', ruta: '/portal/encuestas', icon: '📊', dot: () => (this.portal.casa()?.encuestasPendientes.length ?? 0) > 0 },
    { label: 'Incidencias', ruta: '/portal/incidencias', icon: '⚠' },
    { label: 'Ver todas', ruta: '/portal/menu', icon: '⋯' },
  ];

  constructor() {
    this.portal.cargarPanel().subscribe();
    this.portal.cargarCasa().subscribe({
      next: () => this.cargando.set(false),
      error: () => this.cargando.set(false),
    });
  }

  inicial(nombre: string | undefined): string {
    return (nombre ?? '?').trim().charAt(0).toUpperCase();
  }

  diaRelativo(iso: string): string {
    const d = new Date(iso);
    const hoy = new Date();
    const dif = Math.round((d.setHours(0, 0, 0, 0) - hoy.setHours(0, 0, 0, 0)) / 86_400_000);
    if (dif === 0) return 'Hoy';
    if (dif === 1) return 'Mañana';
    if (dif > 1 && dif < 7) return new Date(iso).toLocaleDateString('es-AR', { weekday: 'long' });
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short' });
  }
  estadoLabel(e: string): string {
    return e === 'Confirmada' ? 'Confirmada' : e === 'Pendiente' ? 'Pendiente' : e;
  }
  estadoColor(e: string): string {
    return e === 'Confirmada' ? '#16a34a' : e === 'Pendiente' ? '#d97706' : '#64748b';
  }
}
