import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import {
  GuardarPersonal, LABEL_TIPO_PERSONAL, MiembroPersonal, PersonalCreado,
  PersonalService, TIPOS_PERSONAL, TipoPersonal,
} from '../../core/services/personal.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './staff.component.html',
  styleUrl: './staff.component.scss',
  host: { '(document:click)': 'menu.set(null)' },
})
export class StaffComponent {
  private consorcios = inject(ConsorcioService);
  private api = inject(PersonalService);
  private toasts = inject(ToastService);

  tipos = TIPOS_PERSONAL;
  labelTipo = LABEL_TIPO_PERSONAL;

  cargando = signal(true);
  miembros = signal<MiembroPersonal[]>([]);
  total = signal(0);
  seguridad = signal(0);
  conAcceso = signal(0);
  busqueda = signal('');
  menu = signal<string | null>(null);

  form = signal<(GuardarPersonal & { id?: string; crearCuenta: boolean }) | null>(null);
  guardando = signal(false);
  generado = signal<PersonalCreado | null>(null);
  detalle = signal<MiembroPersonal | null>(null);
  confirmEliminar = signal<MiembroPersonal | null>(null);

  private cid = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    if (!q) return this.miembros();
    return this.miembros().filter((m) => (m.nombre + ' ' + m.apellido).toLowerCase().includes(q));
  });

  constructor() {
    effect(() => { if (this.cid()) this.cargar(); });
  }

  private cargar(): void {
    const cid = this.cid();
    if (!cid) return;
    this.cargando.set(true);
    this.api.listar(cid).subscribe({
      next: (l) => {
        this.miembros.set(l.miembros);
        this.total.set(l.total);
        this.seguridad.set(l.seguridad);
        this.conAcceso.set(l.conAcceso);
        this.cargando.set(false);
      },
      error: () => { this.toasts.error('No pudimos cargar el personal.'); this.cargando.set(false); },
    });
  }

  iniciales(m: MiembroPersonal): string {
    return ((m.nombre[0] ?? '') + (m.apellido[0] ?? '')).toUpperCase() || '?';
  }

  nuevo(): void {
    this.form.set({ nombre: '', apellido: '', tipo: 'Seguridad', emailCuenta: null, crearCuenta: false });
  }
  editar(m: MiembroPersonal): void {
    this.detalle.set(null);
    this.form.set({ id: m.id, nombre: m.nombre, apellido: m.apellido, tipo: m.tipo, emailCuenta: null, crearCuenta: false });
  }
  setForm<K extends keyof (GuardarPersonal & { crearCuenta: boolean })>(k: K, v: any): void {
    this.form.update((f) => f && { ...f, [k]: v });
  }
  formValido = computed(() => {
    const f = this.form();
    if (!f) return false;
    if (f.nombre.trim().length === 0) return false;
    if (f.crearCuenta && !/.+@.+\..+/.test(f.emailCuenta ?? '')) return false;
    return true;
  });

  guardar(): void {
    const cid = this.cid();
    const f = this.form();
    if (!cid || !f || !this.formValido() || this.guardando()) return;
    this.guardando.set(true);
    const body: GuardarPersonal = {
      nombre: f.nombre.trim(), apellido: f.apellido.trim(), tipo: f.tipo,
      emailCuenta: f.crearCuenta ? (f.emailCuenta ?? '').trim() : null,
    };
    const obs: Observable<PersonalCreado | MiembroPersonal> =
      f.id ? this.api.actualizar(cid, f.id, body) : this.api.crear(cid, body);
    obs.subscribe({
      next: (r: PersonalCreado | MiembroPersonal) => {
        this.guardando.set(false);
        this.form.set(null);
        const creado = r as PersonalCreado;
        if (!f.id && creado.passwordTemporal) this.generado.set(creado);
        else this.toasts.exito(f.id ? 'Personal actualizado' : 'Personal agregado');
        this.cargar();
      },
      error: (e: any) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  eliminar(m: MiembroPersonal): void {
    const cid = this.cid();
    if (!cid) return;
    this.api.eliminar(cid, m.id).subscribe({
      next: () => { this.toasts.exito('Personal eliminado'); this.confirmEliminar.set(null); this.detalle.set(null); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  copiar(txt: string): void {
    navigator.clipboard?.writeText(txt).then(() => this.toasts.exito('Copiado'), () => {});
  }
}
