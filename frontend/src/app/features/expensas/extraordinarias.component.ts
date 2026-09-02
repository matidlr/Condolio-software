import { Component, computed, effect, inject, signal } from '@angular/core';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CATEGORIAS_EXTRAORDINARIA, EstadoExtraordinaria, ExpensasService, Extraordinaria,
  ExtraordinariasLista,
} from '../../core/services/expensas.service';
import { CuotaExtraordinariaWizardComponent } from './cuota-extraordinaria-wizard.component';

@Component({
  selector: 'app-ex-extraordinarias',
  standalone: true,
  imports: [CuotaExtraordinariaWizardComponent],
  templateUrl: './extraordinarias.component.html',
  styleUrl: './expensas.shared.scss',
})
export class ExtraordinariasComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());

  data = signal<ExtraordinariasLista | null>(null);
  cargando = signal(true);
  wizard = signal(false);

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  fmt(n: number): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { maximumFractionDigits: 2 });
  }
  fmtFecha(iso: string | null | undefined): string {
    if (!iso) return '—';
    const [y, m, d] = iso.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }
  nombreCategoria(v: string): string {
    return CATEGORIAS_EXTRAORDINARIA.find((c) => c.v === v)?.t ?? v;
  }
  estadoTxt(e: EstadoExtraordinaria): string { return e; }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.extraordinarias(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar las cuotas extraordinarias.'); },
    });
  }
  refrescar(): void { const c = this.cid(); if (c) this.cargar(c); }

  onCreada(): void { this.wizard.set(false); this.refrescar(); }

  cambiarEstado(x: Extraordinaria, estado: EstadoExtraordinaria): void {
    const cid = this.cid(); if (!cid) return;
    this.api.estadoExtraordinaria(cid, x.id, estado).subscribe({
      next: () => { this.toasts.exito(`Marcada como ${estado.toLowerCase()}`); this.refrescar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cambiar el estado.'),
    });
  }
}
