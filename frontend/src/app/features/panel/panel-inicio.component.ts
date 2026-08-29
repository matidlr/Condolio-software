import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgTemplateOutlet } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { PanelResumen, PanelService } from '../../core/services/panel.service';

interface Funcion {
  titulo: string;
  descripcion: string;
  icon: string;
  color: string;
  ruta?: string;
  statLabel: string;
  stat: () => string;
  alerta?: () => boolean;
}

@Component({
  selector: 'app-panel-inicio',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './panel-inicio.component.html',
  styleUrl: './panel-inicio.component.scss',
})
export class PanelInicioComponent {
  consorcios = inject(ConsorcioService);
  private api = inject(PanelService);
  private sanitizer = inject(DomSanitizer);
  private iconoCache = new Map<string, SafeHtml>();

  icono(nombre: string): SafeHtml {
    if (!this.iconoCache.has(nombre)) {
      const svg = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"
        stroke-linecap="round" stroke-linejoin="round">${this.ICONOS[nombre] ?? ''}</svg>`;
      this.iconoCache.set(nombre, this.sanitizer.bypassSecurityTrustHtml(svg));
    }
    return this.iconoCache.get(nombre)!;
  }

  cargando = signal(true);
  r = signal<PanelResumen | null>(null);

  iniciales = computed(() => (this.consorcios.activo?.nombre ?? '?').trim().charAt(0).toUpperCase());

  constructor() {
    effect(() => {
      const id = this.consorcios.activoId();
      if (!id) { this.cargando.set(false); return; }
      this.cargando.set(true);
      this.api.resumen(id).subscribe({
        next: (x) => { this.r.set(x); this.cargando.set(false); },
        error: () => this.cargando.set(false),
      });
    });
  }

  private n(sel: (r: PanelResumen) => number): string {
    const r = this.r();
    return r ? String(sel(r)) : '—';
  }
  private money(sel: (r: PanelResumen) => number): string {
    const r = this.r();
    return r ? '$' + sel(r).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '—';
  }

  funciones: Funcion[] = [
    {
      titulo: 'Residentes', icon: 'users', color: '#3b82f6', ruta: '/panel/residentes/directorio',
      descripcion: 'Gestionar perfiles de residentes, unidades y acceso',
      statLabel: 'Residentes activos', stat: () => this.n((r) => r.residentesActivos),
    },
    {
      titulo: 'Paquetes', icon: 'box', color: '#f59e0b',
      descripcion: 'Rastrear entregas, notificaciones y recogidas',
      statLabel: 'Pendientes de recogida', stat: () => this.n((r) => r.paquetesPendientes),
    },
    {
      titulo: 'Pagos', icon: 'card', color: '#10b981',
      descripcion: 'Monitorear transacciones, tarifas y facturación',
      statLabel: 'Este mes', stat: () => this.money((r) => r.pagosMes),
    },
    {
      titulo: 'Publicaciones', icon: 'megaphone', color: '#a855f7', ruta: '/panel/anuncios',
      descripcion: 'Difundir mensajes y avisos importantes',
      statLabel: 'Este mes', stat: () => this.n((r) => r.publicacionesMes),
    },
    {
      titulo: 'Tickets', icon: 'alert', color: '#ef4444', ruta: '/panel/tickets/lista',
      descripcion: 'Gestionar y rastrear solicitudes de mantenimiento e incidencias',
      statLabel: 'Tickets abiertos', stat: () => this.n((r) => r.ticketsAbiertos),
      alerta: () => (this.r()?.ticketsAbiertos ?? 0) > 0,
    },
    {
      titulo: 'Reservaciones', icon: 'calendar', color: '#ec4899', ruta: '/panel/amenidades/reservaciones',
      descripcion: 'Gestionar reservaciones de instalaciones y horarios',
      statLabel: 'Esta semana', stat: () => this.n((r) => r.reservasSemana),
    },
    {
      titulo: 'Control de Acceso', icon: 'shield', color: '#f43f5e',
      descripcion: 'Gestión de seguridad y permisos de entrada',
      statLabel: 'Entradas hoy', stat: () => this.n((r) => r.entradasHoy),
    },
    {
      titulo: 'Códigos QR', icon: 'qr', color: '#6366f1',
      descripcion: 'Generar y gestionar códigos QR de acceso',
      statLabel: 'Códigos activos', stat: () => this.n((r) => r.codigosQrActivos),
    },
  ];

  readonly ICONOS: Record<string, string> = {
    users: '<path d="M16 14a4 4 0 1 0-8 0M12 7a3 3 0 1 0 0-6 3 3 0 0 0 0 6M20 19a5 5 0 0 0-9-3M4 19a5 5 0 0 1 9-3"/>',
    box: '<path d="M3 8l9-5 9 5v8l-9 5-9-5z"/><path d="M3 8l9 5 9-5M12 13v8"/>',
    card: '<rect x="3" y="6" width="18" height="12" rx="2"/><path d="M3 10h18"/>',
    megaphone: '<path d="M3 11l14-6v14L3 13z"/><path d="M3 11v2M7 12v5a2 2 0 0 0 4 0v-3"/>',
    alert: '<path d="M12 3l9 16H3z"/><path d="M12 10v4M12 17h.01"/>',
    calendar: '<rect x="3" y="4" width="18" height="17" rx="2"/><path d="M3 9h18M8 2v4M16 2v4"/>',
    shield: '<path d="M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z"/>',
    qr: '<path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h3v3h-3zM19 19h1M17 19h.01M19 17h.01"/>',
  };
}
