import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaseAccesoService } from '../../core/services/pase-acceso.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CrearPase, ICON_VISITA, LABEL_PASE, LABEL_VISITA, PaseAcceso,
  TIPOS_PASE, TIPOS_VEHICULO, TIPOS_VISITA, TipoPase, TipoVehiculo, TipoVisita,
} from '../../core/models/pase-acceso.models';

@Component({
  selector: 'app-portal-qr',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './portal-qr.component.html',
  styleUrl: './portal-qr.component.scss',
})
export class PortalQrComponent {
  private api = inject(PaseAccesoService);
  private toasts = inject(ToastService);

  tiposPase = TIPOS_PASE;
  tiposVisita = TIPOS_VISITA;
  tiposVehiculo = TIPOS_VEHICULO;
  labelVisita = LABEL_VISITA;
  iconVisita = ICON_VISITA;
  labelPase = LABEL_PASE;

  vista = signal<'lista' | 'nuevo' | 'detalle'>('lista');
  cargando = signal(true);
  pases = signal<PaseAcceso[]>([]);
  tab = signal<'activos' | 'expirados'>('activos');
  seleccion = signal<PaseAcceso | null>(null);
  generando = signal(false);

  form = signal<CrearPase & { tipoPase: TipoPase; tipoVisita: TipoVisita; vehiculo: TipoVehiculo }>({
    tipoPase: 'UnaEntrada',
    tipoVisita: 'Familia',
    vehiculo: 'SinVehiculo',
    visitanteNombre: '',
    patente: '',
    fechaEntrada: new Date().toISOString().slice(0, 10),
    validoHasta: null,
  });

  activos = computed(() =>
    this.pases().filter((p) => p.estado === 'Activo' || p.estado === 'Usado'));
  expirados = computed(() =>
    this.pases().filter((p) => p.estado === 'Vencido' || p.estado === 'Revocado'));

  grupos = computed(() => {
    const lista = this.tab() === 'activos' ? this.activos() : this.expirados();
    const map = new Map<string, PaseAcceso[]>();
    for (const p of lista) {
      const k = p.fechaEntrada.slice(0, 10);
      (map.get(k) ?? map.set(k, []).get(k)!).push(p);
    }
    return [...map.entries()].map(([fecha, items]) => ({ fecha, items }));
  });

  formConVehiculo = computed(() => this.form().vehiculo !== 'SinVehiculo');
  formValido = computed(() => this.form().visitanteNombre.trim().length > 0);

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.mis().subscribe({
      next: (l) => { this.pases.set(l); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar tus QR.'); this.cargando.set(false); },
    });
  }

  setForm<K extends keyof CrearPase>(campo: K, valor: CrearPase[K]): void {
    this.form.update((f) => ({ ...f, [campo]: valor }));
  }

  nuevo(): void {
    this.form.set({
      tipoPase: 'UnaEntrada', tipoVisita: 'Familia', vehiculo: 'SinVehiculo',
      visitanteNombre: '', patente: '', fechaEntrada: new Date().toISOString().slice(0, 10), validoHasta: null,
    });
    this.vista.set('nuevo');
  }

  generar(): void {
    if (!this.formValido() || this.generando()) return;
    this.generando.set(true);
    const f = this.form();
    const body: CrearPase = {
      tipoPase: f.tipoPase,
      tipoVisita: f.tipoVisita,
      vehiculo: f.vehiculo,
      visitanteNombre: f.visitanteNombre.trim(),
      patente: f.vehiculo !== 'SinVehiculo' && f.patente ? f.patente.trim() : null,
      fechaEntrada: f.fechaEntrada,
      validoHasta: f.tipoPase === 'UnaEntrada' ? null : (f.validoHasta || null),
    };
    this.api.crear(body).subscribe({
      next: (p) => {
        this.generando.set(false);
        this.pases.update((l) => [p, ...l]);
        this.seleccion.set(p);
        this.vista.set('detalle');
      },
      error: (e) => {
        this.generando.set(false);
        this.toasts.error(e?.error?.message ?? 'No se pudo generar el QR.');
      },
    });
  }

  abrir(p: PaseAcceso): void {
    this.seleccion.set(p);
    this.vista.set('detalle');
  }

  cerrarDetalle(): void {
    this.seleccion.set(null);
    this.vista.set('lista');
  }

  eliminar(): void {
    const p = this.seleccion();
    if (!p) return;
    this.api.revocar(p.id).subscribe({
      next: () => {
        this.pases.update((l) => l.map((x) => x.id === p.id ? { ...x, estado: 'Revocado' } : x));
        this.toasts.exito('QR eliminado');
        this.cerrarDetalle();
      },
      error: () => this.toasts.error('No se pudo eliminar.'),
    });
  }

  private pngFile(p: PaseAcceso): File {
    const bin = atob(p.qrPngBase64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    return new File([bytes], `qr-${p.visitanteNombre.replace(/\s+/g, '-').toLowerCase()}.png`, { type: 'image/png' });
  }

  async compartir(): Promise<void> {
    const p = this.seleccion();
    if (!p) return;
    const file = this.pngFile(p);
    const nav = navigator as Navigator & { canShare?: (d: ShareData) => boolean };
    const texto = `Acceso a ${p.consorcioNombre} — ${p.visitanteNombre} (${this.labelVisita[p.tipoVisita]})`;
    try {
      if (nav.canShare && nav.canShare({ files: [file] })) {
        await nav.share({ files: [file], title: 'QR de acceso', text: texto });
        return;
      }
      if (navigator.share) {
        await navigator.share({ title: 'QR de acceso', text: texto });
        return;
      }
    } catch { /* usuario canceló */ return; }
    // fallback: descargar la imagen
    const url = URL.createObjectURL(file);
    const a = document.createElement('a');
    a.href = url; a.download = file.name; a.click();
    URL.revokeObjectURL(url);
  }
}
