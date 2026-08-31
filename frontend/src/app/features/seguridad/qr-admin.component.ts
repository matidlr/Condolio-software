import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { AccesoAdminService, CrearPaseAdmin, PaseAdmin } from '../../core/services/acceso-admin.service';
import { ToastService } from '../../core/services/toast.service';
import { PaseAcceso } from '../../core/models/pase-acceso.models';
import {
  ICON_VISITA, LABEL_PASE, LABEL_VISITA, TIPOS_VEHICULO, TIPOS_VISITA,
} from '../../core/models/pase-acceso.models';
import { Unidad } from '../../core/models/consorcio.models';

interface FormPase {
  tipoVisita: string;
  vehiculo: string;
  patente: string;
  tipoPase: 'UnaEntrada' | 'Temporal';
  fechaEntrada: string;
  validoHasta: string;
  visitanteNombre: string;
  unidadId: string;
}

@Component({
  selector: 'app-qr-admin',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './qr-admin.component.html',
  styleUrl: './qr-admin.component.scss',
})
export class QrAdminComponent {
  private consorcios = inject(ConsorcioService);
  private unidades = inject(UnidadService);
  private api = inject(AccesoAdminService);
  private toasts = inject(ToastService);

  tiposVisita = TIPOS_VISITA;
  tiposVehiculo = TIPOS_VEHICULO;
  labelVisita = LABEL_VISITA;
  iconVisita = ICON_VISITA;
  labelPase = LABEL_PASE;

  cargando = signal(true);
  pases = signal<PaseAdmin[]>([]);
  activos = signal(0);
  generadosRango = signal(0);
  escaneadosHoy = signal(0);

  mes = signal(new Date().getMonth() + 1);
  anio = signal(new Date().getFullYear());
  busqueda = signal('');

  unidadesLista = signal<Unidad[]>([]);
  filtroUnidad = signal('');

  form = signal<FormPase | null>(null);
  guardando = signal(false);
  resultado = signal<PaseAcceso | null>(null);
  detalle = signal<PaseAcceso | null>(null);

  private cid = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    if (!q) return this.pases();
    return this.pases().filter((p) =>
      p.visitanteNombre.toLowerCase().includes(q) ||
      p.destino.toLowerCase().includes(q) ||
      p.creadoPor.toLowerCase().includes(q));
  });

  mesLabel = computed(() =>
    new Date(this.anio(), this.mes() - 1, 1).toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));

  unidadesFiltradas = computed(() => {
    const q = this.filtroUnidad().trim().toLowerCase();
    return this.unidadesLista().filter((u) => !q || u.nombre.toLowerCase().includes(q));
  });

  constructor() {
    effect(() => { if (this.cid()) this.cargar(); });
    effect(() => {
      const cid = this.cid();
      if (cid) this.unidades.listar(cid).subscribe((l) => this.unidadesLista.set(l));
    });
  }

  private cargar(): void {
    const cid = this.cid();
    if (!cid) return;
    this.cargando.set(true);
    this.api.listarPases(cid, this.anio(), this.mes(), '').subscribe({
      next: (l) => {
        this.pases.set(l.pases);
        this.activos.set(l.activos);
        this.generadosRango.set(l.generadosEnRango);
        this.escaneadosHoy.set(l.escaneadosHoy);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar los códigos QR.'); this.cargando.set(false); },
    });
  }

  cambiarMes(delta: number): void {
    let m = this.mes() + delta;
    let a = this.anio();
    if (m < 1) { m = 12; a--; }
    if (m > 12) { m = 1; a++; }
    this.mes.set(m); this.anio.set(a);
    this.cargar();
  }

  iconoVehiculo(v: string): string { return TIPOS_VEHICULO.find((x) => x.value === v)?.icon ?? ''; }
  estadoColor(e: string): string {
    return { Activo: '#16a34a', Usado: '#64748b', Vencido: '#d97706', Revocado: '#dc2626' }[e] ?? '#64748b';
  }

  // ---- crear ----
  nuevo(): void {
    const n = new Date();
    this.form.set({
      tipoVisita: 'Empleado', vehiculo: 'SinVehiculo', patente: '',
      tipoPase: 'UnaEntrada',
      fechaEntrada: `${n.getFullYear()}-${String(n.getMonth() + 1).padStart(2, '0')}-${String(n.getDate()).padStart(2, '0')}`,
      validoHasta: '', visitanteNombre: '', unidadId: '',
    });
  }
  setForm<K extends keyof FormPase>(k: K, v: FormPase[K]): void {
    this.form.update((f) => f && { ...f, [k]: v });
  }
  formValido = computed(() => (this.form()?.visitanteNombre.trim().length ?? 0) > 0);

  crear(): void {
    const cid = this.cid();
    const f = this.form();
    if (!cid || !f || !this.formValido() || this.guardando()) return;
    this.guardando.set(true);
    const body: CrearPaseAdmin = {
      unidadId: f.unidadId || null,
      tipoPase: f.tipoPase,
      tipoVisita: f.tipoVisita as any,
      vehiculo: f.vehiculo as any,
      patente: f.vehiculo === 'SinVehiculo' ? null : (f.patente.trim() || null),
      visitanteNombre: f.visitanteNombre.trim(),
      fechaEntrada: f.fechaEntrada,
      validoHasta: f.tipoPase === 'Temporal' && f.validoHasta ? f.validoHasta : null,
    };
    this.api.crearPase(cid, body).subscribe({
      next: (p) => {
        this.guardando.set(false);
        this.form.set(null);
        this.resultado.set(p);
        this.cargar();
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo generar el QR.'); },
    });
  }

  // ---- ver detalle ----
  abrir(p: PaseAdmin): void {
    const cid = this.cid();
    if (!cid) return;
    this.api.obtenerPase(cid, p.id).subscribe({
      next: (d) => this.detalle.set(d),
      error: () => this.toasts.error('No se pudo abrir el pase.'),
    });
  }

  revocar(p: PaseAdmin | PaseAcceso): void {
    const cid = this.cid();
    if (!cid) return;
    this.api.revocarPase(cid, p.id).subscribe({
      next: () => { this.toasts.exito('Pase revocado'); this.detalle.set(null); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo revocar.'),
    });
  }

  // ---- QR image helpers ----
  private pngFile(p: PaseAcceso): File {
    const bin = atob(p.qrPngBase64);
    const arr = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
    return new File([arr], `qr-${p.visitanteNombre}.png`, { type: 'image/png' });
  }
  descargar(p: PaseAcceso): void {
    const url = URL.createObjectURL(this.pngFile(p));
    const a = document.createElement('a');
    a.href = url;
    a.download = `qr-${p.visitanteNombre.replace(/[^\w-]+/g, '') || 'acceso'}.png`;
    a.click();
    URL.revokeObjectURL(url);
  }
  async copiar(p: PaseAcceso): Promise<void> {
    try {
      const blob = new Blob([await this.pngFile(p).arrayBuffer()], { type: 'image/png' });
      await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
      this.toasts.exito('Imagen copiada');
    } catch {
      this.descargar(p);
    }
  }
}
