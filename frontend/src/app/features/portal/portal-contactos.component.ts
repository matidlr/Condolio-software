import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CATEGORIAS_CONTACTO, Contacto, GuardarContacto, MiContactoService } from '../../core/services/mi-contacto.service';
import { ToastService } from '../../core/services/toast.service';

const SERVICIOS = ['Plomería', 'Electricista', 'Gasista', 'Cerrajero', 'Jardinero', 'Pintor', 'Carpintero', 'Limpieza', 'Servicio de Mascotas', 'Reparación de electrodomésticos'];
const MANTENIMIENTO = ['Plomería', 'Electricista', 'Gasista', 'Pintor', 'Carpintero', 'Reparación de electrodomésticos', 'Cerrajero'];

@Component({
  selector: 'app-portal-contactos',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './portal-contactos.component.html',
  styleUrl: './portal-contactos.component.scss',
})
export class PortalContactosComponent {
  private api = inject(MiContactoService);
  private toasts = inject(ToastService);

  categorias = CATEGORIAS_CONTACTO;

  vista = signal<'lista' | 'nuevo' | 'detalle'>('lista');
  cargando = signal(true);
  contactos = signal<Contacto[]>([]);
  busqueda = signal('');
  tab = signal<'todos' | 'servicios' | 'mantenimiento'>('todos');

  sel = signal<Contacto | null>(null);
  form = signal<GuardarContacto>(this.formVacio());
  editandoId = signal<string | null>(null);
  guardando = signal(false);

  private formVacio(): GuardarContacto {
    return { nombre: '', categoria: 'Otro', telefono: '+54', email: null, empresa: null, notas: null };
  }

  constructor() {
    this.cargar();
  }

  private cargar(): void {
    this.cargando.set(true);
    this.api.listar().subscribe({
      next: (l) => { this.contactos.set(l); this.cargando.set(false); },
      error: () => { this.toasts.error('No pudimos cargar los contactos.'); this.cargando.set(false); },
    });
  }

  filtrados = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const t = this.tab();
    return this.contactos().filter((c) => {
      if (q && !c.nombre.toLowerCase().includes(q) && !c.categoria.toLowerCase().includes(q)) return false;
      if (t === 'servicios') return SERVICIOS.includes(c.categoria);
      if (t === 'mantenimiento') return MANTENIMIENTO.includes(c.categoria);
      return true;
    });
  });

  grupos = computed(() => {
    const map = new Map<string, Contacto[]>();
    for (const c of this.filtrados()) {
      (map.get(c.categoria) ?? map.set(c.categoria, []).get(c.categoria)!).push(c);
    }
    return [...map.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([cat, items]) => ({ cat, items }));
  });

  inicial(nombre: string): string { return (nombre || '?').trim().charAt(0).toUpperCase(); }

  abrir(c: Contacto): void { this.sel.set(c); this.vista.set('detalle'); }

  nuevo(): void {
    this.editandoId.set(null);
    this.form.set(this.formVacio());
    this.vista.set('nuevo');
  }
  editar(c: Contacto): void {
    this.editandoId.set(c.id);
    this.form.set({ nombre: c.nombre, categoria: c.categoria, telefono: c.telefono, email: c.email, empresa: c.empresa, notas: c.notas });
    this.vista.set('nuevo');
  }
  setForm<K extends keyof GuardarContacto>(k: K, v: GuardarContacto[K]): void {
    this.form.update((f) => ({ ...f, [k]: v }));
  }
  formValido = computed(() => this.form().nombre.trim().length > 0 && this.form().telefono.trim().length > 2);

  guardar(): void {
    if (!this.formValido() || this.guardando()) return;
    this.guardando.set(true);
    const id = this.editandoId();
    const obs = id ? this.api.actualizar(id, this.form()) : this.api.crear(this.form());
    obs.subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito(id ? 'Contacto actualizado' : 'Contacto agregado');
        this.vista.set('lista');
        this.cargar();
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  eliminar(c: Contacto): void {
    this.api.eliminar(c.id).subscribe({
      next: () => { this.toasts.exito('Contacto eliminado'); this.vista.set('lista'); this.cargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  copiarTel(c: Contacto): void {
    navigator.clipboard?.writeText(c.telefono).then(
      () => this.toasts.exito('Teléfono copiado'),
      () => this.toasts.info(c.telefono),
    );
  }
  compartir(c: Contacto): void {
    const txt = `${c.nombre} (${c.categoria})\n${c.telefono}`;
    if (navigator.share) navigator.share({ title: c.nombre, text: txt }).catch(() => {});
    else this.copiarTel(c);
  }
}
