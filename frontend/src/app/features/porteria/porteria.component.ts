import { Component, ElementRef, OnDestroy, computed, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Bitacora, EntradaManual, PersonalTurno, PorteriaContexto, PorteriaService, RegistroBitacora,
  ResumenAcceso, TurnoActual, UnidadDetalle, UnidadRef, Verificacion,
} from '../../core/services/porteria.service';
import { LABEL_TIPO_PERSONAL, TipoPersonal, TIPOS_PERSONAL } from '../../core/services/personal.service';
import { ICON_VISITA, LABEL_VISITA, TIPOS_VEHICULO, TIPOS_VISITA } from '../../core/models/pase-acceso.models';

type Vista =
  | 'inicio' | 'entradas' | 'escanear' | 'manual' | 'salidas' | 'bitacora'
  | 'paqueteria' | 'unidades' | 'config';
type Tab = 'inicio' | 'entradas' | 'paqueteria' | 'unidades' | 'config';

const LETRAS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'.split('');
const NUMEROS = ['1', '2', '3', '4', '5', '6', '7', '8', '9', 'del', '0', 'clear'];

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

  tabActiva = computed<Tab>(() => {
    const v = this.vista();
    if (v === 'entradas' || v === 'escanear' || v === 'manual' || v === 'salidas' || v === 'bitacora') return 'entradas';
    return v as Tab;
  });
  esSubvista = computed(() =>
    ['escanear', 'manual', 'salidas', 'bitacora'].includes(this.vista()));

  tituloVista = computed(() => {
    switch (this.vista()) {
      case 'inicio': return 'Inicio';
      case 'entradas': return 'Entradas';
      case 'escanear': return 'Escanear QR';
      case 'manual': return 'Nueva entrada';
      case 'salidas': return 'Registrar salidas';
      case 'bitacora': return 'Bitácora digital';
      case 'paqueteria': return 'Paquetería';
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
