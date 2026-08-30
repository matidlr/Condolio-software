import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { EventoService } from '../../core/services/evento.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CATEGORIAS_EVENTO, CategoriaEvento, Evento, META_EVENTO,
} from '../../core/models/evento.models';

type Vista = 'calendario' | 'lista';

@Component({
  selector: 'app-calendario',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './calendario.component.html',
  styleUrl: './calendario.component.scss',
  host: { '(document:click)': 'menu.set(null)' },
})
export class CalendarioComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(EventoService);
  private toasts = inject(ToastService);

  categorias = CATEGORIAS_EVENTO;
  meta = META_EVENTO;

  mes = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  vista = signal<Vista>('calendario');
  busqueda = signal('');
  eventos = signal<Evento[]>([]);
  cargando = signal(true);
  menu = signal<string | null>(null);

  // modal
  modalAbierto = signal(false);
  editId = signal<string | null>(null);
  fTitulo = signal('');
  fDesc = signal('');
  fUbicacion = signal('');
  fCategoria = signal<CategoriaEvento>('General');
  fTodoElDia = signal(false);
  fFechaIni = signal('');
  fHoraIni = signal('08:00');
  fFechaFin = signal('');
  fHoraFin = signal('09:00');
  fNotificar = signal(false);
  guardando = signal(false);

  private consorcioId = computed(() => this.consorcios.activoId());

  mesLabel = computed(() =>
    this.mes().toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    return this.eventos()
      .filter((e) => !q || e.titulo.toLowerCase().includes(q) || (e.ubicacion ?? '').toLowerCase().includes(q))
      .sort((a, b) => a.inicioUtc.localeCompare(b.inicioUtc));
  });

  grilla = computed(() => {
    const base = this.mes();
    const primero = new Date(base.getFullYear(), base.getMonth(), 1);
    const arranque = new Date(primero);
    arranque.setDate(1 - ((primero.getDay() + 6) % 7));
    const fechaLocal = (d: Date) =>
      `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    const hoy = fechaLocal(new Date());
    const porDia = new Map<string, Evento[]>();
    for (const e of this.visibles()) {
      const k = e.inicioUtc.slice(0, 10);
      porDia.set(k, [...(porDia.get(k) ?? []), e]);
    }
    const celdas: { fecha: string; dia: number; otroMes: boolean; hoy: boolean; eventos: Evento[] }[] = [];
    for (let i = 0; i < 42; i++) {
      const d = new Date(arranque);
      d.setDate(arranque.getDate() + i);
      const fecha = fechaLocal(d);
      celdas.push({
        fecha, dia: d.getDate(),
        otroMes: d.getMonth() !== base.getMonth(),
        hoy: fecha === hoy,
        eventos: porDia.get(fecha) ?? [],
      });
    }
    return celdas;
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
  }

  private periodo() {
    const base = this.mes();
    const desde = new Date(base.getFullYear(), base.getMonth() - 1, 1);
    const hasta = new Date(base.getFullYear(), base.getMonth() + 2, 1);
    // Fechas naive en hora local (el sistema guarda datetimes sin zona; ver convención de zona horaria).
    const fmt = (d: Date) =>
      `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}T00:00:00`;
    return { desde: fmt(desde), hasta: fmt(hasta) };
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    const { desde, hasta } = this.periodo();
    this.api.listar(cid, desde, hasta).subscribe({
      next: (l) => { this.eventos.set(l); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar el calendario.'); this.cargando.set(false); },
    });
  }

  recargar(): void { const id = this.consorcioId(); if (id) this.cargar(id); }

  moverMes(delta: number): void {
    const d = new Date(this.mes());
    d.setMonth(d.getMonth() + delta);
    this.mes.set(d);
  }
  hoy(): void {
    const n = new Date();
    this.mes.set(new Date(n.getFullYear(), n.getMonth(), 1));
  }

  hora(iso: string): string {
    return new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  // ---- modal ----
  abrirNuevo(fecha?: string): void {
    const n = new Date();
    const f = fecha ?? `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`;
    this.editId.set(null);
    this.fTitulo.set(''); this.fDesc.set(''); this.fUbicacion.set('');
    this.fCategoria.set('General'); this.fTodoElDia.set(false); this.fNotificar.set(false);
    this.fFechaIni.set(f); this.fFechaFin.set(f);
    this.fHoraIni.set('08:00'); this.fHoraFin.set('09:00');
    this.modalAbierto.set(true);
  }

  editar(e: Evento): void {
    this.editId.set(e.id);
    this.fTitulo.set(e.titulo);
    this.fDesc.set(e.descripcion ?? '');
    this.fUbicacion.set(e.ubicacion ?? '');
    this.fCategoria.set(e.categoria);
    this.fTodoElDia.set(e.todoElDia);
    this.fNotificar.set(e.notificoComunidad);
    // e.inicioUtc / e.finUtc son strings naive "YYYY-MM-DDTHH:mm:ss": se parsean sin Date para evitar corrimiento de zona.
    const [fIni, hIni = '08:00'] = e.inicioUtc.split('T');
    const [fFin, hFin = '09:00'] = e.finUtc.split('T');
    this.fFechaIni.set(fIni);
    this.fFechaFin.set(fFin);
    this.fHoraIni.set(hIni.slice(0, 5));
    this.fHoraFin.set(hFin.slice(0, 5));
    this.modalAbierto.set(true);
  }

  puedeGuardar = computed(() => this.fTitulo().trim().length > 0 && !this.guardando());

  guardar(): void {
    const cid = this.consorcioId();
    if (!cid || !this.puedeGuardar()) return;
    this.guardando.set(true);
    // Naive local: el backend guarda el datetime tal cual, sin conversión de zona.
    const ini = `${this.fFechaIni()}T${this.fTodoElDia() ? '00:00' : this.fHoraIni()}:00`;
    const fin = `${this.fFechaFin()}T${this.fTodoElDia() ? '23:59' : this.fHoraFin()}:00`;
    const body = {
      titulo: this.fTitulo().trim(),
      descripcion: this.fDesc().trim() || null,
      ubicacion: this.fUbicacion().trim() || null,
      categoria: this.fCategoria(),
      inicioUtc: ini,
      finUtc: fin,
      todoElDia: this.fTodoElDia(),
      notificarComunidad: this.fNotificar(),
    };
    const id = this.editId();
    const op = id ? this.api.actualizar(cid, id, body) : this.api.crear(cid, body);
    op.subscribe({
      next: () => {
        this.guardando.set(false);
        this.modalAbierto.set(false);
        this.toasts.exito(id ? 'Evento actualizado' : 'Evento creado');
        this.cargar(cid);
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar el evento.'); },
    });
  }

  eliminar(e: Evento): void {
    const cid = this.consorcioId();
    if (!cid || !confirm(`¿Eliminar "${e.titulo}"?`)) return;
    this.api.eliminar(cid, e.id).subscribe({
      next: () => { this.toasts.exito('Evento eliminado'); this.cargar(cid); },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }
}
