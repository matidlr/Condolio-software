import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfigExpensas, ExpensasService, FondoReservaTipo } from '../../core/services/expensas.service';

@Component({
  selector: 'app-ex-config',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './expensas-config.component.html',
  styleUrl: './expensas.shared.scss',
})
export class ExpensasConfigComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());

  cfg = signal<ConfigExpensas | null>(null);
  cargando = signal(true);
  guardando = signal(false);

  // Mercado Pago
  mpToken = signal('');
  mpPublicKey = signal('');
  guardandoMp = signal(false);

  fondoTipos: { v: FondoReservaTipo; t: string }[] = [
    { v: 'Ninguno', t: 'Sin fondo de reserva' },
    { v: 'PorcentajeDeGastos', t: '% sobre los gastos del mes' },
    { v: 'MontoFijo', t: 'Monto fijo mensual' },
  ];

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.config(cid).subscribe({
      next: (c) => { this.cfg.set(c); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar la configuración.'); },
    });
  }

  set<K extends keyof ConfigExpensas>(k: K, v: ConfigExpensas[K]): void {
    this.cfg.update((c) => (c ? { ...c, [k]: v } : c));
  }

  guardar(): void {
    const cid = this.cid();
    const c = this.cfg();
    if (!cid || !c || this.guardando()) return;
    this.guardando.set(true);
    this.api.guardarConfig(cid, {
      diaPrimerVencimiento: c.diaPrimerVencimiento,
      diaSegundoVencimiento: c.diaSegundoVencimiento,
      recargoSegundoVencimientoPct: c.recargoSegundoVencimientoPct,
      tasaInteresMoraMensualPct: c.tasaInteresMoraMensualPct,
      fondoReservaTipo: c.fondoReservaTipo,
      fondoReservaValor: c.fondoReservaValor,
      inquilinoPagaOrdinarias: c.inquilinoPagaOrdinarias,
      redondearAlPeso: c.redondearAlPeso,
    }).subscribe({
      next: (r) => { this.cfg.set(r); this.guardando.set(false); this.toasts.exito('Configuración guardada'); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  guardarMp(): void {
    const cid = this.cid();
    if (!cid || this.guardandoMp()) return;
    const token = this.mpToken().trim();
    if (!token) { this.toasts.error('Pegá el Access Token de Mercado Pago.'); return; }
    this.guardandoMp.set(true);
    this.api.guardarMercadoPago(cid, token, this.mpPublicKey().trim() || null).subscribe({
      next: (r) => {
        this.cfg.set(r); this.mpToken.set(''); this.mpPublicKey.set('');
        this.guardandoMp.set(false); this.toasts.exito('Mercado Pago conectado');
      },
      error: (e) => { this.guardandoMp.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  desconectarMp(): void {
    const cid = this.cid();
    if (!cid || this.guardandoMp()) return;
    this.guardandoMp.set(true);
    this.api.guardarMercadoPago(cid, null, null).subscribe({
      next: (r) => { this.cfg.set(r); this.guardandoMp.set(false); this.toasts.exito('Mercado Pago desconectado'); },
      error: (e) => { this.guardandoMp.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo desconectar.'); },
    });
  }
}
