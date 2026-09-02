import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ToastService } from '../../core/services/toast.service';
import {
  CATEGORIAS_PROVEEDOR, ExpensasService, GuardarProveedor, Proveedor, ProveedoresLista,
} from '../../core/services/expensas.service';

type FiltroEstado = 'todos' | 'activo' | 'inactivo';

@Component({
  selector: 'app-ex-proveedores',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './proveedores.component.html',
  styleUrl: './expensas.shared.scss',
})
export class ProveedoresComponent {
  private api = inject(ExpensasService);
  private consorcios = inject(ConsorcioService);
  private toasts = inject(ToastService);

  private cid = computed(() => this.consorcios.activoId());
  categorias = CATEGORIAS_PROVEEDOR;

  data = signal<ProveedoresLista | null>(null);
  cargando = signal(true);
  busqueda = signal('');
  fCategoria = signal('');
  fEstado = signal<FiltroEstado>('todos');

  modal = signal(false);
  editId = signal<string | null>(null);
  form = signal<GuardarProveedor>(this.vacio());
  guardando = signal(false);

  constructor() {
    effect(() => { const c = this.cid(); if (c) this.cargar(c); });
  }

  private vacio(): GuardarProveedor {
    return {
      nombre: '', empresa: '', rubro: 'Mantenimiento', cuit: '', email: '', telefono: '',
      telefonoAlt: '', direccion: '', sitioWeb: '', cbu: '', alias: '', horario: '', notas: '',
      recomendado: false,
    };
  }
  set<K extends keyof GuardarProveedor>(k: K, v: GuardarProveedor[K]): void {
    this.form.update((f) => ({ ...f, [k]: v }));
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.api.proveedores(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.cargando.set(false); this.toasts.error('No pudimos cargar los proveedores.'); },
    });
  }
  refrescar(): void { const c = this.cid(); if (c) this.cargar(c); }

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const cat = this.fCategoria();
    const est = this.fEstado();
    return (this.data()?.proveedores ?? [])
      .filter((p) => !cat || p.rubro === cat)
      .filter((p) => est === 'todos' || (est === 'activo' ? p.activo : !p.activo))
      .filter((p) => !q
        || p.nombre.toLowerCase().includes(q)
        || (p.empresa ?? '').toLowerCase().includes(q)
        || (p.rubro ?? '').toLowerCase().includes(q));
  });

  abrirNuevo(): void { this.editId.set(null); this.form.set(this.vacio()); this.modal.set(true); }
  abrirEditar(p: Proveedor): void {
    this.editId.set(p.id);
    this.form.set({
      nombre: p.nombre, empresa: p.empresa ?? '', rubro: p.rubro ?? 'Otro', cuit: p.cuit ?? '',
      email: p.email ?? '', telefono: p.telefono ?? '', telefonoAlt: p.telefonoAlt ?? '',
      direccion: p.direccion ?? '', sitioWeb: p.sitioWeb ?? '', cbu: p.cbu ?? '', alias: p.alias ?? '',
      horario: p.horario ?? '', notas: p.notas ?? '', recomendado: p.recomendado,
    });
    this.modal.set(true);
  }

  guardar(): void {
    const cid = this.cid();
    const f = this.form();
    if (!cid || !f.nombre.trim() || !f.telefono?.trim() || this.guardando()) return;
    this.guardando.set(true);
    const id = this.editId();
    const obs = id ? this.api.actualizarProveedor(cid, id, f) : this.api.crearProveedor(cid, f);
    obs.subscribe({
      next: () => { this.guardando.set(false); this.modal.set(false); this.toasts.exito(id ? 'Proveedor actualizado' : 'Proveedor agregado'); this.cargar(cid); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  toggleActivo(p: Proveedor): void {
    const cid = this.cid(); if (!cid) return;
    this.api.estadoProveedor(cid, p.id, !p.activo).subscribe({
      next: () => this.cargar(cid),
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cambiar el estado.'),
    });
  }
  toggleRecomendado(p: Proveedor): void {
    const cid = this.cid(); if (!cid) return;
    this.api.recomendarProveedor(cid, p.id, !p.recomendado).subscribe({
      next: () => this.cargar(cid),
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo actualizar.'),
    });
  }
}
