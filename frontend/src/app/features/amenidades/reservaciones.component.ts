import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { forkJoin } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ReservaService } from '../../core/services/reserva.service';
import { AmenidadService } from '../../core/services/amenidad.service';
import { UnidadService } from '../../core/services/unidad.service';
import { ToastService } from '../../core/services/toast.service';
import { Amenidad, opcionesHora } from '../../core/models/amenidad.models';
import { EstadoReserva, LABEL_ESTADO_RESERVA, Reserva, ReservaLista } from '../../core/models/reserva.models';
import { Unidad } from '../../core/models/consorcio.models';

type Rango = 'dia' | 'semana' | 'mes';
type Vista = 'lista' | 'calendario';

@Component({
  selector: 'app-reservaciones',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './reservaciones.component.html',
  styleUrl: './amenidades.component.scss',
})
export class ReservacionesComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(ReservaService);
  private amenidadesApi = inject(AmenidadService);
  private unidadesApi = inject(UnidadService);
  private toasts = inject(ToastService);

  labelEstado = LABEL_ESTADO_RESERVA;
  horas = opcionesHora();

  readonly opcionesFiltro: { valor: EstadoReserva | 'todas'; label: string }[] = [
    { valor: 'todas', label: 'Todas' },
    { valor: 'Pendiente', label: 'Pendientes' },
    { valor: 'Confirmada', label: 'Aprobadas' },
    { valor: 'Rechazada', label: 'Rechazadas' },
    { valor: 'Cancelada', label: 'Canceladas' },
  ];

  data = signal<ReservaLista | null>(null);
  cargando = signal(true);
  ancla = signal(new Date());
  rango = signal<Rango>('mes');
  vista = signal<Vista>('lista');
  busqueda = signal('');
  filtroEstado = signal<EstadoReserva | 'todas'>('todas');
  filtrosAbierto = signal(false);

  nuevaAbierto = signal(false);
  guardando = signal(false);
  amenidades = signal<Amenidad[]>([]);
  unidades = signal<Unidad[]>([]);
  fAmenidad = signal('');
  fUnidad = signal('');
  fFecha = signal(new Date().toISOString().slice(0, 10));
  fInicio = signal(9 * 60);
  fFin = signal(10 * 60);
  fNota = signal('');

  private consorcioId = computed(() => this.consorcios.activoId());

  periodo = computed(() => {
    const base = new Date(this.ancla());
    base.setHours(0, 0, 0, 0);
    let desde = new Date(base), hasta = new Date(base);
    if (this.rango() === 'dia') {
      hasta.setDate(hasta.getDate() + 1);
    } else if (this.rango() === 'semana') {
      desde.setDate(base.getDate() - ((base.getDay() + 6) % 7));
      hasta = new Date(desde); hasta.setDate(desde.getDate() + 7);
    } else {
      desde = new Date(base.getFullYear(), base.getMonth(), 1);
      hasta = new Date(base.getFullYear(), base.getMonth() + 1, 1);
    }
    return { desde, hasta };
  });

  periodoLabel = computed(() => {
    const { desde, hasta } = this.periodo();
    if (this.rango() === 'mes') return desde.toLocaleDateString('es-AR', { month: 'long', year: 'numeric' });
    if (this.rango() === 'dia') return desde.toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' });
    const fin = new Date(hasta); fin.setDate(fin.getDate() - 1);
    return `${desde.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' })} – ${fin.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' })}`;
  });

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const e = this.filtroEstado();
    return (this.data()?.reservas ?? [])
      .filter((r) => e === 'todas' || r.estado === e)
      .filter((r) => !q || r.amenidadNombre.toLowerCase().includes(q)
        || r.solicitante.toLowerCase().includes(q)
        || (r.unidadNombre ?? '').toLowerCase().includes(q));
  });

  constructor() {
    effect(() => {
      const id = this.consorcioId();
      if (!id) return;
      this.periodo(); // track
      this.cargar(id);
    });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    const { desde, hasta } = this.periodo();
    this.api.listar(cid, desde.toISOString(), hasta.toISOString()).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar las reservas.'); this.cargando.set(false); },
    });
  }

  recargar(): void { const id = this.consorcioId(); if (id) this.cargar(id); }

  mover(delta: number): void {
    const d = new Date(this.ancla());
    if (this.rango() === 'dia') d.setDate(d.getDate() + delta);
    else if (this.rango() === 'semana') d.setDate(d.getDate() + delta * 7);
    else d.setMonth(d.getMonth() + delta);
    this.ancla.set(d);
  }

  // ---- nueva reserva ----
  abrirNueva(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.nuevaAbierto.set(true);
    if (!this.amenidades().length) {
      forkJoin([this.amenidadesApi.listar(cid), this.unidadesApi.listar(cid)]).subscribe(([a, u]) => {
        this.amenidades.set(a.amenidades.filter((x) => x.reservable));
        this.unidades.set(u);
        if (a.amenidades.length) this.fAmenidad.set(a.amenidades.find((x) => x.reservable)?.id ?? '');
      });
    }
  }

  crear(): void {
    const cid = this.consorcioId();
    if (!cid || !this.fAmenidad() || this.guardando()) return;
    if (this.fFin() <= this.fInicio()) { this.toasts.error('El horario de fin debe ser posterior al de inicio.'); return; }
    this.guardando.set(true);
    const fecha = this.fFecha();
    const iso = (min: number) => new Date(`${fecha}T${String(Math.floor(min / 60)).padStart(2, '0')}:${String(min % 60).padStart(2, '0')}:00`).toISOString();
    this.api.crear(cid, {
      amenidadId: this.fAmenidad(),
      unidadId: this.fUnidad() || null,
      inicio: iso(this.fInicio()),
      fin: iso(this.fFin()),
      nota: this.fNota().trim() || null,
    }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.nuevaAbierto.set(false);
        this.fNota.set('');
        this.toasts.exito('Reserva creada');
        this.recargar();
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo crear la reserva.'); },
    });
  }

  cambiar(r: Reserva, estado: EstadoReserva): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.cambiarEstado(cid, r.id, estado).subscribe({
      next: () => { this.toasts.exito('Reserva actualizada'); this.recargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo actualizar.'),
    });
  }

  eliminar(r: Reserva): void {
    const cid = this.consorcioId();
    if (!cid || !confirm('¿Eliminar esta reserva?')) return;
    this.api.eliminar(cid, r.id).subscribe({
      next: () => { this.toasts.exito('Reserva eliminada'); this.recargar(); },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }

  moneda(n: number): string {
    return n.toLocaleString('es-AR', { style: 'currency', currency: 'ARS' });
  }

  // ---- vista calendario (mes) ----
  grillaMes = computed(() => {
    const base = new Date(this.ancla());
    const primero = new Date(base.getFullYear(), base.getMonth(), 1);
    const arranque = new Date(primero);
    arranque.setDate(1 - ((primero.getDay() + 6) % 7));
    const porDia = new Map<string, Reserva[]>();
    for (const r of this.visibles()) {
      const k = r.inicio.slice(0, 10);
      porDia.set(k, [...(porDia.get(k) ?? []), r]);
    }
    const celdas: { fecha: string; dia: number; otroMes: boolean; reservas: Reserva[] }[] = [];
    for (let i = 0; i < 42; i++) {
      const d = new Date(arranque);
      d.setDate(arranque.getDate() + i);
      const fecha = d.toISOString().slice(0, 10);
      celdas.push({ fecha, dia: d.getDate(), otroMes: d.getMonth() !== base.getMonth(), reservas: porDia.get(fecha) ?? [] });
    }
    return celdas;
  });
}
