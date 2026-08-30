import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { PaseAccesoService, Visita } from '../../core/services/pase-acceso.service';
import { LABEL_VISITA, ICON_VISITA } from '../../core/models/pase-acceso.models';
import { ToastService } from '../../core/services/toast.service';

type Rango = 'todo' | 'hoy' | 'semana' | 'mes';

@Component({
  selector: 'app-portal-visitas',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './portal-visitas.component.html',
  styleUrl: './portal-visitas.component.scss',
})
export class PortalVisitasComponent {
  private api = inject(PaseAccesoService);
  private toasts = inject(ToastService);

  labelVisita = LABEL_VISITA;
  iconVisita = ICON_VISITA;

  cargando = signal(true);
  visitas = signal<Visita[]>([]);
  busqueda = signal('');
  rango = signal<Rango>('todo');

  constructor() {
    this.api.visitas().subscribe({
      next: (l) => { this.visitas.set(l); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar el historial.'); this.cargando.set(false); },
    });
  }

  filtradas = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const r = this.rango();
    const ahora = Date.now();
    const limite = r === 'hoy' ? 86_400_000 : r === 'semana' ? 7 * 86_400_000 : r === 'mes' ? 30 * 86_400_000 : Infinity;
    return this.visitas().filter((v) => {
      if (ahora - new Date(v.ingresoUtc).getTime() > limite) return false;
      if (!q) return true;
      return v.visitanteNombre.toLowerCase().includes(q)
        || this.labelVisita[v.tipoVisita].toLowerCase().includes(q)
        || (v.patente ?? '').toLowerCase().includes(q);
    });
  });

  grupos = computed(() => {
    const map = new Map<string, Visita[]>();
    for (const v of this.filtradas()) {
      const k = v.ingresoUtc.slice(0, 10);
      (map.get(k) ?? map.set(k, []).get(k)!).push(v);
    }
    return [...map.entries()].sort(([a], [b]) => b.localeCompare(a)).map(([fecha, items]) => ({ fecha, items }));
  });
}
