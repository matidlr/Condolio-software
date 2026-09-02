import { Component, computed, effect, inject, signal } from '@angular/core';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import { ExpensasService, Morosidad, MorosidadUnidad } from '../../core/services/expensas.service';

type Semaforo = 'sin' | 'porVencer' | 'v1' | 'v2' | 'v3';

@Component({
  selector: 'app-ex-morosidad',
  standalone: true,
  imports: [],
  templateUrl: './morosidad.component.html',
  styleUrl: './expensas.shared.scss',
})
export class MorosidadComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());

  data = signal<Morosidad | null>(null);
  cargando = signal(true);
  vista = signal<'tarjetas' | 'tabla'>('tarjetas');

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  fmt(n: number): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.morosidad(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar la morosidad.'); },
    });
  }
  refrescar(): void { const c = this.cid(); if (c) this.cargar(c); }

  semaforo(u: MorosidadUnidad): Semaforo {
    if (u.saldoVencido > 0) {
      if (u.diasAtraso > 60) return 'v3';
      if (u.diasAtraso > 30) return 'v2';
      return 'v1';
    }
    return u.saldoPorVencer > 0 ? 'porVencer' : 'sin';
  }
  estadoTxt(u: MorosidadUnidad): string {
    switch (this.semaforo(u)) {
      case 'sin': return 'Sin adeudos';
      case 'porVencer': return `Por vencer · ${this.fmt(u.saldoPorVencer)}`;
      default: return `${u.diasAtraso} días vencido · ${this.fmt(u.saldoVencido)}`;
    }
  }

  pisos = computed(() => {
    const u = this.data()?.unidades ?? [];
    const map = new Map<number, MorosidadUnidad[]>();
    for (const x of u) {
      if (!map.has(x.piso)) map.set(x.piso, []);
      map.get(x.piso)!.push(x);
    }
    return [...map.entries()].sort((a, b) => a[0] - b[0]).map(([piso, unidades]) => ({ piso, unidades }));
  });

  exportarCsv(): void {
    const d = this.data();
    if (!d) return;
    const filas = [
      ['Unidad', 'Piso', 'Seccion', 'Estado', 'Saldo vencido', 'Saldo por vencer', 'Dias atraso', 'Cargos vencidos'],
      ...d.unidades.map((u) => [
        u.nombre, String(u.piso), u.seccion ?? '', this.estadoTxt(u),
        u.saldoVencido.toFixed(2), u.saldoPorVencer.toFixed(2), String(u.diasAtraso), String(u.cargosVencidos),
      ]),
    ];
    const csv = filas.map((f) => f.map((c) => `"${String(c).replace(/"/g, '""')}"`).join(',')).join('\r\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `morosidad-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(a.href);
  }
}
