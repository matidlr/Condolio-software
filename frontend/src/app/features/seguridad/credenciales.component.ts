import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { CredencialCaseta, CredencialGenerada, PersonalService } from '../../core/services/personal.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-credenciales-caseta',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './credenciales.component.html',
  styleUrl: './credenciales.component.scss',
  host: { '(document:click)': 'menu.set(null)' },
})
export class CredencialesCasetaComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(PersonalService);
  private toasts = inject(ToastService);

  cargando = signal(true);
  dispositivos = signal<CredencialCaseta[]>([]);
  total = signal(0);
  activos = signal(0);
  ultimoUtc = signal<string | null>(null);
  ultimoNombre = signal<string | null>(null);

  menu = signal<string | null>(null);
  masInfo = signal(false);
  agregarAbierto = signal(false);
  nuevoNombre = signal('');
  guardando = signal(false);

  generada = signal<CredencialGenerada | null>(null);
  detalle = signal<CredencialCaseta | null>(null);
  editandoNombre = signal(false);
  confirmReset = signal(false);
  confirmEliminar = signal<CredencialCaseta | null>(null);

  private cid = computed(() => this.consorcios.activoId());

  constructor() {
    effect(() => { if (this.cid()) this.cargar(); });
  }

  private cargar(): void {
    const cid = this.cid();
    if (!cid) return;
    this.cargando.set(true);
    this.api.credenciales(cid).subscribe({
      next: (l) => {
        this.dispositivos.set(l.dispositivos);
        this.total.set(l.total);
        this.activos.set(l.activos);
        this.ultimoUtc.set(l.ultimoAgregadoUtc ?? null);
        this.ultimoNombre.set(l.ultimoAgregadoNombre ?? null);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar las credenciales.'); this.cargando.set(false); },
    });
  }

  hace(iso: string | null): string {
    if (!iso) return '—';
    const s = Math.max(1, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
    if (s < 60) return 'recién';
    if (s < 3600) return `hace ${Math.floor(s / 60)} minutos`;
    if (s < 86400) return `hace ${Math.floor(s / 3600)} horas`;
    return `hace ${Math.floor(s / 86400)} día(s)`;
  }
  esNuevo(iso: string): boolean {
    return Date.now() - new Date(iso).getTime() < 24 * 3600 * 1000;
  }

  abrirAgregar(): void { this.nuevoNombre.set(''); this.agregarAbierto.set(true); }

  crear(): void {
    const cid = this.cid();
    const n = this.nuevoNombre().trim();
    if (!cid || n.length < 2 || this.guardando()) return;
    this.guardando.set(true);
    this.api.crearCredencial(cid, n).subscribe({
      next: (g) => {
        this.guardando.set(false);
        this.agregarAbierto.set(false);
        this.generada.set(g);
        this.cargar();
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo crear el dispositivo.'); },
    });
  }

  abrirDetalle(d: CredencialCaseta): void {
    this.detalle.set(d);
    this.editandoNombre.set(false);
    this.nuevoNombre.set(d.nombre);
  }

  guardarNombre(): void {
    const cid = this.cid();
    const d = this.detalle();
    const n = this.nuevoNombre().trim();
    if (!cid || !d || n.length < 2) return;
    this.api.renombrarCredencial(cid, d.id, n).subscribe({
      next: () => {
        this.toasts.exito('Nombre actualizado');
        this.detalle.set({ ...d, nombre: n });
        this.editandoNombre.set(false);
        this.cargar();
      },
      error: () => this.toasts.error('No se pudo renombrar.'),
    });
  }

  restablecer(): void {
    const cid = this.cid();
    const d = this.detalle();
    if (!cid || !d) return;
    this.api.restablecerCredencial(cid, d.id).subscribe({
      next: (g) => { this.confirmReset.set(false); this.detalle.set(null); this.generada.set(g); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo restablecer.'),
    });
  }

  eliminar(d: CredencialCaseta): void {
    const cid = this.cid();
    if (!cid) return;
    this.api.eliminarCredencial(cid, d.id).subscribe({
      next: () => { this.toasts.exito('Dispositivo eliminado'); this.confirmEliminar.set(null); this.detalle.set(null); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  copiar(txt: string, msg = 'Copiado'): void {
    navigator.clipboard?.writeText(txt).then(() => this.toasts.exito(msg), () => {});
  }
  copiarAmbas(g: CredencialGenerada): void {
    this.copiar(`Email: ${g.email}\nContraseña: ${g.password}`, 'Credenciales copiadas');
  }
}
