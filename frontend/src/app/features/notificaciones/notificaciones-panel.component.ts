import { Component, computed, inject, output, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
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
  consorcios = inject(ConsorcioService);
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
  filtroConsorcio = signal<string>(this.consorcios.activoId() ?? 'todas');

  tiposConteo = computed(() => {
    const m = new Map<TipoNotificacion, number>();
    for (const n of this.items()) m.set(n.tipo, (m.get(n.tipo) ?? 0) + 1);
    return [...m.entries()].map(([tipo, n]) => ({ tipo, n }));
  });

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const t = this.filtroTipo();
    return this.items()
      .filter((n) =>
        (t === 'todas' || n.tipo === t) &&
        (!q || n.titulo.toLowerCase().includes(q) || n.cuerpo.toLowerCase().includes(q)))
      .slice()
      .sort((a, b) => (b.fijada ? 1 : 0) - (a.fijada ? 1 : 0));
  });

  constructor() {
    this.cargar();
  }

  cambiarConsorcio(v: string): void {
    this.filtroConsorcio.set(v);
    this.cargar();
  }

  private cargar(): void {
    const filtro = this.filtroConsorcio();
    const todas = this.consorcios.consorcios();
    const objetivo = filtro === 'todas' ? todas : todas.filter((c) => c.id === filtro);
    if (objetivo.length === 0) { this.cargando.set(false); return; }

    this.cargando.set(true);
    forkJoin(objetivo.map((c) =>
      this.api.listar(c.id).pipe(
        catchError(() => of({ notificaciones: [] as Notificacion[], total: 0, noLeidas: 0 })),
      ),
    )).subscribe((listas) => {
      const merged = listas.flatMap((l, i) =>
        l.notificaciones.map((n) => ({ ...n, consorcioId: objetivo[i].id, consorcioNombre: objetivo[i].nombre })));
      merged.sort((a, b) => b.creadoUtc.localeCompare(a.creadoUtc));
      this.items.set(merged);
      this.total.set(listas.reduce((s, l) => s + l.total, 0));
      this.noLeidas.set(listas.reduce((s, l) => s + l.noLeidas, 0));
      this.api.resumen.set({ total: this.total(), noLeidas: this.noLeidas() });
      this.cargando.set(false);
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
    const cid = n.consorcioId ?? this.consorcios.activoId();
    if (cid && !n.leida) {
      this.api.marcarLeida(cid, n.id).subscribe(() => this.api.refrescarResumen(this.consorcios.activoId() ?? cid));
      this.items.update((l) => l.map((x) => x.id === n.id ? { ...x, leida: true } : x));
      this.noLeidas.update((v) => Math.max(0, v - 1));
    }
    if (n.enlace) {
      if (cid && cid !== this.consorcios.activoId()) this.consorcios.setActivo(cid);
      this.cerrar.emit();
      this.router.navigateByUrl(n.enlace);
    }
  }

  alternarLeida(n: Notificacion, ev: Event): void {
    ev.stopPropagation();
    const cid = n.consorcioId ?? this.consorcios.activoId();
    if (!cid) return;
    this.api.alternarLeida(cid, n.id).subscribe((leida) => {
      this.items.update((l) => l.map((x) => x.id === n.id ? { ...x, leida } : x));
      this.noLeidas.update((v) => Math.max(0, v + (leida ? -1 : 1)));
      this.api.refrescarResumen(this.consorcios.activoId() ?? cid);
    });
  }

  alternarFijada(n: Notificacion, ev: Event): void {
    ev.stopPropagation();
    const cid = n.consorcioId ?? this.consorcios.activoId();
    if (!cid) return;
    this.api.alternarFijada(cid, n.id).subscribe((fijada) => {
      this.items.update((l) => l.map((x) => x.id === n.id ? { ...x, fijada } : x));
    });
  }

  marcarTodas(): void {
    const filtro = this.filtroConsorcio();
    const todas = this.consorcios.consorcios();
    const objetivo = filtro === 'todas' ? todas : todas.filter((c) => c.id === filtro);
    if (objetivo.length === 0) return;
    forkJoin(objetivo.map((c) => this.api.marcarTodasLeidas(c.id).pipe(catchError(() => of(undefined))))).subscribe(() => {
      this.items.update((l) => l.map((x) => ({ ...x, leida: true })));
      this.noLeidas.set(0);
      this.api.refrescarResumen(this.consorcios.activoId() ?? objetivo[0].id);
      this.toasts.exito('Todas marcadas como leídas');
    });
  }
}
