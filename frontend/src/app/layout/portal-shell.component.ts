import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../core/services/auth.service';
import { PortalService } from '../core/services/portal.service';

interface PortalNav {
  label: string;
  ruta: string;
  icon: keyof typeof ICONOS;
  badge?: () => number | undefined;
}

const ICONOS = {
  home: '<path d="M4 11l8-7 8 7M6 10v9h12v-9"/>',
  megaphone: '<path d="M3 11l14-6v14L3 13z"/><path d="M3 11v2M7 12v5a2 2 0 0 0 4 0v-3"/>',
  poll: '<path d="M5 21V10M12 21V4M19 21v-7"/>',
  calendar: '<rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 9h18M8 2v4M16 2v4"/>',
  wrench: '<path d="M14 7a4 4 0 0 1-5 5l-5 5 2 2 5-5a4 4 0 0 0 5-5z"/>',
  folder: '<path d="M3 6h6l2 2h10v11H3z"/>',
  bell: '<path d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6"/><path d="M10 20a2 2 0 0 0 4 0"/>',
  user: '<circle cx="12" cy="8" r="4"/><path d="M4 20a8 8 0 0 1 16 0"/>',
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

  readonly nav: PortalNav[] = [
    { label: 'Inicio', ruta: '/portal/inicio', icon: 'home' },
    { label: 'Comunicados', ruta: '/portal/comunicados', icon: 'megaphone' },
    { label: 'Encuestas', ruta: '/portal/encuestas', icon: 'poll' },
    { label: 'Reservas', ruta: '/portal/reservas', icon: 'calendar' },
    { label: 'Reclamos', ruta: '/portal/reclamos', icon: 'wrench' },
    { label: 'Documentos', ruta: '/portal/documentos', icon: 'folder' },
    { label: 'Notificaciones', ruta: '/portal/notificaciones', icon: 'bell', badge: () => this.portal.notifNoLeidas() || undefined },
    { label: 'Mi unidad', ruta: '/portal/mi-unidad', icon: 'user' },
  ];

  /** Ítems para la barra inferior en mobile (máximo 5). */
  readonly navMobile = computed<PortalNav[]>(() => [
    this.nav[0], this.nav[1], this.nav[2], this.nav[4], this.nav[6],
  ]);

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
