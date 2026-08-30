import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type RolUnidad = 'Propietario' | 'Inquilino' | 'Gestor';

export interface MiUnidad {
  unidadId: string;
  unidadNombre: string;
  consorcioId: string;
  consorcioNombre: string;
  rol: RolUnidad;
  esContactoPrincipal: boolean;
  cuotaMantenimiento?: number | null;
  saldo: number;
}

export interface MiPanel {
  nombre: string;
  unidades: MiUnidad[];
}

export interface EncuestaPendiente {
  id: string;
  titulo: string;
  diasRestantes?: number | null;
}

export interface ReservaResumen {
  id: string;
  amenidad: string;
  inicio: string;
  fin: string;
  estado: string;
}

export interface PortalCasa {
  consorcioId: string;
  consorcioNombre: string;
  localidad?: string | null;
  unidadNombre: string;
  encuestasPendientes: EncuestaPendiente[];
  reservasProximas: ReservaResumen[];
  paquetesPendientes: number;
  notificacionesNoLeidas: number;
}

@Injectable({ providedIn: 'root' })
export class PortalService {
  private http = inject(HttpClient);

  readonly panel = signal<MiPanel | null>(null);
  readonly unidadActivaId = signal<string | null>(null);
  readonly casa = signal<PortalCasa | null>(null);
  readonly notifNoLeidas = signal(0);

  readonly unidadActiva = computed<MiUnidad | null>(() => {
    const p = this.panel();
    if (!p) return null;
    return p.unidades.find((u) => u.unidadId === this.unidadActivaId()) ?? p.unidades[0] ?? null;
  });

  cargarPanel(): Observable<MiPanel> {
    const obs = this.http.get<MiPanel>(`${environment.apiUrl}/mi-unidad`);
    obs.subscribe({
      next: (p) => {
        this.panel.set(p);
        if (!this.unidadActivaId() && p.unidades.length) this.unidadActivaId.set(p.unidades[0].unidadId);
      },
      error: () => this.panel.set({ nombre: '', unidades: [] }),
    });
    return obs;
  }

  cargarCasa(): Observable<PortalCasa> {
    const obs = this.http.get<PortalCasa>(`${environment.apiUrl}/mi-portal/casa`);
    obs.subscribe({
      next: (c) => { this.casa.set(c); this.notifNoLeidas.set(c.notificacionesNoLeidas); },
      error: () => this.casa.set(null),
    });
    return obs;
  }

  setUnidad(id: string): void {
    this.unidadActivaId.set(id);
  }
}
