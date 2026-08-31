import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ResidenteService } from '../../core/services/residente.service';
import { UnidadService } from '../../core/services/unidad.service';
import { IncidenciaUnidadService } from '../../core/services/incidencia-unidad.service';
import { ToastService } from '../../core/services/toast.service';
import { CARGOS_JUNTA, CargoJunta, Directorio, PersonaDetalle, PersonaUnidadRef, Residente } from '../../core/models/residente.models';
import { IncidenciaUnidad, LABEL_CATEGORIA, RolUnidad, Unidad } from '../../core/models/consorcio.models';
import { InvitarResidenteComponent } from './invitar-residente.component';
import { AyudaPanelComponent } from '../../shared/ayuda-panel.component';

type Filtro = 'Todos' | 'Propietario' | 'Inquilino' | 'Gestor';

@Component({
  selector: 'app-directorio',
  standalone: true,
  imports: [FormsModule, InvitarResidenteComponent, AyudaPanelComponent],
  templateUrl: './directorio.component.html',
  styleUrl: './directorio.component.scss',
})
export class DirectorioComponent {
  private router = inject(Router);
  private toasts = inject(ToastService);
  consorcios = inject(ConsorcioService);
  private api = inject(ResidenteService);
  private unidadesApi = inject(UnidadService);
  private incidenciasApi = inject(IncidenciaUnidadService);

  data = signal<Directorio | null>(null);
  cargando = signal(true);
  error = signal<string | null>(null);
  busqueda = signal('');
  filtro = signal<Filtro>('Todos');
  invitarAbierto = signal(false);
  ayuda = signal(false);

  unidades = signal<Unidad[]>([]);
  detalle = signal<PersonaDetalle | null>(null);
  personaIdSel = signal<string>('');
  editandoTel = signal(false);
  telNuevo = signal('');
  agregandoUnidad = signal(false);
  nuevaUnidadId = signal('');
  nuevaUnidadRol = signal<RolUnidad>('Propietario');
  incidenciasAbierto = signal(false);
  cargandoIncidencias = signal(false);
  incidencias = signal<(IncidenciaUnidad & { unidadNombre: string })[]>([]);
  labelCategoria = LABEL_CATEGORIA;

  cargosJunta = CARGOS_JUNTA;
  gestionarRolesAbierto = signal(false);
  rolesJuntaSel = signal<Set<CargoJunta>>(new Set());
  guardandoRoles = signal(false);
  removerAbierto = signal(false);

  private consorcioId = computed(() => this.consorcios.activoId());

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const f = this.filtro();
    return (this.data()?.residentes ?? [])
      .filter((r) => f === 'Todos' || r.rol === f)
      .filter((r) => !q
        || `${r.nombre} ${r.apellido}`.toLowerCase().includes(q)
        || (r.email ?? '').toLowerCase().includes(q)
        || r.unidadNombre.toLowerCase().includes(q));
  });

  unidadesDisponibles = computed(() => {
    const asignadas = new Set((this.detalle()?.unidades ?? []).map((u) => u.unidadId));
    return this.unidades().filter((u) => !asignadas.has(u.id));
  });

  constructor() {
    effect(() => { const id = this.consorcioId(); if (id) this.cargar(id); });
  }

  private cargar(cid: string): void {
    this.cargando.set(true);
    this.error.set(null);
    this.api.directorio(cid).subscribe({
      next: (d) => { this.data.set(d); this.cargando.set(false); },
      error: () => { this.error.set('No pudimos cargar el directorio.'); this.cargando.set(false); },
    });
    this.unidadesApi.listar(cid).subscribe((u) => this.unidades.set(u));
  }

  refrescar(): void {
    const id = this.consorcioId();
    if (id) this.cargar(id);
  }

  cerrar(): void {
    this.router.navigate(['/panel/unidades']);
  }

  onInvitado(): void {
    this.invitarAbierto.set(false);
    this.refrescar();
    const id = this.consorcioId();
    if (id) this.api.refrescarPendientes(id);
  }

  iniciales(r: { nombre: string; apellido: string }): string {
    return ((r.nombre[0] ?? '') + (r.apellido[0] ?? '')).toUpperCase() || '?';
  }

  fecha(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  // ---- Detalle del residente ----
  abrirDetalle(r: Residente): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.personaIdSel.set(r.id);
    this.editandoTel.set(false);
    this.agregandoUnidad.set(false);
    this.incidenciasAbierto.set(false);
    this.incidencias.set([]);
    this.api.personaDetalle(cid, r.id).subscribe((d) => {
      this.detalle.set(d);
      this.telNuevo.set(d.telefono ?? '');
    });
  }

  cerrarDetalle(): void {
    this.detalle.set(null);
    this.refrescar();
  }

  toggleIncidencias(): void {
    const abrir = !this.incidenciasAbierto();
    this.incidenciasAbierto.set(abrir);
    const cid = this.consorcioId();
    const d = this.detalle();
    if (!abrir || !cid || !d || this.incidencias().length || this.cargandoIncidencias()) return;
    if (!d.unidades.length) return;
    this.cargandoIncidencias.set(true);
    forkJoin(
      d.unidades.map((u) => this.incidenciasApi.listar(cid, u.unidadId)),
    ).subscribe({
      next: (listas) => {
        const merged = listas.flatMap((lista, i) =>
          lista.map((inc) => ({ ...inc, unidadNombre: d.unidades[i].unidadNombre })),
        );
        merged.sort((a, b) => b.fechaEvento.localeCompare(a.fechaEvento));
        this.incidencias.set(merged);
        this.cargandoIncidencias.set(false);
      },
      error: () => this.cargandoIncidencias.set(false),
    });
  }

  private recargarDetalle(): void {
    const cid = this.consorcioId();
    if (cid) this.api.personaDetalle(cid, this.personaIdSel()).subscribe((d) => this.detalle.set(d));
  }

  guardarTelefono(): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    if (!cid || !d) return;
    this.api.actualizarContacto(cid, this.personaIdSel(), {
      nombre: d.nombre, apellido: d.apellido, telefono: this.telNuevo().trim() || null,
    }).subscribe({
      next: () => { this.editandoTel.set(false); this.toasts.exito('Teléfono actualizado'); this.recargarDetalle(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo guardar.'),
    });
  }

  agregarAUnidad(): void {
    const cid = this.consorcioId();
    const d = this.detalle();
    if (!cid || !d || !this.nuevaUnidadId()) return;
    const [nombre, ...resto] = `${d.nombre} ${d.apellido}`.trim().split(/\s+/);
    this.unidadesApi.agregarPersona(cid, this.nuevaUnidadId(), {
      nombre, apellido: resto.join(' '), email: d.email, telefono: d.telefono ?? null,
      rol: this.nuevaUnidadRol(),
    }).subscribe({
      next: () => {
        this.agregandoUnidad.set(false);
        this.nuevaUnidadId.set('');
        this.toasts.exito('Residente agregado a la unidad');
        this.recargarDetalle();
      },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo agregar.'),
    });
  }

  cambiarRolUnidad(u: PersonaUnidadRef, rol: string): void {
    const cid = this.consorcioId();
    if (!cid || rol === u.rol) return;
    this.unidadesApi.cambiarRolPersona(cid, u.unidadId, u.personaId, rol as RolUnidad).subscribe({
      next: () => { this.toasts.exito('Rol actualizado'); this.recargarDetalle(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cambiar el rol.'),
    });
  }

  quitarDeUnidad(u: PersonaUnidadRef): void {
    const cid = this.consorcioId();
    if (!cid || !confirm(`¿Quitar a este residente de la unidad ${u.unidadNombre}?`)) return;
    this.unidadesApi.eliminarPersona(cid, u.unidadId, u.personaId).subscribe({
      next: () => { this.toasts.exito('Quitado de la unidad'); this.recargarDetalle(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo quitar.'),
    });
  }

  // ---- Roles de junta ----
  abrirGestionarRoles(): void {
    const d = this.detalle();
    this.rolesJuntaSel.set(new Set(d?.rolesJunta ?? []));
    this.gestionarRolesAbierto.set(true);
  }

  toggleCargoJunta(c: CargoJunta): void {
    this.rolesJuntaSel.update((s) => {
      const n = new Set(s);
      n.has(c) ? n.delete(c) : n.add(c);
      return n;
    });
  }

  guardarRolesJunta(): void {
    const cid = this.consorcioId();
    if (!cid || this.guardandoRoles()) return;
    this.guardandoRoles.set(true);
    this.api.gestionarRolesJunta(cid, this.personaIdSel(), [...this.rolesJuntaSel()]).subscribe({
      next: () => {
        this.guardandoRoles.set(false);
        this.gestionarRolesAbierto.set(false);
        this.toasts.exito('Roles de junta actualizados');
        this.recargarDetalle();
      },
      error: (e) => { this.guardandoRoles.set(false); this.toasts.error(e?.error?.message ?? 'No se pudieron guardar los roles.'); },
    });
  }

  removerDeComunidad(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.removerDeComunidad(cid, this.personaIdSel()).subscribe({
      next: () => { this.removerAbierto.set(false); this.toasts.exito('Residente removido de la comunidad'); this.cerrarDetalle(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo remover.'),
    });
  }
}
