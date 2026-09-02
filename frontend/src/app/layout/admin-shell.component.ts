import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { AuthService } from '../core/services/auth.service';
import { ConsorcioService } from '../core/services/consorcio.service';
import { ResidenteService } from '../core/services/residente.service';
import { TicketService } from '../core/services/ticket.service';
import { EncuestaService } from '../core/services/encuesta.service';
import { NotificacionService } from '../core/services/notificacion.service';
import { NotificacionesPanelComponent } from '../features/notificaciones/notificaciones-panel.component';
import { AreaAdmin } from '../core/models/auth.models';

interface NavHijo {
  label: string;
  ruta: string;
  disponible: boolean;
  badge?: number;
}

interface NavItem {
  label: string;
  ruta?: string;
  icon: keyof typeof ICONOS;
  disponible: boolean;
  badge?: number;
  hijos?: NavHijo[];
  area?: AreaAdmin;
}

const ICONOS = {
  grid: '<path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z"/>',
  building: '<rect x="5" y="3" width="14" height="18" rx="1.5"/><path d="M9 7h2M13 7h2M9 11h2M13 11h2M9 15h2M13 15h2"/>',
  users: '<circle cx="9" cy="8" r="3"/><path d="M3.5 19c0-3 2.5-5 5.5-5s5.5 2 5.5 5M16 6a3 3 0 0 1 0 6"/>',
  receipt: '<path d="M6 3h12v18l-3-2-3 2-3-2-3 2zM9 8h6M9 12h6"/>',
  wallet: '<rect x="3" y="6" width="18" height="13" rx="2"/><path d="M3 10h18M16 14h2"/>',
  chat: '<path d="M4 5h16v11H9l-5 4z"/>',
  folder: '<path d="M3 6h6l2 2h10v11H3z"/>',
  ticket: '<path d="M4 7a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v3a2 2 0 0 0 0 4v3a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-3a2 2 0 0 0 0-4z"/><path d="M14 5v14" stroke-dasharray="2 2"/>',
  amenity: '<path d="M3 15c1.5 0 1.5 1 3 1s1.5-1 3-1 1.5 1 3 1 1.5-1 3-1 1.5 1 3 1 1.5-1 3-1"/><path d="M3 19c1.5 0 1.5 1 3 1s1.5-1 3-1 1.5 1 3 1 1.5-1 3-1 1.5 1 3 1 1.5-1 3-1"/><path d="M7 12V6a2 2 0 0 1 4 0M7 9h4"/>',
  megaphone: '<path d="M3 11l14-6v14L3 13z"/><path d="M3 11v2M7 12v5a2 2 0 0 0 4 0v-3"/>',
  calendar: '<rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 9h18M8 2v4M16 2v4"/>',
  poll: '<path d="M5 21V10M12 21V4M19 21v-7"/>',
  bell: '<path d="M6 9a6 6 0 0 1 12 0c0 5 2 6 2 6H4s2-1 2-6"/><path d="M10 20a2 2 0 0 0 4 0"/>',
  shield: '<path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z"/>',
  package: '<path d="M12 3l8 4.5v9L12 21l-8-4.5v-9zM4 7.5l8 4.5 8-4.5M12 12v9"/>',
  gear: '<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.6 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.6a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9c.14.31.22.65.22 1"/>',
} as const;

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NotificacionesPanelComponent],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
  host: { '(document:click)': 'switcherAbierto.set(false); menuAbierto.set(false)' },
})
export class AdminShellComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);
  consorcios = inject(ConsorcioService);
  private residentes = inject(ResidenteService);
  private ticketsApi = inject(TicketService);
  private encuestasApi = inject(EncuestaService);
  notificacionesApi = inject(NotificacionService);

  colapsado = signal(false);
  menuAbierto = signal(false);
  panelNotif = signal(false);
  switcherAbierto = signal(false);

  consorcioActivo = computed(() =>
    this.consorcios.consorcios().find((c) => c.id === this.consorcios.activoId()) ?? null);

  iniConsorcio(nombre: string | undefined | null): string {
    return (nombre ?? 'S').trim().charAt(0).toUpperCase() || 'S';
  }

  elegirConsorcio(id: string): void {
    this.switcherAbierto.set(false);
    if (id === this.consorcios.activoId()) return;
    this.cambiarConsorcio(id);
  }

  crearSociedad(): void {
    this.switcherAbierto.set(false);
    this.router.navigate(['/nueva-sociedad']);
  }

  nombre = this.auth.nombre;
  iniciales = computed(() =>
    this.nombre().split(' ').map((p) => p[0] ?? '').slice(0, 2).join('').toUpperCase() || 'AD',
  );

  readonly nav = computed<NavItem[]>(() => {
    const pend = this.residentes.pendientes();
    const tks = this.ticketsApi.activos();
    const enc = this.encuestasApi.activas();
    const items: NavItem[] = [
      { label: 'Panel', ruta: '/panel/inicio', icon: 'grid', disponible: true },
      { label: 'Unidades', ruta: '/panel/unidades', icon: 'building', disponible: true, area: 'Operacion' },
      {
        label: 'Residentes', icon: 'users', disponible: true, area: 'Residentes',
        hijos: [
          { label: 'Directorio', ruta: '/panel/residentes/directorio', disponible: true },
          { label: 'Por asignar', ruta: '/panel/residentes/por-asignar', disponible: true },
          { label: 'Invitaciones', ruta: '/panel/residentes/invitaciones', disponible: true, badge: pend || undefined },
        ],
      },
      {
        label: 'Tickets', icon: 'ticket', disponible: true, area: 'Operacion',
        hijos: [
          { label: 'Lista', ruta: '/panel/tickets/lista', disponible: true, badge: tks || undefined },
          { label: 'Panel', ruta: '/panel/tickets/panel', disponible: true },
          { label: 'Métricas', ruta: '/panel/tickets/metricas', disponible: true },
        ],
      },
      {
        label: 'Amenidades', icon: 'amenity', disponible: true, area: 'Operacion',
        hijos: [
          { label: 'Directorio', ruta: '/panel/amenidades/directorio', disponible: true },
          { label: 'Reservaciones', ruta: '/panel/amenidades/reservaciones', disponible: true },
          { label: 'Estadísticas', ruta: '/panel/amenidades/estadisticas', disponible: true },
        ],
      },
      {
        label: 'Seguridad', icon: 'shield', disponible: true, area: 'Seguridad',
        hijos: [
          { label: 'Bitácora de accesos', ruta: '/panel/seguridad/bitacora', disponible: true },
          { label: 'Códigos QR', ruta: '/panel/seguridad/qr', disponible: true },
          { label: 'Staff', ruta: '/panel/seguridad/staff', disponible: true },
          { label: 'Credenciales de caseta', ruta: '/panel/seguridad/credenciales', disponible: true },
        ],
      },
      { label: 'Paquetería', ruta: '/panel/paqueteria', icon: 'package', disponible: true, area: 'Operacion' },
      { label: 'Anuncios', ruta: '/panel/anuncios', icon: 'megaphone', disponible: true, area: 'Comunicacion' },
      { label: 'Calendario', ruta: '/panel/calendario', icon: 'calendar', disponible: true, area: 'Comunicacion' },
      { label: 'Encuestas', ruta: '/panel/encuestas', icon: 'poll', disponible: true, badge: enc || undefined, area: 'Comunicacion' },
      {
        label: 'Expensas', icon: 'receipt', disponible: true, area: 'Finanzas',
        hijos: [
          { label: 'Proveedores', ruta: '/panel/expensas/proveedores', disponible: true },
          { label: 'Gastos fijos', ruta: '/panel/expensas/gastos-fijos', disponible: true },
          { label: 'Extraordinarias', ruta: '/panel/expensas/extraordinarias', disponible: true },
          { label: 'Configuración', ruta: '/panel/expensas/configuracion', disponible: true },
        ],
      },
      { label: 'Reclamos', ruta: '/panel/reclamos', icon: 'chat', disponible: false, area: 'Operacion' },
      { label: 'Documentos', ruta: '/panel/documentos', icon: 'folder', disponible: true, area: 'Comunicacion' },
    ];
    return items.filter((it) => !it.area || this.auth.puedeArea(it.area));
  });

  readonly esGeneral = this.auth.adminGeneral;

  gruposAbiertos = signal<Set<string>>(new Set());

  toggleGrupo(label: string): void {
    const s = new Set(this.gruposAbiertos());
    s.has(label) ? s.delete(label) : s.add(label);
    this.gruposAbiertos.set(s);
  }

  private iconoCache = new Map<string, SafeHtml>();

  icono(nombre: keyof typeof ICONOS): SafeHtml {
    if (!this.iconoCache.has(nombre)) {
      const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"
        stroke-linecap="round" stroke-linejoin="round">${ICONOS[nombre]}</svg>`;
      this.iconoCache.set(nombre, this.sanitizer.bypassSecurityTrustHtml(svg));
    }
    return this.iconoCache.get(nombre)!;
  }

  constructor() {
    effect(() => {
      const id = this.consorcios.activoId();
      if (id) {
        this.residentes.refrescarPendientes(id);
        this.ticketsApi.refrescarActivos(id);
        this.encuestasApi.refrescarActivas(id);
        this.notificacionesApi.refrescarResumen(id);
      }
    });
  }

  ngOnInit(): void {
    this.consorcios.cargar().subscribe();
  }

  cambiarConsorcio(id: string): void {
    this.consorcios.setActivo(id);
    const url = this.router.url;
    this.router
      .navigateByUrl('/panel', { skipLocationChange: true })
      .then(() => this.router.navigateByUrl(url));
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
