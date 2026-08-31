import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { BillingService, EstadoSuscripcionDto } from '../../core/services/billing.service';
import { AdminMiembro, AdminMiembroService, AreaAdmin, AREAS_ADMIN, LABEL_AREA } from '../../core/services/admin-miembro.service';
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
  private billing = inject(BillingService);
  private adminsApi = inject(AdminMiembroService);
  private auth = inject(AuthService);
  private toasts = inject(ToastService);
  private router = inject(Router);

  tipos = TIPOS_CONSORCIO;
  provincias = PROVINCIAS_AR;

  readonly grupos: { titulo: string; items: { id: Tab; label: string; icon: string; listo: boolean }[] }[] = [
    {
      titulo: '', items: [
        { id: 'general', label: 'General', icon: '🏢', listo: true },
        { id: 'administradores', label: 'Administradores', icon: '👥', listo: true },
        { id: 'secciones', label: 'Secciones', icon: '🗂️', listo: true },
        { id: 'preferencias', label: 'Preferencias', icon: '🎛️', listo: false },
        { id: 'suscripcion', label: 'Suscripción', icon: '💳', listo: true },
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

  // ---- Suscripción ----
  suscripcion = signal<EstadoSuscripcionDto | null>(null);
  cargandoSusc = signal(false);
  ciclo = signal<'mensual' | 'anual'>('mensual');
  mpAviso = signal(false);

  private cargarSuscripcion(): void {
    if (this.suscripcion() || this.cargandoSusc()) return;
    this.cargandoSusc.set(true);
    this.billing.estado().subscribe({
      next: (s) => { this.suscripcion.set(s); this.cargandoSusc.set(false); },
      error: () => this.cargandoSusc.set(false),
    });
  }

  irA(t: Tab): void {
    this.tab.set(t);
    if (t === 'suscripcion') this.cargarSuscripcion();
    if (t === 'administradores') this.cargarAdmins();
  }

  // ---- Administradores ----
  areasAll = AREAS_ADMIN;
  labelArea = LABEL_AREA;
  admins = signal<AdminMiembro[]>([]);
  cargandoAdmins = signal(false);

  adminModal = signal(false);
  adminEdit = signal<AdminMiembro | null>(null);   // null = agregar
  adminEmail = signal('');
  adminGeneral = signal(true);
  adminAreas = signal<Set<AreaAdmin>>(new Set());
  guardandoAdmin = signal(false);
  adminBorrar = signal<AdminMiembro | null>(null);

  private cargarAdmins(): void {
    if (this.cargandoAdmins()) return;
    this.cargandoAdmins.set(true);
    this.adminsApi.listar().subscribe({
      next: (l) => { this.admins.set(l); this.cargandoAdmins.set(false); },
      error: () => this.cargandoAdmins.set(false),
    });
  }

  abrirAgregarAdmin(): void {
    this.adminEdit.set(null);
    this.adminEmail.set('');
    this.adminGeneral.set(true);
    this.adminAreas.set(new Set());
    this.adminModal.set(true);
  }
  abrirEditarAdmin(m: AdminMiembro): void {
    this.adminEdit.set(m);
    this.adminGeneral.set(m.esGeneral);
    this.adminAreas.set(new Set(m.areas));
    this.adminModal.set(true);
  }
  toggleArea(a: AreaAdmin): void {
    this.adminAreas.update((s) => { const n = new Set(s); n.has(a) ? n.delete(a) : n.add(a); return n; });
  }
  atajoArea(a: AreaAdmin): void {
    this.adminGeneral.set(false);
    this.adminAreas.update((s) => { const n = new Set(s); n.add(a); return n; });
  }

  guardarAdmin(): void {
    if (this.guardandoAdmin()) return;
    const areas = [...this.adminAreas()];
    if (!this.adminGeneral() && areas.length === 0) { this.toasts.error('Elegí al menos un área.'); return; }
    this.guardandoAdmin.set(true);
    const edit = this.adminEdit();
    const obs = edit
      ? this.adminsApi.cambiarRol(edit.usuarioId, this.adminGeneral(), areas)
      : this.adminsApi.agregar(this.adminEmail().trim(), this.adminGeneral(), areas);
    obs.subscribe({
      next: () => {
        this.guardandoAdmin.set(false);
        this.adminModal.set(false);
        this.toasts.exito(edit ? 'Rol actualizado' : 'Administrador agregado');
        this.admins.set([]);
        this.cargandoAdmins.set(false);
        this.cargarAdmins();
      },
      error: (e) => { this.guardandoAdmin.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'); },
    });
  }

  confirmarBorrarAdmin(): void {
    const m = this.adminBorrar();
    if (!m) return;
    this.adminsApi.quitar(m.usuarioId).subscribe({
      next: () => { this.adminBorrar.set(null); this.toasts.exito('Administrador quitado'); this.admins.set([]); this.cargandoAdmins.set(false); this.cargarAdmins(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo quitar.'),
    });
  }

  fechaLarga(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' });
  }
  plata(n: number): string {
    return new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(n);
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
