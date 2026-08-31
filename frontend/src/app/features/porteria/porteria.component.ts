import { Component, ElementRef, OnDestroy, computed, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import {
  Bitacora, EntradaManual, PorteriaContexto, PorteriaService, RegistroBitacora,
  ResumenAcceso, UnidadRef, Verificacion,
} from '../../core/services/porteria.service';
import { ICON_VISITA, LABEL_VISITA, TIPOS_VEHICULO, TIPOS_VISITA } from '../../core/models/pase-acceso.models';

type Vista = 'home' | 'escanear' | 'manual' | 'salidas' | 'bitacora' | 'alertas';

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

  vista = signal<Vista>('home');
  ctx = signal<PorteriaContexto | null>(null);
  resumen = signal<ResumenAcceso>({ adentroAhora: 0, entradasHoy: 0, salidasHoy: 0 });

  // escáner
  escaneando = signal(false);
  verificando = signal(false);
  resultado = signal<Verificacion | null>(null);
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

  // salidas / bitácora
  adentro = signal<RegistroBitacora[]>([]);
  bitacora = signal<Bitacora>({ registros: [], adentroAhora: 0 });
  cargandoLista = signal(false);
  alertas = signal<any[]>([]);

  nombre = computed(() => this.ctx()?.casetaNombre ?? 'Portería');

  constructor() {
    this.api.contexto().subscribe({
      next: (c) => this.ctx.set(c),
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cargar la caseta.'),
    });
    this.cargarResumen();
    this.api.unidades().subscribe({ next: (u) => this.unidades.set(u), error: () => {} });
  }

  private emVacia(): EntradaManual {
    return { visitanteNombre: '', tipoVisita: 'Familia', vehiculo: 'SinVehiculo', patente: null, unidadId: null, nota: null };
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
    if (v === 'alertas') this.cargarAlertas();
    if (v === 'manual') this.em.set(this.emVacia());
    if (v === 'home') this.cargarResumen();
  }

  // ---- escáner ----
  async escanear(): Promise<void> {
    this.resultado.set(null);
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
      next: (r) => { this.resultado.set(r); this.verificando.set(false); this.cargarResumen(); },
      error: (e) => {
        this.toasts.error(e?.error?.message ?? 'No se pudo verificar el código.');
        this.verificando.set(false);
        this.vista.set('home');
      },
    });
  }

  otroEscaneo(): void {
    this.resultado.set(null);
    this.manualToken.set('');
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
    }).subscribe({
      next: () => {
        this.guardandoEm.set(false);
        this.toasts.exito('Entrada registrada');
        this.cargarResumen();
        this.vista.set('home');
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
      next: () => { this.toasts.exito('Salida registrada'); this.cargarAdentro(); this.cargarResumen(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo registrar.'),
    });
  }

  // ---- bitácora ----
  cargarBitacora(): void {
    this.cargandoLista.set(true);
    this.api.bitacora(7).subscribe({
      next: (b) => { this.bitacora.set(b); this.cargandoLista.set(false); },
      error: () => { this.cargandoLista.set(false); this.toasts.error('No pudimos cargar la bitácora.'); },
    });
  }

  // ---- alertas ----
  cargarAlertas(): void {
    this.cargandoLista.set(true);
    this.api.alertas().subscribe({
      next: (r) => { this.alertas.set(r.anuncios ?? []); this.cargandoLista.set(false); },
      error: () => { this.cargandoLista.set(false); this.alertas.set([]); },
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
