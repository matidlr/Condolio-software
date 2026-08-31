import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import {
  EstadoPaquete, LABEL_TIPO_PAQUETE, Paquete, PaqueteAdminService, PaqueteDetalle, ResumenPaqueteria,
} from '../../core/services/paquete-admin.service';

type Tab = 'Todos' | 'EnRecepcion' | 'Entregado';
const POR_PAGINA = 10;

@Component({
  selector: 'app-paqueteria',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './paqueteria.component.html',
  styleUrl: './paqueteria.component.scss',
  host: { '(document:click)': 'menuId.set(null)' },
})
export class PaqueteriaComponent {
  private api = inject(PaqueteAdminService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());

  labelTipo = LABEL_TIPO_PAQUETE;

  resumen = signal<ResumenPaqueteria>({ porEntregar: 0, llegaronHoy: 0, entregadosHoy: 0, total: 0, necesitanAtencion: 0 });
  paquetes = signal<Paquete[]>([]);
  cargando = signal(true);
  busqueda = signal('');
  tab = signal<Tab>('Todos');
  pagina = signal(1);
  soloHistorial = signal(false);

  menuId = signal<string | null>(null);
  detalle = signal<PaqueteDetalle | null>(null);
  cargandoDetalle = signal(false);

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.resumen(cid).subscribe({ next: (r) => this.resumen.set(r), error: () => {} });
    this.api.listar(cid).subscribe({
      next: (r) => { this.paquetes.set(r.paquetes); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar la paquetería.'); },
    });
  }

  refrescar(): void { const c = this.cid(); if (c) this.cargar(c); }

  filtrados = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const t = this.tab();
    return this.paquetes()
      .filter((p) => t === 'Todos' || p.estado === t)
      .filter((p) => this.soloHistorial() ? p.estado === 'Entregado' : true)
      .filter((p) => !q
        || p.unidadNombre.toLowerCase().includes(q)
        || (p.transportista ?? '').toLowerCase().includes(q));
  });

  totalPaginas = computed(() => Math.max(1, Math.ceil(this.filtrados().length / POR_PAGINA)));
  page = computed(() => {
    const p = Math.min(this.pagina(), this.totalPaginas());
    return this.filtrados().slice((p - 1) * POR_PAGINA, p * POR_PAGINA);
  });

  irPagina(p: number): void { this.pagina.set(Math.min(Math.max(1, p), this.totalPaginas())); }

  setTab(t: Tab): void { this.tab.set(t); this.pagina.set(1); }

  necesitaAtencion(p: Paquete): boolean {
    if (p.estado !== 'EnRecepcion') return false;
    return Date.now() - new Date(p.llegadaUtc).getTime() > 3 * 24 * 3600 * 1000;
  }

  desde(iso: string): string {
    const min = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
    if (min < 60) return `hace ${min} min`;
    const h = Math.floor(min / 60);
    if (h < 24) return `hace ${h} h`;
    const d = Math.floor(h / 24);
    return d === 1 ? 'ayer' : `hace ${d} días`;
  }

  toggleMenu(id: string, ev: Event): void {
    ev.stopPropagation();
    this.menuId.set(this.menuId() === id ? null : id);
  }

  abrirDetalle(p: Paquete): void {
    const cid = this.cid();
    if (!cid) return;
    this.cargandoDetalle.set(true);
    this.detalle.set(null);
    this.api.detalle(cid, p.id).subscribe({
      next: (d) => { this.detalle.set(d); this.cargandoDetalle.set(false); },
      error: () => { this.cargandoDetalle.set(false); this.toasts.error('No se pudo abrir el paquete.'); },
    });
  }

  entregar(p: Paquete): void {
    const cid = this.cid();
    if (!cid) return;
    this.menuId.set(null);
    this.api.entregar(cid, p.id).subscribe({
      next: () => {
        this.toasts.exito('Paquete marcado como entregado');
        this.detalle.set(null);
        this.cargar(cid);
      },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo entregar.'),
    });
  }

  recordatorio(p: Paquete): void {
    const cid = this.cid();
    if (!cid) return;
    this.menuId.set(null);
    this.api.recordatorio(cid, p.id).subscribe({
      next: () => this.toasts.exito('Recordatorio enviado al residente'),
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo enviar el recordatorio.'),
    });
  }
}
