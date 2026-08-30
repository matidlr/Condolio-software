import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { EncuestaService } from '../../core/services/encuesta.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CATEGORIAS_ENCUESTA, CategoriaEncuesta, DURACIONES, DuracionPreset, Encuesta,
  EstadisticasEncuestas, EstadoEncuesta, ICON_CAT_ENCUESTA, LABEL_CAT_ENCUESTA, LABEL_MODO_VOTO,
  META_ESTADO, MODOS_VOTO, ModoVotacion,
} from '../../core/models/encuesta.models';

type FiltroEstado = 'todas' | EstadoEncuesta;

@Component({
  selector: 'app-encuestas',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './encuestas.component.html',
  styleUrl: './encuestas.component.scss',
  host: { '(document:click)': 'menu.set(null)' },
})
export class EncuestasComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(EncuestaService);
  private toasts = inject(ToastService);
  private router = inject(Router);

  categorias = CATEGORIAS_ENCUESTA;
  labelCat = LABEL_CAT_ENCUESTA;
  iconCat = ICON_CAT_ENCUESTA;
  metaEstado = META_ESTADO;
  DURACIONES = DURACIONES;
  MODOS_VOTO = MODOS_VOTO;
  labelModo = (m: ModoVotacion) => LABEL_MODO_VOTO[m];

  cargando = signal(true);
  encuestas = signal<Encuesta[]>([]);
  stats = signal<EstadisticasEncuestas>({ total: 0, activas: 0, borradores: 0, cerradas: 0, totalVotos: 0 });

  busqueda = signal('');
  filtroEstado = signal<FiltroEstado>('todas');
  filtroCategoria = signal<CategoriaEncuesta | null>(null);
  catAbierto = signal(true);
  menu = signal<string | null>(null);

  // selección de voto por encuesta (ids de opción)
  seleccion = signal<Record<string, string[]>>({});

  // crear / editar (asistente de 3 pasos)
  form = signal<FormEncuesta | null>(null);
  paso = signal<1 | 2 | 3>(1);
  readonly PASOS = [
    { n: 1, label: 'Contenido' },
    { n: 2, label: 'Configuración' },
    { n: 3, label: 'Revisar y publicar' },
  ] as const;
  readonly MAX_PREGUNTA = 300;

  private consorcioId = computed(() => this.consorcios.activoId());

  filtradas = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const est = this.filtroEstado();
    const cat = this.filtroCategoria();
    return this.encuestas().filter((e) =>
      (est === 'todas' || e.estado === est) &&
      (!cat || e.categoria === cat) &&
      (!q || e.titulo.toLowerCase().includes(q) || e.descripcion.toLowerCase().includes(q)));
  });

  hayFiltros = computed(() =>
    this.filtroEstado() !== 'todas' || this.filtroCategoria() !== null || this.busqueda().trim() !== '');

  limpiarFiltros(): void {
    this.filtroEstado.set('todas');
    this.filtroCategoria.set(null);
    this.busqueda.set('');
  }

  grupos = computed<{ titulo: string; activo: boolean; items: Encuesta[] }[]>(() => {
    const list = this.filtradas();
    const activas = list.filter((e) => e.estado === 'Activa');
    const otras = list.filter((e) => e.estado !== 'Activa');
    const out: { titulo: string; activo: boolean; items: Encuesta[] }[] = [];
    if (activas.length) out.push({ titulo: 'ENCUESTAS ACTIVAS', activo: true, items: activas });
    if (otras.length) {
      out.push({
        titulo: this.filtroEstado() === 'todas' ? 'BORRADORES Y CERRADAS' : 'RESULTADOS',
        activo: false, items: otras,
      });
    }
    return out;
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(); });
  }

  private cargar(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.cargando.set(true);
    this.api.listar(cid).subscribe({
      next: (l) => {
        this.encuestas.set(l.encuestas);
        this.stats.set(l.estadisticas);
        this.api.activas.set(l.estadisticas.activas);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar las encuestas.'); this.cargando.set(false); },
    });
  }

  // ---- helpers de presentación ----
  diasRestantes(e: Encuesta): number | null {
    if (!e.cierreUtc) return null;
    const ms = new Date(e.cierreUtc).getTime() - Date.now();
    return ms <= 0 ? 0 : Math.ceil(ms / 86_400_000);
  }
  textoCierre(e: Encuesta): string {
    const d = this.diasRestantes(e);
    if (d === null) return 'Sin fecha de cierre';
    if (d === 0) return 'Finalizada';
    return `Termina en ${d} día${d === 1 ? '' : 's'}`;
  }
  rango(e: Encuesta): string {
    const ini = e.publicadaUtc ? new Date(e.publicadaUtc) : null;
    const fin = e.cierreUtc ? new Date(e.cierreUtc) : null;
    const fmt = (dt: Date) => dt.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' });
    if (ini && fin) return `${fmt(ini)} – ${fmt(fin)}`;
    if (ini) return `desde ${fmt(ini)}`;
    return '';
  }

  // ---- votar ----
  opcionElegida(encuestaId: string, opcionId: string): boolean {
    return (this.seleccion()[encuestaId] ?? []).includes(opcionId);
  }
  marcaOpcion(e: Encuesta, opcionId: string): string {
    const sel = this.opcionElegida(e.id, opcionId);
    return e.multiplesOpciones ? (sel ? '☑' : '☐') : (sel ? '◉' : '○');
  }
  toggleOpcion(e: Encuesta, opcionId: string): void {
    if (e.estado !== 'Activa') return;
    this.seleccion.update((s) => {
      const actual = s[e.id] ?? [];
      let next: string[];
      if (e.multiplesOpciones) {
        next = actual.includes(opcionId) ? actual.filter((x) => x !== opcionId) : [...actual, opcionId];
      } else {
        next = actual.includes(opcionId) ? [] : [opcionId];
      }
      return { ...s, [e.id]: next };
    });
  }
  puedeVotar(e: Encuesta): boolean {
    return e.estado === 'Activa' && (this.seleccion()[e.id] ?? []).length > 0;
  }
  votar(e: Encuesta): void {
    const cid = this.consorcioId();
    const opciones = this.seleccion()[e.id] ?? [];
    if (!cid || opciones.length === 0) return;
    this.api.votar(cid, e.id, opciones).subscribe({
      next: (act) => {
        this.encuestas.update((l) => l.map((x) => x.id === act.id ? act : x));
        this.seleccion.update((s) => ({ ...s, [e.id]: [] }));
        this.toasts.exito('¡Voto registrado!');
      },
      error: (err) => this.toasts.error(err?.error?.message ?? 'No se pudo registrar el voto.'),
    });
  }

  // ---- acciones ----
  abrirDetalle(e: Encuesta): void {
    this.router.navigate(['/panel/encuestas', e.id]);
  }

  cambiarEstado(e: Encuesta, estado: EstadoEncuesta): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.cambiarEstado(cid, e.id, estado).subscribe({
      next: () => { this.toasts.exito('Encuesta actualizada'); this.cargar(); },
      error: (err) => this.toasts.error(err?.error?.message ?? 'No se pudo actualizar.'),
    });
  }

  eliminar(e: Encuesta): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.eliminar(cid, e.id).subscribe({
      next: () => { this.toasts.exito('Encuesta eliminada'); this.cargar(); },
      error: (err) => this.toasts.error(err?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  // ---- crear / editar ----
  abrirCrear(): void {
    this.paso.set(1);
    this.form.set({
      id: null, titulo: '', descripcion: '', categoria: 'General',
      modoVotacion: 'PorUnidad', duracion: '1w',
      opciones: ['', ''], multiplesOpciones: false, anonima: false,
      cierre: this.fechaEnDias(7), publicar: true,
    });
  }
  abrirEditar(e: Encuesta): void {
    this.paso.set(1);
    this.form.set({
      id: e.id, titulo: e.titulo, descripcion: e.descripcion, categoria: e.categoria,
      modoVotacion: e.modoVotacion, duracion: e.cierreUtc ? 'custom' : '1w',
      opciones: e.opciones.length ? e.opciones.map((o) => o.texto) : ['', ''],
      multiplesOpciones: e.multiplesOpciones, anonima: e.anonima,
      cierre: e.cierreUtc ? e.cierreUtc.slice(0, 10) : '',
      publicar: e.estado !== 'Borrador',
    });
  }

  private readonly DOTS = ['#3b82f6', '#22c55e', '#f59e0b', '#a855f7', '#ef4444', '#14b8a6', '#64748b'];
  dotColor(i: number): string { return this.DOTS[i % this.DOTS.length]; }
  opcionesLlenas(f: { opciones: string[] }): number { return f.opciones.filter((o) => o.trim()).length; }
  diasDuracion(f: FormEncuesta): string {
    if (f.duracion !== 'custom') {
      const d = DURACIONES.find((x) => x.k === f.duracion)?.dias;
      return d ? `${d} día${d === 1 ? '' : 's'}` : 'sin cierre';
    }
    if (!f.cierre) return 'sin cierre';
    const dias = Math.max(0, Math.ceil((new Date(f.cierre).getTime() - Date.now()) / 86_400_000));
    return `${dias} día${dias === 1 ? '' : 's'}`;
  }

  setDuracion(k: DuracionPreset): void {
    const d = DURACIONES.find((x) => x.k === k);
    this.setForm({ duracion: k, cierre: d?.dias ? this.fechaEnDias(d.dias) : this.form()?.cierre ?? '' });
  }

  paso1Valido = computed(() => {
    const f = this.form();
    return !!f && f.titulo.trim().length > 0 && f.opciones.filter((o) => o.trim()).length >= 2;
  });
  avanzar(): void {
    if (this.paso() === 1 && !this.paso1Valido()) return;
    this.paso.update((p) => (p < 3 ? ((p + 1) as 1 | 2 | 3) : p));
  }
  retroceder(): void {
    this.paso.update((p) => (p > 1 ? ((p - 1) as 1 | 2 | 3) : p));
  }
  irAPaso(n: 1 | 2 | 3): void {
    if (n > this.paso() && !this.paso1Valido()) return;
    this.paso.set(n);
  }
  setForm(patch: Partial<FormEncuesta>): void {
    this.form.update((f) => f && { ...f, ...patch });
  }
  private fechaEnDias(n: number): string {
    const d = new Date();
    d.setDate(d.getDate() + n);
    return d.toISOString().slice(0, 10);
  }
  setOpcion(i: number, valor: string): void {
    this.form.update((f) => f && ({ ...f, opciones: f.opciones.map((o, idx) => idx === i ? valor : o) }));
  }
  agregarOpcion(): void {
    this.form.update((f) => f && f.opciones.length < 10 ? { ...f, opciones: [...f.opciones, ''] } : f);
  }
  quitarOpcion(i: number): void {
    this.form.update((f) => f && f.opciones.length > 2 ? { ...f, opciones: f.opciones.filter((_, idx) => idx !== i) } : f);
  }
  formValido = computed(() => {
    const f = this.form();
    if (!f) return false;
    return f.titulo.trim().length > 0 && f.opciones.filter((o) => o.trim()).length >= 2;
  });
  guardar(): void {
    const cid = this.consorcioId();
    const f = this.form();
    if (!cid || !f || !this.formValido()) return;
    const body = {
      titulo: f.titulo.trim(),
      descripcion: f.descripcion.trim(),
      categoria: f.categoria,
      modoVotacion: f.modoVotacion,
      opciones: f.opciones.map((o) => o.trim()).filter(Boolean),
      multiplesOpciones: f.multiplesOpciones,
      anonima: f.anonima,
      cierreUtc: f.cierre ? new Date(f.cierre + 'T23:59:00').toISOString() : null,
      publicar: f.publicar,
    };
    const obs = f.id ? this.api.actualizar(cid, f.id, body) : this.api.crear(cid, body);
    obs.subscribe({
      next: () => { this.form.set(null); this.toasts.exito(f.id ? 'Encuesta actualizada' : 'Encuesta creada'); this.cargar(); },
      error: (err) => this.toasts.error(err?.error?.message ?? 'No se pudo guardar la encuesta.'),
    });
  }
}

interface FormEncuesta {
  id: string | null;
  titulo: string;
  descripcion: string;
  categoria: CategoriaEncuesta;
  modoVotacion: ModoVotacion;
  duracion: DuracionPreset;
  opciones: string[];
  multiplesOpciones: boolean;
  anonima: boolean;
  cierre: string;
  publicar: boolean;
}
