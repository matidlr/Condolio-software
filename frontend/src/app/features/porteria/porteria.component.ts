import { Component, ElementRef, OnDestroy, computed, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Bitacora, EntradaManual, Paquete, PersonalTurno, PorteriaContexto, PorteriaService, RegistrarPaquete,
  RegistroBitacora, ResumenAcceso, ResumenPaqueteria, TipoPaquete, TRANSPORTISTAS, TurnoActual,
  UnidadDetalle, UnidadRef, Verificacion,
} from '../../core/services/porteria.service';
import { LABEL_TIPO_PERSONAL, TipoPersonal, TIPOS_PERSONAL } from '../../core/services/personal.service';
import { ICON_VISITA, LABEL_VISITA, TIPOS_VEHICULO, TIPOS_VISITA } from '../../core/models/pase-acceso.models';

type Vista =
  | 'inicio' | 'entradas' | 'escanear' | 'manual' | 'salidas' | 'bitacora'
  | 'paqueteria' | 'pq-nuevo' | 'pq-entregar' | 'pq-registro' | 'unidades' | 'config';
type Tab = 'inicio' | 'entradas' | 'paqueteria' | 'unidades' | 'config';

const LETRAS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');
const NUMEROS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', 'del', '0', 'clear'];

interface NuevoPaquete {
  fecha: string; hora: string;
  tipo: TipoPaquete; cantidad: number;
  transportista: string | null; unidadId: string | null;
  descripcion: string | null;
  foto: string | null;
}

/** Reduce una imagen a JPEG (máx `max` px de lado) y devuelve un data URL. */
export function comprimirImagen(file: File, max = 1280, calidad = 0.8): Promise<string> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    const url = URL.createObjectURL(file);
    img.onload = () => {
      URL.revokeObjectURL(url);
      const escala = Math.min(1, max / Math.max(img.width, img.height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(img.width * escala);
      canvas.height = Math.round(img.height * escala);
      const ctx = canvas.getContext('2d');
      if (!ctx) { reject(new Error('canvas')); return; }
      ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
      resolve(canvas.toDataURL('image/jpeg', calidad));
    };
    img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('img')); };
    img.src = url;
  });
}

@Component({
  selector: 'app-porteria',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './porteria.component.html',
  styleUrl: './porteria.component.scss',
})
export class PorteriaComponent implements OnDestroy {
  private api = inject(PorteriaService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toasts = inject(ToastService);

  video = viewChild<ElementRef<HTMLVideoElement>>('video');

  tiposVisita = TIPOS_VISITA;
  tiposVehiculo = TIPOS_VEHICULO;
  labelVisita = LABEL_VISITA;
  iconVisita = ICON_VISITA;
  letras = LETRAS;
  numeros = NUMEROS;

  vista = signal<Vista>('inicio');
  ctx = signal<PorteriaContexto | null>(null);
  resumen = signal<ResumenAcceso>({ adentroAhora: 0, entradasHoy: 0, salidasHoy: 0 });

  cuenta = computed(() => this.auth.sesion());
  iniciales = computed(() => {
    const n = (this.auth.sesion()?.nombre ?? 'Portería').trim().split(/\s+/);
    return ((n[0]?.[0] ?? 'P') + (n[1]?.[0] ?? '')).toUpperCase();
  });

  private subEntradas = ['escanear', 'manual', 'salidas', 'bitacora'];
  private subPaq = ['pq-nuevo', 'pq-entregar', 'pq-registro'];

  tabActiva = computed<Tab>(() => {
    const v = this.vista();
    if (v === 'entradas' || this.subEntradas.includes(v)) return 'entradas';
    if (this.subPaq.includes(v)) return 'paqueteria';
    return v as Tab;
  });
  esSubvista = computed(() => this.subEntradas.includes(this.vista()) || this.subPaq.includes(this.vista()));
  private volverA = computed<Vista>(() => this.subPaq.includes(this.vista()) ? 'paqueteria' : 'entradas');

  tituloVista = computed(() => {
    switch (this.vista()) {
      case 'inicio': return 'Inicio';
      case 'entradas': return 'Entradas';
      case 'escanear': return 'Escanear QR';
      case 'manual': return 'Nueva entrada';
      case 'salidas': return 'Registrar salidas';
      case 'bitacora': return 'Bitácora digital';
      case 'paqueteria': return 'Administración de paquetería';
      case 'pq-nuevo': return 'Nuevo paquete';
      case 'pq-entregar': return 'Entregar paquete';
      case 'pq-registro': return 'Registro de paquetería';
      case 'unidades': return 'Unidades';
      case 'config': return 'Configuración';
    }
  });

  // ---- turno ----
  turno = signal<TurnoActual | null>(null);
  personalCaseta = signal<PersonalTurno[]>([]);
  sheetTurno = signal(false);
  buscarPersonal = signal('');
  iniciandoTurno = signal(false);
  dlgFinTurno = signal(false);
  notaFinTurno = signal('');
  finalizandoTurno = signal(false);

  personalAgrupado = computed(() => {
    const q = this.buscarPersonal().trim().toLowerCase();
    const l = q
      ? this.personalCaseta().filter((p) => `${p.nombre} ${p.apellido}`.toLowerCase().includes(q))
      : this.personalCaseta();
    const grupos = new Map<string, PersonalTurno[]>();
    for (const p of l) (grupos.get(p.tipo) ?? grupos.set(p.tipo, []).get(p.tipo)!).push(p);
    return TIPOS_PERSONAL
      .map((t) => ({ tipo: t.value, label: t.label.toUpperCase(), icon: t.icon, items: grupos.get(t.value) ?? [] }))
      .filter((g) => g.items.length > 0);
  });

  labelTipoPersonal(t: string): string { return LABEL_TIPO_PERSONAL[t as TipoPersonal] ?? t; }

  minutosDesde(iso: string): string {
    const min = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 60000));
    if (min < 60) return `${min}m`;
    const h = Math.floor(min / 60);
    return h < 24 ? `${h}h ${min % 60}m` : `${Math.floor(h / 24)}d`;
  }
  horaCorta(iso: string): string {
    return new Date(iso).toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  cargarTurno(): void {
    this.api.turnoActual().subscribe({ next: (t) => this.turno.set(t), error: () => {} });
  }
  abrirSheetTurno(): void {
    this.buscarPersonal.set('');
    this.sheetTurno.set(true);
    this.api.personalCaseta().subscribe({
      next: (l) => this.personalCaseta.set(l),
      error: () => this.personalCaseta.set([]),
    });
  }
  iniciarTurno(p: PersonalTurno): void {
    if (this.iniciandoTurno()) return;
    this.iniciandoTurno.set(true);
    this.api.iniciarTurno(p.id).subscribe({
      next: (t) => {
        this.iniciandoTurno.set(false);
        this.turno.set(t);
        this.sheetTurno.set(false);
        this.toasts.exito('Turno iniciado exitosamente');
      },
      error: (e) => {
        this.iniciandoTurno.set(false);
        this.toasts.error(e?.error?.message ?? 'No se pudo iniciar el turno.');
      },
    });
  }
  confirmarFinTurno(): void {
    if (this.finalizandoTurno()) return;
    this.finalizandoTurno.set(true);
    this.api.finalizarTurno(this.notaFinTurno().trim() || null).subscribe({
      next: () => {
        this.finalizandoTurno.set(false);
        this.turno.set(null);
        this.dlgFinTurno.set(false);
        this.notaFinTurno.set('');
        this.toasts.exito('Turno finalizado');
      },
      error: (e) => {
        this.finalizandoTurno.set(false);
        this.toasts.error(e?.error?.message ?? 'No se pudo finalizar el turno.');
      },
    });
  }

  // escáner
  escaneando = signal(false);
  verificando = signal(false);
  preview = signal<Verificacion | null>(null);
  resultado = signal<Verificacion | null>(null);
  docVisitante = signal('');
  patenteVisitante = signal('');
  confirmando = signal(false);
  manualToken = signal('');
  soportaCamara = signal('BarcodeDetector' in window);
  private stream: MediaStream | null = null;
  private detector: any = null;
  private raf = 0;
  private ultimoToken = '';

  // entrada manual
  em = signal<EntradaManual>(this.emVacia());
  unidades = signal<UnidadRef[]>([]);
  guardandoEm = signal(false);
  unidadManualLabel = computed(() => {
    const id = this.em().unidadId;
    return id ? (this.unidades().find((u) => u.id === id)?.nombre ?? '—') : 'Administración';
  });

  // unidades (tab)
  tecleo = signal('');
  unidadSel = signal<UnidadDetalle | null>(null);
  verTodas = signal(false);
  buscarUnidad = signal('');
  cargandoUnidad = signal(false);

  unidadesFiltradas = computed(() => {
    const q = this.tecleo().trim().toLowerCase();
    if (!q) return [];
    return this.unidades().filter((u) => u.nombre.toLowerCase().replace(/\s+/g, '').includes(q.replace(/\s+/g, '')));
  });
  unidadesModal = computed(() => {
    const q = this.buscarUnidad().trim().toLowerCase();
    if (!q) return this.unidades();
    return this.unidades().filter((u) => u.nombre.toLowerCase().includes(q));
  });

  tecla(k: string): void {
    if (k === 'del') this.tecleo.update((t) => t.slice(0, -1));
    else if (k === 'clear') this.tecleo.set('');
    else this.tecleo.update((t) => (t + k).slice(0, 8));
  }
  abrirUnidad(id: string): void {
    this.cargandoUnidad.set(true);
    this.api.unidad(id).subscribe({
      next: (u) => { this.unidadSel.set(u); this.cargandoUnidad.set(false); this.verTodas.set(false); },
      error: () => { this.cargandoUnidad.set(false); this.toasts.error('No se pudo abrir la unidad.'); },
    });
  }
  registrarVisitaUnidad(): void {
    const u = this.unidadSel();
    if (!u) return;
    this.em.set({ ...this.emVacia(), tipoVisita: 'Visita', unidadId: u.id });
    this.unidadSel.set(null);
    this.tecleo.set('');
    this.vista.set('manual');
  }

  // salidas / bitácora
  adentro = signal<RegistroBitacora[]>([]);
  bitacora = signal<Bitacora>({ registros: [], adentroAhora: 0 });
  cargandoLista = signal(false);
  alertas = signal<any[]>([]);
  busquedaSalidas = signal('');
  bitMes = signal(new Date().getMonth() + 1);
  bitAnio = signal(new Date().getFullYear());
  busquedaBit = signal('');

  visitaSel = signal<RegistroBitacora | null>(null);
  confirmSalida = signal(false);

  bitMesLabel = computed(() =>
    new Date(this.bitAnio(), this.bitMes() - 1, 1).toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));
  bitStats = computed(() => {
    const l = this.bitacora().registros;
    return { visitas: l.length, completadas: l.filter((r) => r.egresoUtc).length, adentro: l.filter((r) => !r.egresoUtc).length };
  });
  bitVisibles = computed(() => {
    const q = this.busquedaBit().trim().toLowerCase();
    const l = q ? this.bitacora().registros.filter((r) =>
      r.visitanteNombre.toLowerCase().includes(q) || (r.patente ?? '').toLowerCase().includes(q) || r.unidad.toLowerCase().includes(q))
      : this.bitacora().registros;
    const grupos = new Map<string, RegistroBitacora[]>();
    for (const r of l) {
      const k = r.ingresoUtc.slice(0, 10);
      (grupos.get(k) ?? grupos.set(k, []).get(k)!).push(r);
    }
    const hoy = new Date().toISOString().slice(0, 10);
    return [...grupos.entries()].map(([k, items]) => ({
      label: k === hoy ? 'Hoy' : new Date(k + 'T12:00:00').toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }),
      items,
    }));
  });

  cambiarBitMes(delta: number): void {
    let m = this.bitMes() + delta, a = this.bitAnio();
    if (m < 1) { m = 12; a--; }
    if (m > 12) { m = 1; a++; }
    this.bitMes.set(m); this.bitAnio.set(a);
    this.cargarBitacora();
  }

  duracion(r: RegistroBitacora): string {
    if (!r.egresoUtc) return '—';
    const min = Math.max(0, Math.round((new Date(r.egresoUtc).getTime() - new Date(r.ingresoUtc).getTime()) / 60000));
    if (min < 60) return `${min} min`;
    return `${Math.floor(min / 60)} h ${min % 60} min`;
  }

  adentroFiltrado = computed(() => {
    const q = this.busquedaSalidas().trim().toLowerCase();
    if (!q) return this.adentro();
    return this.adentro().filter((r) =>
      r.visitanteNombre.toLowerCase().includes(q) ||
      (r.patente ?? '').toLowerCase().includes(q) ||
      r.unidad.toLowerCase().includes(q));
  });
  adentroPorVehiculo = computed(() => {
    const l = this.adentro();
    return {
      autos: l.filter((r) => r.vehiculo === 'Auto').length,
      motos: l.filter((r) => r.vehiculo === 'Motocicleta').length,
      aPie: l.filter((r) => r.vehiculo === 'SinVehiculo').length,
    };
  });

  tiempoAdentro(iso: string): string {
    const min = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 60000));
    if (min < 60) return `${min} min`;
    const h = Math.floor(min / 60);
    return h < 24 ? `${h} h ${min % 60} min` : `${Math.floor(h / 24)} d`;
  }

  nombre = computed(() => this.ctx()?.casetaNombre ?? 'Portería');

  // ================= paquetería =================
  transportistas = TRANSPORTISTAS;
  tiposPaquete: { value: TipoPaquete; label: string }[] = [
    { value: 'Paquete', label: 'Paquete' },
    { value: 'Correo', label: 'Correo / sobre' },
    { value: 'Otro', label: 'Otro' },
  ];

  pqResumen = signal<ResumenPaqueteria>({ porEntregar: 0, llegaronHoy: 0, entregadosHoy: 0 });
  pqPendientes = signal<Paquete[]>([]);
  pqRegistro = signal<Paquete[]>([]);
  cargandoPq = signal(false);
  pqMes = signal(new Date().getMonth() + 1);
  pqAnio = signal(new Date().getFullYear());
  busquedaPq = signal('');

  nuevoPq = signal<NuevoPaquete>(this.nuevoPqVacio());
  guardandoPq = signal(false);

  // entrega
  entregaUnidad = signal<string | null>(null);       // nombre de unidad
  pqSeleccionados = signal<Set<string>>(new Set());
  residentesUnidad = signal<{ nombre: string; rol: string }[]>([]);
  quienRecibe = signal('');
  entregando = signal(false);

  private nuevoPqVacio(): NuevoPaquete {
    const n = new Date();
    const p = (x: number) => String(x).padStart(2, '0');
    return {
      fecha: `${n.getFullYear()}-${p(n.getMonth() + 1)}-${p(n.getDate())}`,
      hora: `${p(n.getHours())}:${p(n.getMinutes())}`,
      tipo: 'Paquete', cantidad: 1, transportista: null, unidadId: null, descripcion: null, foto: null,
    };
  }
  setPq<K extends keyof NuevoPaquete>(k: K, v: NuevoPaquete[K]): void {
    this.nuevoPq.update((e) => ({ ...e, [k]: v }));
  }
  procesandoFoto = signal(false);
  async elegirFotoPq(ev: Event): Promise<void> {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.procesandoFoto.set(true);
    try {
      this.setPq('foto', await comprimirImagen(file));
    } catch {
      this.toasts.error('No se pudo procesar la foto.');
    } finally {
      this.procesandoFoto.set(false);
    }
  }
  nuevoPqValido = computed(() => {
    const p = this.nuevoPq();
    return !!p.unidadId && !!p.transportista && p.cantidad >= 1;
  });
  nuevoPqUnidadLabel = computed(() => {
    const id = this.nuevoPq().unidadId;
    return id ? (this.unidades().find((u) => u.id === id)?.nombre ?? 'Seleccionar') : 'Seleccionar';
  });

  pendientesVisible = computed(() => {
    const q = this.busquedaPq().trim().toLowerCase();
    if (!q) return this.pqPendientes();
    return this.pqPendientes().filter((p) =>
      p.unidadNombre.toLowerCase().includes(q)
      || (p.transportista ?? '').toLowerCase().includes(q)
      || (this.contactoDe(p.unidadNombre) ?? '').toLowerCase().includes(q));
  });
  contactoDe(unidadNombre: string): string | null {
    return this.unidades().find((u) => u.nombre === unidadNombre)?.contacto ?? null;
  }
  entregaItems = computed(() => this.pqPendientes().filter((p) => p.unidadNombre === this.entregaUnidad()));
  puedeEntregar = computed(() =>
    this.pqSeleccionados().size > 0 && this.quienRecibe().trim().length > 0 && !this.entregando());

  pqRegistroVisible = computed(() => {
    const q = this.busquedaPq().trim().toLowerCase();
    const l = q ? this.pqRegistro().filter((p) =>
      p.unidadNombre.toLowerCase().includes(q) || (p.transportista ?? '').toLowerCase().includes(q))
      : this.pqRegistro();
    const grupos = new Map<string, Paquete[]>();
    for (const p of l) {
      const k = p.llegadaUtc.slice(0, 10);
      (grupos.get(k) ?? grupos.set(k, []).get(k)!).push(p);
    }
    const hoy = new Date().toISOString().slice(0, 10);
    return [...grupos.entries()].map(([k, items]) => ({
      label: k === hoy ? 'Hoy' : new Date(k + 'T12:00:00').toLocaleDateString('es-AR', { weekday: 'long', day: 'numeric', month: 'long' }),
      items,
    }));
  });
  pqMesLabel = computed(() =>
    new Date(this.pqAnio(), this.pqMes() - 1, 1).toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));

  cargarPqResumen(): void {
    this.api.paqueteResumen().subscribe({ next: (r) => this.pqResumen.set(r), error: () => {} });
  }
  cargarPendientes(): void {
    this.cargandoPq.set(true);
    this.api.paquetes('EnRecepcion').subscribe({
      next: (r) => { this.pqPendientes.set(r.paquetes); this.cargandoPq.set(false); },
      error: () => { this.cargandoPq.set(false); this.toasts.error('No pudimos cargar los paquetes.'); },
    });
  }
  cargarRegistroPq(): void {
    this.cargandoPq.set(true);
    this.api.paquetes(undefined, '', this.pqAnio(), this.pqMes()).subscribe({
      next: (r) => { this.pqRegistro.set(r.paquetes); this.cargandoPq.set(false); },
      error: () => { this.cargandoPq.set(false); this.toasts.error('No pudimos cargar el registro.'); },
    });
  }
  cambiarPqMes(delta: number): void {
    let m = this.pqMes() + delta, a = this.pqAnio();
    if (m < 1) { m = 12; a--; }
    if (m > 12) { m = 1; a++; }
    this.pqMes.set(m); this.pqAnio.set(a);
    this.cargarRegistroPq();
  }

  crearPaquete(): void {
    if (!this.nuevoPqValido() || this.guardandoPq()) return;
    this.guardandoPq.set(true);
    const p = this.nuevoPq();
    const dto: RegistrarPaquete = {
      unidadId: p.unidadId!,
      tipo: p.tipo,
      cantidad: p.cantidad,
      transportista: p.transportista,
      descripcion: p.descripcion?.trim() || null,
      llegadaLocal: `${p.fecha}T${p.hora}:00`,
      fotoBase64: p.foto,
    };
    this.api.registrarPaquete(dto).subscribe({
      next: () => {
        this.guardandoPq.set(false);
        this.toasts.exito('Paquete registrado');
        this.cargarPqResumen();
        this.vista.set('paqueteria');
      },
      error: (e) => { this.guardandoPq.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo registrar.'); },
    });
  }

  abrirEntrega(unidadNombre: string): void {
    this.entregaUnidad.set(unidadNombre);
    this.pqSeleccionados.set(new Set(this.pqPendientes().filter((p) => p.unidadNombre === unidadNombre).map((p) => p.id)));
    this.quienRecibe.set('');
    this.residentesUnidad.set([]);
    const uid = this.pqPendientes().find((p) => p.unidadNombre === unidadNombre)?.unidadId;
    if (uid) this.api.unidad(uid).subscribe({ next: (u) => this.residentesUnidad.set(u.residentes), error: () => {} });
    this.vista.set('pq-entregar');
  }
  togglePq(id: string): void {
    this.pqSeleccionados.update((s) => {
      const n = new Set(s);
      n.has(id) ? n.delete(id) : n.add(id);
      return n;
    });
  }
  confirmarEntrega(): void {
    if (!this.puedeEntregar()) return;
    this.entregando.set(true);
    const ids = [...this.pqSeleccionados()];
    const quien = this.quienRecibe().trim() || null;
    let hechos = 0;
    for (const id of ids) {
      this.api.entregarPaquete(id, quien).subscribe({
        next: () => {
          if (++hechos === ids.length) {
            this.entregando.set(false);
            this.toasts.exito(ids.length === 1 ? 'Paquete entregado' : `${ids.length} paquetes entregados`);
            this.cargarPqResumen();
            this.cargarPendientes();
            this.vista.set('paqueteria');
          }
        },
        error: (e) => { this.entregando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo entregar.'); },
      });
    }
  }
  horaPq(iso: string): string {
    const d = new Date(iso);
    const hoy = new Date();
    const mismoDia = d.toDateString() === hoy.toDateString();
    const hh = d.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
    return mismoDia ? `Hoy · ${hh}` : `${d.toLocaleDateString('es-AR', { day: 'numeric', month: 'short' })} · ${hh}`;
  }
  labelTipoPq(t: TipoPaquete): string { return this.tiposPaquete.find((x) => x.value === t)?.label ?? t; }

  get ahoraHora(): string {
    return new Date().toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
  }

  constructor() {
    this.api.contexto().subscribe({
      next: (c) => this.ctx.set(c),
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cargar la caseta.'),
    });
    this.cargarResumen();
    this.cargarTurno();
    this.cargarAlertas();
    this.api.unidades().subscribe({ next: (u) => this.unidades.set(u), error: () => {} });
  }

  private emVacia(): EntradaManual {
    return { visitanteNombre: '', tipoVisita: 'Familia', vehiculo: 'SinVehiculo', patente: null, unidadId: null, nota: null, documento: null };
  }

  cargarResumen(): void {
    this.api.resumen().subscribe({ next: (r) => this.resumen.set(r), error: () => {} });
  }

  ir(v: Vista): void {
    this.pararCamara();
    this.vista.set(v);
    if (v === 'escanear') this.escanear();
    if (v === 'salidas') this.cargarAdentro();
    if (v === 'bitacora') this.cargarBitacora();
    if (v === 'manual') this.em.set(this.emVacia());
    if (v === 'inicio') { this.cargarResumen(); this.cargarTurno(); this.cargarAlertas(); }
    if (v === 'entradas') this.cargarResumen();
    if (v === 'paqueteria') this.cargarPqResumen();
    if (v === 'pq-nuevo') this.nuevoPq.set(this.nuevoPqVacio());
    if (v === 'pq-entregar') { this.entregaUnidad.set(null); this.cargarPendientes(); }
    if (v === 'pq-registro') this.cargarRegistroPq();
  }

  volver(): void {
    if (this.vista() === 'pq-entregar' && this.entregaUnidad()) { this.entregaUnidad.set(null); return; }
    this.ir(this.volverA());
  }

  // ---- escáner ----
  async escanear(): Promise<void> {
    this.resultado.set(null);
    this.preview.set(null);
    this.docVisitante.set('');
    this.patenteVisitante.set('');
    this.ultimoToken = '';
    if (!this.soportaCamara()) { this.escaneando.set(true); return; }
    this.escaneando.set(true);
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
      const v = this.video()?.nativeElement;
      if (v) { v.srcObject = this.stream; await v.play(); }
      this.detector = new (window as any).BarcodeDetector({ formats: ['qr_code'] });
      this.loop();
    } catch {
      this.toasts.error('No pudimos abrir la cámara. Ingresá el código a mano.');
      this.pararCamara();
      this.soportaCamara.set(false);
    }
  }

  private loop = async (): Promise<void> => {
    const v = this.video()?.nativeElement;
    if (!v || !this.escaneando()) return;
    try {
      const codes = await this.detector.detect(v);
      if (codes.length && codes[0].rawValue && codes[0].rawValue !== this.ultimoToken) {
        this.ultimoToken = codes[0].rawValue;
        this.verificar(codes[0].rawValue);
        return;
      }
    } catch { /* frame no listo */ }
    this.raf = requestAnimationFrame(this.loop);
  };

  verificarManual(): void {
    const t = this.manualToken().trim();
    if (t) this.verificar(t);
  }

  private verificar(token: string): void {
    this.pararCamara();
    this.escaneando.set(false);
    this.verificando.set(true);
    this.api.verificar(token).subscribe({
      next: (r) => {
        this.verificando.set(false);
        this.patenteVisitante.set(r.patente ?? '');
        if (r.valido) this.preview.set(r);
        else this.resultado.set(r);
      },
      error: (e) => {
        this.toasts.error(e?.error?.message ?? 'No se pudo verificar el código.');
        this.verificando.set(false);
        this.vista.set('entradas');
      },
    });
  }

  pidePatente = computed(() => this.preview()?.vehiculo !== 'SinVehiculo');
  puedeConfirmar = computed(() => {
    if (this.docVisitante().trim().length < 4) return false;
    if (this.pidePatente() && this.patenteVisitante().trim().length < 3) return false;
    return true;
  });

  confirmarEntrada(): void {
    const p = this.preview();
    if (!p || !this.puedeConfirmar() || this.confirmando()) return;
    this.confirmando.set(true);
    this.api.confirmarIngreso(p.token, this.docVisitante().trim(), this.patenteVisitante().trim() || null).subscribe({
      next: (r) => { this.confirmando.set(false); this.preview.set(null); this.resultado.set(r); this.cargarResumen(); },
      error: (e) => { this.confirmando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo confirmar el ingreso.'); },
    });
  }

  otroEscaneo(): void {
    this.resultado.set(null);
    this.preview.set(null);
    this.manualToken.set('');
    this.docVisitante.set('');
    this.patenteVisitante.set('');
    this.escanear();
  }

  // ---- entrada manual ----
  setEm<K extends keyof EntradaManual>(k: K, v: EntradaManual[K]): void {
    this.em.update((e) => ({ ...e, [k]: v }));
  }
  emValida = computed(() => this.em().visitanteNombre.trim().length > 0);

  registrarManual(): void {
    if (!this.emValida() || this.guardandoEm()) return;
    this.guardandoEm.set(true);
    const e = this.em();
    this.api.entradaManual({
      ...e,
      visitanteNombre: e.visitanteNombre.trim(),
      patente: e.vehiculo === 'SinVehiculo' ? null : (e.patente?.trim() || null),
      unidadId: e.unidadId || null,
      documento: e.documento?.trim() || null,
    }).subscribe({
      next: () => {
        this.guardandoEm.set(false);
        this.toasts.exito('Entrada registrada');
        this.cargarResumen();
        this.vista.set('entradas');
      },
      error: (err) => { this.guardandoEm.set(false); this.toasts.error(err?.error?.message ?? 'No se pudo registrar.'); },
    });
  }

  // ---- salidas ----
  cargarAdentro(): void {
    this.cargandoLista.set(true);
    this.api.adentro().subscribe({
      next: (l) => { this.adentro.set(l); this.cargandoLista.set(false); },
      error: () => { this.cargandoLista.set(false); this.toasts.error('No pudimos cargar la lista.'); },
    });
  }
  registrarSalida(r: RegistroBitacora): void {
    this.api.salida(r.id).subscribe({
      next: () => {
        this.toasts.exito('Salida registrada');
        this.confirmSalida.set(false);
        this.visitaSel.set(null);
        this.cargarAdentro();
        this.cargarResumen();
      },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo registrar.'),
    });
  }

  // ---- bitácora ----
  cargarBitacora(): void {
    this.cargandoLista.set(true);
    this.api.bitacora(this.bitAnio(), this.bitMes()).subscribe({
      next: (b) => { this.bitacora.set(b); this.cargandoLista.set(false); },
      error: () => { this.cargandoLista.set(false); this.toasts.error('No pudimos cargar la bitácora.'); },
    });
  }

  // ---- alertas ----
  cargarAlertas(): void {
    this.api.alertas().subscribe({
      next: (r) => this.alertas.set(r.anuncios ?? []),
      error: () => this.alertas.set([]),
    });
  }

  iconoVehiculo(v: string): string { return TIPOS_VEHICULO.find((x) => x.value === v)?.icon ?? ''; }

  private pararCamara(): void {
    cancelAnimationFrame(this.raf);
    this.escaneando.set(false);
    this.stream?.getTracks().forEach((t) => t.stop());
    this.stream = null;
  }

  salir(): void {
    this.pararCamara();
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void { this.pararCamara(); }
}
