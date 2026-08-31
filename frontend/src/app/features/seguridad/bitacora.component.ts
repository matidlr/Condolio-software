import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { AccesoAdminService, RegistroBitacora } from '../../core/services/acceso-admin.service';
import { ToastService } from '../../core/services/toast.service';
import { ICON_VISITA, LABEL_VISITA, TIPOS_VEHICULO } from '../../core/models/pase-acceso.models';

@Component({
  selector: 'app-bitacora',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './bitacora.component.html',
  styleUrl: './bitacora.component.scss',
})
export class BitacoraComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(AccesoAdminService);
  private toasts = inject(ToastService);

  labelVisita = LABEL_VISITA;
  iconVisita = ICON_VISITA;

  cargando = signal(true);
  registros = signal<RegistroBitacora[]>([]);
  adentroAhora = signal(0);

  fecha = signal(this.hoyStr());
  dias = signal(1);
  filtro = signal<'todos' | 'adentro' | 'salio'>('todos');
  busqueda = signal('');

  private cid = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    if (!q) return this.registros();
    return this.registros().filter((r) =>
      r.visitanteNombre.toLowerCase().includes(q) ||
      (r.patente ?? '').toLowerCase().includes(q) ||
      r.unidad.toLowerCase().includes(q));
  });

  tituloRango = computed(() => {
    if (this.dias() > 1) return `Últimos ${this.dias()} días`;
    const d = new Date(this.fecha() + 'T00:00:00');
    return d.toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' });
  });

  constructor() {
    effect(() => { if (this.cid()) this.cargar(); });
  }

  private hoyStr(): string {
    const n = new Date();
    return `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`;
  }

  private cargar(): void {
    const cid = this.cid();
    if (!cid) return;
    this.cargando.set(true);
    this.api.bitacora(cid, this.fecha(), this.dias(), this.filtro()).subscribe({
      next: (b) => { this.registros.set(b.registros); this.adentroAhora.set(b.adentroAhora); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar la bitácora.'); this.cargando.set(false); },
    });
  }

  cambiarDia(delta: number): void {
    this.dias.set(1);
    const d = new Date(this.fecha() + 'T00:00:00');
    d.setDate(d.getDate() + delta);
    this.fecha.set(`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`);
    this.cargar();
  }
  hoy(): void { this.dias.set(1); this.fecha.set(this.hoyStr()); this.cargar(); }
  ultimos7(): void { this.dias.set(7); this.cargar(); }
  setFiltro(f: 'todos' | 'adentro' | 'salio'): void { this.filtro.set(f); this.cargar(); }

  iconoVehiculo(v: string): string {
    return TIPOS_VEHICULO.find((x) => x.value === v)?.icon ?? '';
  }

  registrarSalida(r: RegistroBitacora): void {
    const cid = this.cid();
    if (!cid) return;
    this.api.egreso(cid, r.id).subscribe({
      next: () => { this.toasts.exito('Salida registrada'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo registrar la salida.'),
    });
  }

  exportar(): void {
    const filas = [['Visitante', 'Propósito', 'Ingreso', 'Egreso', 'Estado', 'Vehículo', 'Patente', 'Unidad', 'Registró']];
    for (const r of this.visibles()) {
      filas.push([
        r.visitanteNombre, this.labelVisita[r.tipoVisita],
        new Date(r.ingresoUtc).toLocaleString('es-AR'),
        r.egresoUtc ? new Date(r.egresoUtc).toLocaleString('es-AR') : '',
        r.egresoUtc ? 'Salió' : 'Adentro',
        r.vehiculo === 'SinVehiculo' ? '' : r.vehiculo, r.patente ?? '', r.unidad, r.registradoPor,
      ]);
    }
    const csv = '﻿' + filas.map((f) => f.map((c) => `"${(c ?? '').replace(/"/g, '""')}"`).join(',')).join('\r\n');
    const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8;' }));
    const a = document.createElement('a');
    a.href = url;
    a.download = `bitacora-${this.fecha()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
