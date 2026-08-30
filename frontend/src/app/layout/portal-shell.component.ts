import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../core/services/auth.service';
import { PortalService } from '../core/services/portal.service';

interface NavItem {
  label: string;
  ruta: string;
  icon: keyof typeof ICONOS;
}

const ICONOS = {
  wall: '<path d="M4 5h16v6H4zM4 13h7v6H4zM13 13h7v6h-7z"/>',
  home: '<path d="M4 11l8-7 8 7M6 10v9h12v-9"/>',
  bell: '<path d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6"/><path d="M10 20a2 2 0 0 0 4 0"/>',
  gear: '<circle cx="12" cy="12" r="3.2"/><path d="M12 2v3M12 19v3M4.2 4.2l2.1 2.1M17.7 17.7l2.1 2.1M2 12h3M19 12h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1"/>',
  qr: '<path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h3v3h-3zM19 19h1M17 19h.01M19 17h.01"/>',
  wallet: '<rect x="3" y="6" width="18" height="13" rx="2"/><path d="M16 12h3M3 10h18"/>',
  amenity: '<path d="M12 3l3 6 6 .9-4.5 4.2 1 6L12 17l-6.5 3 1-6L2 9.9 8 9z"/>',
  calendar: '<rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 9h18M8 2v4M16 2v4"/>',
  doc: '<path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/>',
  people: '<circle cx="9" cy="8" r="3.2"/><path d="M2 20a7 7 0 0 1 14 0M17 11a3 3 0 0 0 0-6M22 20a6 6 0 0 0-4-5.6"/>',
  poll: '<path d="M5 21V10M12 21V4M19 21v-7"/>',
  info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
  alert: '<path d="M12 3l9 16H3z"/><path d="M12 10v4M12 17h.01"/>',
  history: '<path d="M3 12a9 9 0 1 0 3-6.7M3 4v4h4"/><path d="M12 8v4l3 2"/>',
  box: '<path d="M3 8l9-5 9 5v8l-9 5-9-5z"/><path d="M3 8l9 5 9-5M12 13v8"/>',
  unit: '<path d="M4 11l8-7 8 7M6 10v9h12v-9M10 19v-5h4v5"/>',
} as const;

@Component({
  selector: 'app-portal-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './portal-shell.component.html',
  styleUrl: './portal-shell.component.scss',
})
export class PortalShellComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);
  portal = inject(PortalService);

  nombre = this.auth.nombre;
  menuAbierto = signal(false);

  iniciales = computed(() =>
    this.nombre().split(' ').map((p) => p[0] ?? '').slice(0, 2).join('').toUpperCase() || 'US',
  );

  readonly tabs: NavItem[] = [
    { label: 'Muro', ruta: '/portal/muro', icon: 'wall' },
    { label: 'Casa', ruta: '/portal/casa', icon: 'home' },
    { label: 'Notificaciones', ruta: '/portal/notificaciones', icon: 'bell' },
    { label: 'Configuración', ruta: '/portal/config', icon: 'gear' },
  ];

  readonly secciones: NavItem[] = [
    { label: 'Amenidades / Reservas', ruta: '/portal/amenidades', icon: 'amenity' },
    { label: 'Calendario', ruta: '/portal/calendario', icon: 'calendar' },
    { label: 'Contactos', ruta: '/portal/contactos', icon: 'people' },
    { label: 'Documentos', ruta: '/portal/documentos', icon: 'doc' },
    { label: 'Finanzas', ruta: '/portal/finanzas', icon: 'wallet' },
    { label: 'QR', ruta: '/portal/qr', icon: 'qr' },
    { label: 'Historial de visitas', ruta: '/portal/historial-visitas', icon: 'history' },
    { label: 'Encuestas', ruta: '/portal/encuestas', icon: 'poll' },
    { label: 'Información', ruta: '/portal/informacion', icon: 'info' },
    { label: 'Incidencias', ruta: '/portal/incidencias', icon: 'alert' },
    { label: 'Mis paquetes', ruta: '/portal/paquetes', icon: 'box' },
    { label: 'Mi unidad', ruta: '/portal/mi-unidad', icon: 'unit' },
  ];

  notifBadge = computed(() => this.portal.notifNoLeidas());

  private iconoCache = new Map<string, SafeHtml>();
  icono(nombre: keyof typeof ICONOS): SafeHtml {
    if (!this.iconoCache.has(nombre)) {
      const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"
        stroke-linecap="round" stroke-linejoin="round">${ICONOS[nombre]}</svg>`;
      this.iconoCache.set(nombre, this.sanitizer.bypassSecurityTrustHtml(svg));
    }
    return this.iconoCache.get(nombre)!;
  }

  salir(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
