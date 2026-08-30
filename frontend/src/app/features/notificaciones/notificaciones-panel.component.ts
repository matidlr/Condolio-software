import { Component, computed, inject, output, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { NotificacionService } from '../../core/services/notificacion.service';
import { ToastService } from '../../core/services/toast.service';
import { META_NOTIF, Notificacion, TipoNotificacion } from '../../core/models/notificacion.models';

@Component({
  selector: 'app-notificaciones-panel',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './notificaciones-panel.component.html',
  styleUrl: './notificaciones-panel.component.scss',
})
export class NotificacionesPanelComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(NotificacionService);
  private toasts = inject(ToastService);
  private router = inject(Router);

  cerrar = output<void>();

  meta = META_NOTIF;

  cargando = signal(true);
  items = signal<Notificacion[]>([]);
  total = signal(0);
  noLeidas = signal(0);
  busqueda = signal('');
  filtroTipo = signal<TipoNotificacion | 'todas'>('todas');

  tiposConteo = computed(() => {
    const m = new Map<TipoNotificacion, number>();
    for (const n of this.items()) m.set(n.tipo, (m.get(n.tipo) ?? 0) + 1);
    return [...m.entries()].map(([tipo, n]) => ({ tipo, n }));
  });

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const t = this.filtroTipo();
    return this.items().filter((n) =>
      (t === 'todas' || n.tipo === t) &&
      (!q || n.titulo.toLowerCase().includes(q) || n.cuerpo.toLowerCase().includes(q)));
  });

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    const cid = this.consorcios.activoId();
    if (!cid) { this.cargando.set(false); return; }
    this.cargando.set(true);
    this.api.listar(cid).subscribe({
      next: (l) => {
        this.items.set(l.notificaciones);
        this.total.set(l.total);
        this.noLeidas.set(l.noLeidas);
        this.api.resumen.set({ total: l.total, noLeidas: l.noLeidas });
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar las notificaciones.'); this.cargando.set(false); },
    });
  }

  hace(iso: string): string {
    const s = Math.max(1, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
    if (s < 60) return 'recién';
    const m = Math.floor(s / 60);
    if (m < 60) return `hace ${m} min`;
    const h = Math.floor(m / 60);
    if (h < 24) return `hace ${h} h`;
    const d = Math.floor(h / 24);
    if (d < 30) return `hace ${d} día${d === 1 ? '' : 's'}`;
    return `hace ${Math.floor(d / 30)} mes${Math.floor(d / 30) === 1 ? '' : 'es'}`;
  }

  abrir(n: Notificacion): void {
    const cid = this.consorcios.activoId();
    if (cid && !n.leida) {
      this.api.marcarLeida(cid, n.id).subscribe(() => this.api.refrescarResumen(cid));
      this.items.update((l) => l.map((x) => x.id === n.id ? { ...x, leida: true } : x));
      this.noLeidas.update((v) => Math.max(0, v - 1));
    }
    if (n.enlace) {
      this.cerrar.emit();
      this.router.navigateByUrl(n.enlace);
    }
  }

  marcarTodas(): void {
    const cid = this.consorcios.activoId();
    if (!cid) return;
    this.api.marcarTodasLeidas(cid).subscribe(() => {
      this.items.update((l) => l.map((x) => ({ ...x, leida: true })));
      this.noLeidas.set(0);
      this.api.refrescarResumen(cid);
      this.toasts.exito('Todas marcadas como leídas');
    });
  }
}
