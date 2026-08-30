import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { PortalService } from '../../core/services/portal.service';

const ICONOS: Record<string, string> = {
  amenity: '<path d="M12 3l3 6 6 .9-4.5 4.2 1 6L12 17l-6.5 3 1-6L2 9.9 8 9z"/>',
  calendar: '<rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 9h18M8 2v4M16 2v4"/>',
  people: '<circle cx="9" cy="8" r="3.2"/><path d="M2 20a7 7 0 0 1 14 0M17 11a3 3 0 0 0 0-6M22 20a6 6 0 0 0-4-5.6"/>',
  doc: '<path d="M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z"/><path d="M14 3v5h5"/>',
  wallet: '<rect x="3" y="6" width="18" height="13" rx="2"/><path d="M16 12h3M3 10h18"/>',
  qr: '<path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h3v3h-3zM19 19h1M17 19h.01M19 17h.01"/>',
  history: '<path d="M3 12a9 9 0 1 0 3-6.7M3 4v4h4"/><path d="M12 8v4l3 2"/>',
  poll: '<path d="M5 21V10M12 21V4M19 21v-7"/>',
  info: '<circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/>',
  alert: '<path d="M12 3l9 16H3z"/><path d="M12 10v4M12 17h.01"/>',
  box: '<path d="M3 8l9-5 9 5v8l-9 5-9-5z"/><path d="M3 8l9 5 9-5M12 13v8"/>',
  unit: '<path d="M4 11l8-7 8 7M6 10v9h12v-9M10 19v-5h4v5"/>',
};

@Component({
  selector: 'app-portal-menu',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="pm">
      <div class="pm-header">
        <span class="pm-header__ph">{{ (portal.casa()?.consorcioNombre ?? '?').charAt(0).toUpperCase() }}</span>
        <div>
          <h1>{{ portal.casa()?.consorcioNombre ?? portal.unidadActiva()?.consorcioNombre }}</h1>
          <p>{{ portal.casa()?.localidad ?? 'Todas las funciones' }}</p>
        </div>
      </div>

      <div class="pm-list">
        @for (s of items; track s.ruta) {
          <a class="pm-row" [routerLink]="s.ruta">
            <span class="pm-row__ic" [innerHTML]="icono(s.icon)"></span>
            <b>{{ s.label }}</b>
            <span class="pm-row__go">›</span>
          </a>
        }
      </div>
    </section>
  `,
  styles: [`
    :host { display: block; }
    .pm-header {
      display: flex; align-items: center; gap: 14px;
      background: var(--c-surface); border: 1px solid var(--c-border); border-radius: 16px; padding: 16px 18px; margin-bottom: 16px;
    }
    .pm-header__ph {
      width: 52px; height: 52px; border-radius: 13px; flex-shrink: 0;
      background: linear-gradient(140deg, #3a5568, #17263a); color: #fff;
      display: grid; place-items: center; font-size: 1.3rem; font-weight: 700;
    }
    .pm-header h1 { margin: 0; font-size: 1.25rem; }
    .pm-header p { margin: 3px 0 0; color: var(--c-text-muted); font-size: 0.85rem; }
    .pm-list { background: var(--c-surface); border: 1px solid var(--c-border); border-radius: 16px; overflow: hidden; }
    .pm-row {
      display: flex; align-items: center; gap: 14px; padding: 14px 16px;
      text-decoration: none; color: var(--c-text); border-bottom: 1px solid var(--c-border);
    }
    .pm-row:last-child { border-bottom: none; }
    .pm-row:hover { background: var(--c-bg); }
    .pm-row__ic { display: grid; place-items: center; width: 22px; height: 22px; color: var(--c-text-muted); }
    .pm-row__ic svg { width: 22px; height: 22px; }
    .pm-row b { flex: 1; font-weight: 500; font-size: 0.95rem; }
    .pm-row__go { color: var(--c-text-soft); }
  `],
})
export class PortalMenuComponent {
  portal = inject(PortalService);
  private sanitizer = inject(DomSanitizer);

  items = [
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

  icono(n: string): SafeHtml {
    const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"
      stroke-linecap="round" stroke-linejoin="round">${ICONOS[n] ?? ''}</svg>`;
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }
}
