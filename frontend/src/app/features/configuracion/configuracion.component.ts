import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ConsorcioDetalle, TIPOS_CONSORCIO, TipoConsorcio, Unidad } from '../../core/models/consorcio.models';

type Tab =
  | 'general' | 'administradores' | 'secciones' | 'preferencias' | 'suscripcion' | 'avanzado'
  | 'perfil' | 'seguridad';

const PROVINCIAS_AR = [
  'Buenos Aires', 'Ciudad Autónoma de Buenos Aires', 'Catamarca', 'Chaco', 'Chubut', 'Córdoba',
  'Corrientes', 'Entre Ríos', 'Formosa', 'Jujuy', 'La Pampa', 'La Rioja', 'Mendoza', 'Misiones',
  'Neuquén', 'Río Negro', 'Salta', 'San Juan', 'San Luis', 'Santa Cruz', 'Santa Fe',
  'Santiago del Estero', 'Tierra del Fuego', 'Tucumán',
];

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.scss',
})
export class ConfiguracionComponent {
  consorcios = inject(ConsorcioService);
  private unidadesApi = inject(UnidadService);
  private auth = inject(AuthService);
  private toasts = inject(ToastService);
  private router = inject(Router);

  tipos = TIPOS_CONSORCIO;
  provincias = PROVINCIAS_AR;

  readonly grupos: { titulo: string; items: { id: Tab; label: string; icon: string; listo: boolean }[] }[] = [
    {
      titulo: '', items: [
        { id: 'general', label: 'General', icon: '🏢', listo: true },
        { id: 'administradores', label: 'Administradores', icon: '👥', listo: false },
        { id: 'secciones', label: 'Secciones', icon: '🗂️', listo: true },
        { id: 'preferencias', label: 'Preferencias', icon: '🎛️', listo: false },
        { id: 'suscripcion', label: 'Suscripción', icon: '💳', listo: false },
        { id: 'avanzado', label: 'Avanzado', icon: '🛡️', listo: true },
      ],
    },
    {
      titulo: 'Mi cuenta', items: [
        { id: 'perfil', label: 'Perfil', icon: '🙍', listo: true },
        { id: 'seguridad', label: 'Seguridad', icon: '🔒', listo: true },
      ],
    },
  ];

  tab = signal<Tab>('general');

  // ---- General: sociedad ----
  detalle = signal<ConsorcioDetalle | null>(null);
  cargando = signal(true);
  editandoPerfil = signal(false);
  editandoDireccion = signal(false);
  guardando = signal(false);

  fNombre = signal('');
  fTipo = signal<TipoConsorcio>('EdificioResidencial');
  fCalle = signal('');
  fCiudad = signal('');
  fProvincia = signal('');
  fCp = signal('');

  cuenta = computed(() => this.auth.sesion());

  // ---- Seguridad: cambio de clave ----
  claveActual = signal('');
  claveNueva = signal('');
  claveNueva2 = signal('');
  cambiandoClave = signal(false);
  claveOk = computed(() =>
    this.claveNueva().length >= 6 && this.claveNueva() === this.claveNueva2() && !!this.claveActual());

  constructor() {
    effect(() => {
      const id = this.consorcios.activoId();
      if (id) { this.cargar(id); this.cargarUnidades(id); }
    });
  }

  private cargar(id: string): void {
    this.cargando.set(true);
    this.consorcios.detalle(id).subscribe({
      next: (d) => {
        this.detalle.set(d);
        this.resetForm(d);
        this.cargando.set(false);
      },
      error: () => { this.cargando.set(false); this.toasts.error('No se pudo cargar la configuración.'); },
    });
  }

  private resetForm(d: ConsorcioDetalle): void {
    this.fNombre.set(d.nombre);
    this.fTipo.set(d.tipo);
    this.fCalle.set(d.direccion);
    this.fCiudad.set(d.localidad ?? '');
    this.fProvincia.set(d.provincia ?? '');
    this.fCp.set(d.codigoPostal ?? '');
  }

  labelTipo(t: TipoConsorcio): string { return this.tipos.find((x) => x.value === t)?.label ?? t; }

  editarPerfil(): void { const d = this.detalle(); if (d) { this.resetForm(d); this.editandoPerfil.set(true); } }
  editarDireccion(): void { const d = this.detalle(); if (d) { this.resetForm(d); this.editandoDireccion.set(true); } }

  guardarPerfil(): void { this.guardar({ nombre: this.fNombre().trim(), tipo: this.fTipo() }, () => this.editandoPerfil.set(false)); }
  guardarDireccion(): void {
    this.guardar({
      direccion: this.fCalle().trim(), localidad: this.fCiudad().trim(),
      provincia: this.fProvincia(), codigoPostal: this.fCp().trim(),
    }, () => this.editandoDireccion.set(false));
  }

  private guardar(cambios: Partial<ConsorcioDetalle>, ok: () => void): void {
    const d = this.detalle();
    const id = this.consorcios.activoId();
    if (!d || !id || this.guardando()) return;
    this.guardando.set(true);
    this.consorcios.actualizar(id, {
      nombre: cambios.nombre ?? d.nombre,
      tipo: cambios.tipo ?? d.tipo,
      direccion: cambios.direccion ?? d.direccion,
      localidad: cambios.localidad ?? d.localidad,
      provincia: cambios.provincia ?? d.provincia,
      pais: 'AR',
      codigoPostal: cambios.codigoPostal ?? d.codigoPostal,
      cuit: d.cuit,
      latitud: d.latitud,
      longitud: d.longitud,
    }).subscribe({
      next: (nd) => { this.detalle.set(nd); this.guardando.set(false); ok(); this.toasts.exito('Cambios guardados'); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  // ---- Secciones ----
  unidades = signal<Unidad[]>([]);
  cargandoUnidades = signal(false);
  secModal = signal(false);
  secEditando = signal<string | null>(null); // nombre de sección en edición
  secNombre = signal('');
  secBusca = signal('');
  secSel = signal<Set<string>>(new Set());
  guardandoSec = signal(false);

  secciones = computed(() => {
    const map = new Map<string, { unidades: number; pisos: Set<number> }>();
    for (const u of this.unidades()) {
      const s = (u.seccion ?? '').trim();
      if (!s) continue;
      if (!map.has(s)) map.set(s, { unidades: 0, pisos: new Set() });
      const e = map.get(s)!;
      e.unidades++;
      e.pisos.add(u.piso);
    }
    return [...map.entries()]
      .map(([nombre, e]) => ({ nombre, unidades: e.unidades, pisos: e.pisos.size }))
      .sort((a, b) => a.nombre.localeCompare(b.nombre));
  });

  unidadesFiltradas = computed(() => {
    const q = this.secBusca().trim().toLowerCase();
    return this.unidades().filter((u) => !q || u.nombre.toLowerCase().includes(q));
  });

  private cargarUnidades(id: string): void {
    this.cargandoUnidades.set(true);
    this.unidadesApi.listar(id).subscribe({
      next: (l) => { this.unidades.set(l); this.cargandoUnidades.set(false); },
      error: () => this.cargandoUnidades.set(false),
    });
  }

  abrirSeccion(nombre?: string): void {
    this.secEditando.set(nombre ?? null);
    this.secNombre.set(nombre ?? '');
    this.secBusca.set('');
    this.secSel.set(new Set(nombre ? this.unidades().filter((u) => (u.seccion ?? '').trim() === nombre).map((u) => u.id) : []));
    this.secModal.set(true);
  }
  toggleUnidadSec(id: string): void {
    this.secSel.update((s) => { const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n; });
  }
  selTodasSec(v: boolean): void {
    this.secSel.set(v ? new Set(this.unidadesFiltradas().map((u) => u.id)) : new Set());
  }

  guardarSeccion(): void {
    const id = this.consorcios.activoId();
    const nombre = this.secNombre().trim();
    if (!id || !nombre || this.guardandoSec()) return;
    const anterior = this.secEditando();
    const sel = this.secSel();

    // Unidades que cambian: las seleccionadas -> nombre; las que tenían "anterior" y ya no están seleccionadas -> null
    const items = this.unidades()
      .filter((u) => sel.has(u.id) || (anterior && (u.seccion ?? '').trim() === anterior))
      .map((u) => ({
        id: u.id, nombre: u.nombre, tipo: u.tipo, ocupacion: u.ocupacion, piso: u.piso,
        areaM2: u.areaM2 ?? null, cuotaMantenimiento: u.cuotaMantenimiento ?? null,
        coeficiente: u.coeficiente ?? null,
        seccion: sel.has(u.id) ? nombre : null,
      }));

    if (items.length === 0) { this.toasts.error('Asigná al menos una unidad a la sección.'); return; }
    this.guardandoSec.set(true);
    this.unidadesApi.editarMasivo(id, items).subscribe({
      next: () => { this.guardandoSec.set(false); this.secModal.set(false); this.toasts.exito('Sección guardada'); this.cargarUnidades(id); },
      error: (e) => { this.guardandoSec.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  secBorrar = signal<string | null>(null);
  confirmarEliminarSeccion(): void {
    const id = this.consorcios.activoId();
    const nombre = this.secBorrar();
    if (!id || !nombre) return;
    const items = this.unidades()
      .filter((u) => (u.seccion ?? '').trim() === nombre)
      .map((u) => ({
        id: u.id, nombre: u.nombre, tipo: u.tipo, ocupacion: u.ocupacion, piso: u.piso,
        areaM2: u.areaM2 ?? null, cuotaMantenimiento: u.cuotaMantenimiento ?? null,
        coeficiente: u.coeficiente ?? null, seccion: null,
      }));
    this.unidadesApi.editarMasivo(id, items).subscribe({
      next: () => { this.secBorrar.set(null); this.toasts.exito('Sección eliminada'); this.cargarUnidades(id); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  cambiarClave(): void {
    if (!this.claveOk() || this.cambiandoClave()) return;
    this.cambiandoClave.set(true);
    this.auth.cambiarClave(this.claveActual(), this.claveNueva()).subscribe({
      next: () => {
        this.cambiandoClave.set(false);
        this.claveActual.set(''); this.claveNueva.set(''); this.claveNueva2.set('');
        this.toasts.exito('Contraseña actualizada');
      },
      error: (e) => { this.cambiandoClave.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo cambiar la contraseña.'); },
    });
  }

  // ---- Avanzado ----
  dlgEliminar = signal(false);
  textoEliminar = signal('');
  eliminandoComunidad = signal(false);
  puedeEliminar = computed(() => this.textoEliminar().trim() === (this.detalle()?.nombre ?? '').trim());

  eliminarComunidad(): void {
    const id = this.consorcios.activoId();
    if (!id || !this.puedeEliminar() || this.eliminandoComunidad()) return;
    this.eliminandoComunidad.set(true);
    this.consorcios.eliminar(id).subscribe({
      next: () => {
        this.eliminandoComunidad.set(false);
        this.toasts.exito('Comunidad eliminada');
        this.consorcios.consorcios.update((l) => l.filter((c) => c.id !== id));
        const otra = this.consorcios.consorcios()[0]?.id ?? null;
        this.consorcios.setActivo(otra);
        this.router.navigate([otra ? '/panel/inicio' : '/nueva-sociedad']);
      },
      error: (e) => { this.eliminandoComunidad.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'); },
    });
  }

  volver(): void { this.router.navigate(['/panel/inicio']); }
}
