import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { MiAmenidad, MiAmenidadService, MiReserva, Slot } from '../../core/services/mi-amenidad.service';
import { PortalService } from '../../core/services/portal.service';
import { ToastService } from '../../core/services/toast.service';

const DIAS = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
const DIAS_ORD = [1, 2, 3, 4, 5, 6, 0]; // Lun..Dom

@Component({
  selector: 'app-portal-amenidades',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './portal-amenidades.component.html',
  styleUrl: './portal-amenidades.component.scss',
})
export class PortalAmenidadesComponent {
  private api = inject(MiAmenidadService);
  private adjuntos = inject(AdjuntoService);
  private toasts = inject(ToastService);
  portal = inject(PortalService);

  diasOrden = DIAS_ORD;
  diasLabel = DIAS;

  vista = signal<'directorio' | 'detalle' | 'solicitar'>('directorio');
  tab = signal<'amenidades' | 'reservas'>('amenidades');
  cargando = signal(true);

  amenidades = signal<MiAmenidad[]>([]);
  portadas = signal<Record<string, string>>({});
  reservasActivas = signal<MiReserva[]>([]);
  reservasPrevias = signal<MiReserva[]>([]);
  verPrevias = signal(false);

  sel = signal<MiAmenidad | null>(null);

  // solicitar
  mes = signal(new Date());
  fecha = signal<string>(new Date().toISOString().slice(0, 10));
  slots = signal<Slot[]>([]);
  slotsCargando = signal(false);
  confirmar = signal<Slot | null>(null);
  nota = signal('');
  enviando = signal(false);

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.amenidades().subscribe({
      next: (l) => {
        this.amenidades.set(l);
        this.cargando.set(false);
        for (const a of l) this.cargarPortada(a);
      },
      error: () => { this.toasts.error('No pudimos cargar las amenidades.'); this.cargando.set(false); },
    });
    this.api.misReservas().subscribe({
      next: (r) => { this.reservasActivas.set(r.activas); this.reservasPrevias.set(r.previas); },
      error: () => {},
    });
  }

  private cargarPortada(a: MiAmenidad): void {
    const id = a.imagenesIds?.[0];
    if (!id || this.portadas()[a.id]) return;
    this.adjuntos.descargar(id).subscribe({
      next: (blob) => this.portadas.update((p) => ({ ...p, [a.id]: URL.createObjectURL(blob) })),
      error: () => {},
    });
  }
  portada(a: MiAmenidad): string | null { return this.portadas()[a.id] ?? null; }

  // ---- detalle ----
  abrir(a: MiAmenidad): void {
    this.sel.set(a);
    this.vista.set('detalle');
    this.api.amenidad(a.id).subscribe({ next: (full) => this.sel.set(full) });
  }

  horarioTexto(a: MiAmenidad): string {
    const h = a.horarios?.find((x) => !x.cerrado);
    if (!h) return 'Sin horario configurado';
    return `${this.hhmm(h.abreMin)} - ${this.hhmm(h.cierraMin)}`;
  }
  duracionTexto(a: MiAmenidad): string {
    const m = a.intervaloMinutos || 60;
    return m % 60 === 0 ? `${m / 60} hora${m / 60 === 1 ? '' : 's'}` : `${m} min`;
  }
  diaAbierto(a: MiAmenidad, dow: number): boolean {
    const h = a.horarios?.find((x) => x.dia === dow);
    return !!h && !h.cerrado;
  }
  private hhmm(min: number): string {
    const h = Math.floor(min / 60), m = min % 60;
    const ap = h < 12 ? 'AM' : 'PM';
    const h12 = h % 12 === 0 ? 12 : h % 12;
    return `${h12}:${m.toString().padStart(2, '0')} ${ap}`;
  }

  // ---- solicitar ----
  irSolicitar(): void {
    const hoy = new Date();
    this.mes.set(new Date(hoy.getFullYear(), hoy.getMonth(), 1));
    this.fecha.set(hoy.toISOString().slice(0, 10));
    this.confirmar.set(null);
    this.nota.set('');
    this.vista.set('solicitar');
    this.cargarSlots();
  }

  gridMes = computed(() => {
    const base = this.mes();
    const primero = new Date(base.getFullYear(), base.getMonth(), 1);
    const dias = new Date(base.getFullYear(), base.getMonth() + 1, 0).getDate();
    const offset = primero.getDay();
    const celdas: (string | null)[] = Array(offset).fill(null);
    for (let d = 1; d <= dias; d++) {
      celdas.push(new Date(base.getFullYear(), base.getMonth(), d).toISOString().slice(0, 10));
    }
    return celdas;
  });
  mesLabel = computed(() =>
    this.mes().toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));

  cambiarMes(delta: number): void {
    const m = this.mes();
    this.mes.set(new Date(m.getFullYear(), m.getMonth() + delta, 1));
  }
  hoy(): void {
    const h = new Date();
    this.mes.set(new Date(h.getFullYear(), h.getMonth(), 1));
    this.elegirFecha(h.toISOString().slice(0, 10));
  }
  esPasado(iso: string | null): boolean {
    if (!iso) return true;
    return iso < new Date().toISOString().slice(0, 10);
  }
  elegirFecha(iso: string): void {
    if (this.esPasado(iso)) return;
    this.fecha.set(iso);
    this.confirmar.set(null);
    this.cargarSlots();
  }
  private cargarSlots(): void {
    const a = this.sel();
    if (!a) return;
    this.slotsCargando.set(true);
    this.api.slots(a.id, this.fecha()).subscribe({
      next: (s) => { this.slots.set(s); this.slotsCargando.set(false); },
      error: () => { this.slots.set([]); this.slotsCargando.set(false); },
    });
  }

  elegirSlot(s: Slot): void {
    this.nota.set('');
    this.confirmar.set(s);
  }

  enviarSolicitud(): void {
    const a = this.sel(); const s = this.confirmar();
    if (!a || !s || this.enviando()) return;
    this.enviando.set(true);
    this.api.solicitar({ amenidadId: a.id, inicio: s.inicio, fin: s.fin, nota: this.nota().trim() || null }).subscribe({
      next: (r) => {
        this.enviando.set(false);
        this.confirmar.set(null);
        this.toasts.exito(r.estado === 'Pendiente'
          ? 'Reserva solicitada — a la espera de aprobación'
          : 'Reserva confirmada');
        this.reservasActivas.update((l) => [r, ...l]);
        this.portal.cargarCasa().subscribe();
        this.vista.set('directorio');
        this.tab.set('reservas');
      },
      error: (e) => {
        this.enviando.set(false);
        this.toasts.error(e?.error?.message ?? 'No se pudo solicitar la reserva.');
      },
    });
  }

  cancelarReserva(r: MiReserva): void {
    this.api.cancelar(r.id).subscribe({
      next: () => {
        this.reservasActivas.update((l) => l.filter((x) => x.id !== r.id));
        this.toasts.exito('Reserva cancelada');
        this.portal.cargarCasa().subscribe();
      },
      error: () => this.toasts.error('No se pudo cancelar.'),
    });
  }

  estadoMeta(e: string): { label: string; color: string } {
    switch (e) {
      case 'Confirmada': return { label: 'Confirmada', color: '#16a34a' };
      case 'Pendiente': return { label: 'Pendiente', color: '#d97706' };
      case 'Rechazada': return { label: 'Rechazada', color: '#dc2626' };
      default: return { label: 'Cancelada', color: '#64748b' };
    }
  }
}
