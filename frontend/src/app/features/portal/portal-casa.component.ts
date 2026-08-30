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
}
