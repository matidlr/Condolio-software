import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import { ExpensasService, PeriodoResumen } from '../../core/services/expensas.service';

const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio',
  'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

@Component({
  selector: 'app-ex-periodos',
  standalone: true,
  imports: [],
  templateUrl: './periodos.component.html',
  styleUrl: './expensas.shared.scss',
})
export class PeriodosComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);
  private router = inject(Router);

  private cid = computed(() => this.consorcios.activoId());

  data = signal<PeriodoResumen[]>([]);
  cargando = signal(true);
  abriendo = signal(false);

  meses = MESES.map((t, i) => ({ v: i + 1, t }));
  hoy = new Date();
  nvAnio = signal(this.hoy.getFullYear());
  nvMes = signal(this.hoy.getMonth() + 1);
  anios = [this.hoy.getFullYear() - 1, this.hoy.getFullYear(), this.hoy.getFullYear() + 1];

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  nombreMes(m: number): string { return MESES[m - 1] ?? `${m}`; }
  fmt(n: number): string { return '$' + (n ?? 0).toLocaleString('es-AR', { maximumFractionDigits: 0 }); }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.periodos(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar los períodos.'); },
    });
  }

  yaExiste = computed(() =>
    this.data().some((p) => p.anio === this.nvAnio() && p.mes === this.nvMes()));

  abrir(): void {
    const cid = this.cid();
    if (!cid || this.abriendo() || this.yaExiste()) return;
    this.abriendo.set(true);
    this.api.abrirPeriodo(cid, this.nvAnio(), this.nvMes()).subscribe({
      next: (p) => { this.abriendo.set(false); this.toasts.exito('Período abierto'); this.router.navigate(['/panel/expensas/gastos', p.id]); },
      error: (e) => { this.abriendo.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo abrir el período.'); },
    });
  }

  ir(p: PeriodoResumen): void {
    this.router.navigate(['/panel/expensas/gastos', p.id]);
  }
}
