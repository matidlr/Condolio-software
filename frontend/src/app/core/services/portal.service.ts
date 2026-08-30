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

@Injectable({ providedIn: 'root' })
export class PortalService {
  private http = inject(HttpClient);

  readonly panel = signal<MiPanel | null>(null);
  readonly unidadActivaId = signal<string | null>(null);
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

  setUnidad(id: string): void {
    this.unidadActivaId.set(id);
  }
}
