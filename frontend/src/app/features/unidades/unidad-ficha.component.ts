import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { NotaUnidadService } from '../../core/services/nota-unidad.service';
import { IncidenciaUnidadService } from '../../core/services/incidencia-unidad.service';
import { AdjuntosComponent } from '../../shared/adjuntos.component';
import { ToastService } from '../../core/services/toast.service';
import {
  ActividadUnidad,
  CATEGORIAS_INCIDENCIA,
  CategoriaIncidencia,
  GuardarUnidad,
  IncidenciaDetalle,
  IncidenciaUnidad,
  LABEL_CATEGORIA,
  NotaUnidad,
  OCUPACIONES,
  PLANTILLAS_INCIDENCIA,
  PersonaUnidad,
  RolUnidad,
  SeveridadIncidencia,
  TipoOcupacion,
  TipoUnidad,
  Unidad,
  UnidadDetalle,
} from '../../core/models/consorcio.models';

type Tab = 'resumen' | 'detalles' | 'residentes' | 'notas' | 'actividad' | 'incidencias';

const TIPOS: TipoUnidad[] = ['Departamento', 'Local', 'Cochera', 'Baulera'];

@Component({
  selector: 'app-unidad-ficha',
  standalone: true,
  imports: [ReactiveFormsModule, AdjuntosComponent],
  templateUrl: './unidad-ficha.component.html',
  styleUrl: './unidad-ficha.component.scss',
  host: { '(document:click)': 'onDocClick($event)' },
})
export class UnidadFichaComponent {
  id = input.required<string>();

  private fb = inject(FormBuilder);
  private router = inject(Router);
  private consorcios = inject(ConsorcioService);
  private api = inject(UnidadService);
  private notasApi = inject(NotaUnidadService);
  private incApi = inject(IncidenciaUnidadService);
  private toasts = inject(ToastService);

  readonly tipos = TIPOS;
  readonly ocupaciones = OCUPACIONES;
  readonly categorias = CATEGORIAS_INCIDENCIA;
  readonly plantillas = PLANTILLAS_INCIDENCIA;
  readonly severidades: { value: SeveridadIncidencia; label: string }[] = [
    { value: 'Critica', label: 'Crítica' },
    { value: 'Alta', label: 'Alta' },
    { value: 'Media', label: 'Media' },
    { value: 'Baja', label: 'Baja' },
  ];
  readonly labelCategoria = LABEL_CATEGORIA;

  tab = signal<Tab>('resumen');
  cargando = signal(true);
  guardando = signal(false);
  error = signal<string | null>(null);
  guardadoOk = signal(false);
  unidad = signal<UnidadDetalle | null>(null);
  hermanas = signal<Unidad[]>([]);

  ocupacionPendiente = signal<TipoOcupacion>('Desocupado');
  personaModal = signal<{ rol: RolUnidad; modo: 'buscar' | 'manual' } | null>(null);

  notas = signal<NotaUnidad[]>([]);
  notaModal = signal<{ nota: NotaUnidad | null } | null>(null);

  actividad = signal<ActividadUnidad[]>([]);
  incidencias = signal<IncidenciaUnidad[]>([]);
  incidenciaModal = signal<{ incidencia: IncidenciaUnidad | null } | null>(null);
  incidenciaDetalle = signal<IncidenciaDetalle | null>(null);

  menuUnidadAbierto = signal(false);
  eliminarUnidadModal = signal(false);
  confirmarEliminarTexto = signal('');
  desocuparModal = signal(false);

  readonly tabs: { id: Tab; label: string }[] = [
    { id: 'resumen', label: 'Resumen' },
    { id: 'detalles', label: 'Detalles' },
    { id: 'residentes', label: 'Residentes' },
    { id: 'notas', label: 'Notas' },
    { id: 'actividad', label: 'Actividad' },
    { id: 'incidencias', label: 'Incidencias' },
  ];

  form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    tipo: ['Departamento' as TipoUnidad, [Validators.required]],
    piso: [0],
    areaM2: [null as number | null],
    coeficiente: [null as number | null],
    seccion: [''],
    cuotaMantenimiento: [null as number | null],
    facturable: [true],
  });

  formPersona = this.fb.nonNullable.group({
    nombreCompleto: ['', [Validators.required]],
    email: [''],
    telefono: [''],
    esContactoPrincipal: [false],
  });

  formNota = this.fb.nonNullable.group({
    texto: ['', [Validators.required]],
  });

  formComentario = this.fb.nonNullable.group({
    texto: ['', [Validators.required]],
  });

  formIncidencia = this.fb.nonNullable.group({
    categoria: ['Otro' as CategoriaIncidencia, [Validators.required]],
    severidad: ['Media' as SeveridadIncidencia, [Validators.required]],
    fechaEvento: [this.hoyISO()],
    titulo: [''],
    descripcion: ['', [Validators.required]],
  });

  etiquetasChips = signal<string[]>([]);
  etiquetaInput = signal('');

  agregarEtiquetas(): void {
    const nuevas = this.etiquetaInput()
      .split(',')
      .map((t) => t.trim())
      .filter((t) => t.length > 0);
    if (!nuevas.length) return;
    const set = new Set([...this.etiquetasChips(), ...nuevas]);
    this.etiquetasChips.set([...set]);
    this.etiquetaInput.set('');
  }

  quitarEtiqueta(tag: string): void {
    this.etiquetasChips.set(this.etiquetasChips().filter((t) => t !== tag));
  }

  private hoyISO(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private consorcioId = computed(() => this.consorcios.activoId());

  indice = computed(() => this.hermanas().findIndex((u) => u.id === this.id()));
  anterior = computed(() => this.hermanas()[this.indice() - 1] ?? null);
  siguiente = computed(() => this.hermanas()[this.indice() + 1] ?? null);

  propietarios = computed(() => this.unidad()?.personas.filter((p) => p.rol === 'Propietario') ?? []);
  inquilinos = computed(() => this.unidad()?.personas.filter((p) => p.rol === 'Inquilino') ?? []);
  gestores = computed(() => this.unidad()?.personas.filter((p) => p.rol === 'Gestor') ?? []);

  mostrarInquilinos = computed(() => this.unidad()?.ocupacion === 'Alquiler');

  ocupacionCambio = computed(() => this.ocupacionPendiente() !== this.unidad()?.ocupacion);

  constructor() {
    effect(() => {
      this.id();
      this.cargar();
    });
  }

  private cargar(): void {
    const cid = this.consorcioId();
    if (!cid) {
      this.error.set('Elegí un consorcio.');
      this.cargando.set(false);
      return;
    }
    this.cargando.set(true);
    this.error.set(null);
    this.guardadoOk.set(false);
    this.api.obtener(cid, this.id()).subscribe({
      next: (u) => {
        this.unidad.set(u);
        this.ocupacionPendiente.set(u.ocupacion);
        this.form.reset({
          nombre: u.nombre,
          tipo: u.tipo,
          piso: u.piso,
          areaM2: u.areaM2 ?? null,
          coeficiente: u.coeficiente ?? null,
          seccion: u.seccion ?? '',
          cuotaMantenimiento: u.cuotaMantenimiento ?? null,
          facturable: u.facturable,
        });
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No encontramos la unidad.');
        this.cargando.set(false);
      },
    });
    this.api.listar(cid).subscribe((list) => this.hermanas.set(list));
    this.notasApi.listar(cid, this.id()).subscribe((n) => this.notas.set(n));
    this.incApi.actividad(cid, this.id()).subscribe((a) => this.actividad.set(a));
    this.incApi.listar(cid, this.id()).subscribe((i) => this.incidencias.set(i));
  }

  private recargarActividad(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.incApi.actividad(cid, this.id()).subscribe((a) => this.actividad.set(a));
    this.incApi.listar(cid, this.id()).subscribe((i) => this.incidencias.set(i));
  }

  private recargar(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.api.obtener(cid, this.id()).subscribe((u) => {
      this.unidad.set(u);
      this.ocupacionPendiente.set(u.ocupacion);
    });
    this.consorcios.cargar().subscribe();
    this.recargarActividad();
  }

  onDocClick(e: MouseEvent): void {
    if (!(e.target as HTMLElement).closest('.f-menu')) this.menuUnidadAbierto.set(false);
  }

  volver(): void {
    this.router.navigate(['/panel/unidades']);
  }

  irA(u: Unidad | null): void {
    if (u) this.router.navigate(['/panel/unidades', u.id]);
  }

  // ---- Detalles ----
  guardarDetalles(): void {
    const cid = this.consorcioId();
    if (!cid || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const body: GuardarUnidad = {
      nombre: v.nombre,
      piso: v.piso,
      tipo: v.tipo,
      cuotaMantenimiento: v.cuotaMantenimiento,
      coeficiente: v.coeficiente,
      facturable: v.facturable,
      areaM2: v.areaM2,
      seccion: v.seccion?.trim() || null,
    };
    this.guardando.set(true);
    this.error.set(null);
    this.api.actualizar(cid, this.id(), body).subscribe({
      next: () => {
        this.guardando.set(false);
        this.guardadoOk.set(true);
        this.toasts.exito('Unidad actualizada exitosamente');
        this.recargar();
      },
      error: (e) => {
        this.error.set(e?.error?.message ?? 'No se pudo guardar.');
        this.guardando.set(false);
      },
    });
  }

  // ---- Ocupación ----
  confirmarOcupacion(): void {
    const tieneOcupantes = this.propietarios().length + this.inquilinos().length > 0;
    if (this.ocupacionPendiente() === 'Desocupado' && tieneOcupantes) {
      this.desocuparModal.set(true);
      return;
    }
    this.aplicarOcupacion();
  }

  aplicarOcupacion(): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.desocuparModal.set(false);
    this.api.cambiarOcupacion(cid, this.id(), this.ocupacionPendiente()).subscribe({
      next: () => { this.toasts.exito('Ocupación actualizada'); this.recargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cambiar la ocupación.'),
    });
  }

  toggleInquilinosFinanzas(): void {
    const cid = this.consorcioId();
    const u = this.unidad();
    if (!cid || !u) return;
    this.api.cambiarInquilinosFinanzas(cid, this.id(), !u.inquilinosVenFinanzas).subscribe({
      next: () => { this.toasts.exito('Configuración actualizada'); this.recargar(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cambiar la configuración.'),
    });
  }

  // ---- Personas ----
  abrirPersona(rol: RolUnidad): void {
    this.formPersona.reset({ nombreCompleto: '', email: '', telefono: '', esContactoPrincipal: false });
    this.error.set(null);
    this.personaModal.set({ rol, modo: 'manual' });
  }

  cerrarPersona(): void {
    this.personaModal.set(null);
  }

  agregarPersona(): void {
    const cid = this.consorcioId();
    const modal = this.personaModal();
    if (!cid || !modal || this.formPersona.invalid) {
      this.formPersona.markAllAsTouched();
      return;
    }
    const v = this.formPersona.getRawValue();
    const full = v.nombreCompleto.trim().split(/\s+/);
    const nombre = full[0];
    const apellido = full.slice(1).join(' ');

    this.guardando.set(true);
    this.error.set(null);
    this.api.agregarPersona(cid, this.id(), {
      nombre,
      apellido,
      email: v.email?.trim() || null,
      telefono: v.telefono?.trim() || null,
      rol: modal.rol,
      esContactoPrincipal: v.esContactoPrincipal,
    }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito(`${modal.rol} agregado`);
        this.cerrarPersona();
        this.recargar();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo agregar.'); this.guardando.set(false); },
    });
  }

  marcarPrincipal(p: PersonaUnidad): void {
    const cid = this.consorcioId();
    if (!cid || p.esContactoPrincipal) return;
    this.api.marcarPrincipal(cid, this.id(), p.id).subscribe({
      next: () => { this.toasts.exito('Contacto principal actualizado'); this.recargar(); },
    });
  }

  cambiarRol(p: PersonaUnidad): void {
    const cid = this.consorcioId();
    if (!cid) return;
    const nuevo: RolUnidad = p.rol === 'Propietario' ? 'Inquilino' : 'Propietario';
    this.api.cambiarRolPersona(cid, this.id(), p.id, nuevo).subscribe({
      next: () => { this.toasts.exito(`Ahora es ${nuevo}`); this.recargar(); },
    });
  }

  eliminarPersona(p: PersonaUnidad): void {
    const cid = this.consorcioId();
    if (!cid || !confirm(`¿Está seguro que desea eliminar a ${p.nombre} ${p.apellido} de esta unidad?`)) return;
    this.api.eliminarPersona(cid, this.id(), p.id).subscribe({
      next: () => { this.toasts.exito('Ocupante eliminado'); this.recargar(); },
    });
  }

  iniciales(p: PersonaUnidad): string {
    return ((p.nombre[0] ?? '') + (p.apellido[0] ?? '')).toUpperCase() || '?';
  }

  // ---- Notas ----
  private recargarNotas(): void {
    const cid = this.consorcioId();
    if (cid) this.notasApi.listar(cid, this.id()).subscribe((n) => this.notas.set(n));
    this.recargarActividad();
  }

  abrirNota(nota: NotaUnidad | null): void {
    this.formNota.reset({ texto: nota?.texto ?? '' });
    this.error.set(null);
    this.notaModal.set({ nota });
  }

  cerrarNota(): void {
    this.notaModal.set(null);
  }

  guardarNota(): void {
    const cid = this.consorcioId();
    const modal = this.notaModal();
    if (!cid || !modal || this.formNota.invalid) {
      this.formNota.markAllAsTouched();
      return;
    }
    const texto = this.formNota.getRawValue().texto.trim();
    this.guardando.set(true);
    this.error.set(null);
    const op = modal.nota
      ? this.notasApi.editar(cid, this.id(), modal.nota.id, texto)
      : this.notasApi.agregar(cid, this.id(), texto);
    op.subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito(modal.nota ? 'Nota actualizada' : 'Nota agregada exitosamente');
        this.cerrarNota();
        this.recargarNotas();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo guardar la nota.'); this.guardando.set(false); },
    });
  }

  eliminarNota(nota: NotaUnidad): void {
    const cid = this.consorcioId();
    if (!cid || !confirm('¿Eliminar esta nota? Esta acción no se puede deshacer.')) return;
    this.notasApi.eliminar(cid, this.id(), nota.id).subscribe({
      next: () => { this.toasts.exito('Nota eliminada'); this.recargarNotas(); },
    });
  }

  // ---- Incidencias ----
  abrirIncidencia(inc: IncidenciaUnidad | null): void {
    this.formIncidencia.reset({
      categoria: inc?.categoria ?? 'Otro',
      severidad: inc?.severidad ?? 'Media',
      fechaEvento: inc ? inc.fechaEvento.slice(0, 10) : this.hoyISO(),
      titulo: inc?.titulo ?? '',
      descripcion: inc?.descripcion ?? '',
    });
    this.etiquetasChips.set(inc?.etiquetas ?? []);
    this.etiquetaInput.set('');
    this.error.set(null);
    this.incidenciaModal.set({ incidencia: inc });
  }

  cerrarIncidencia(): void {
    this.incidenciaModal.set(null);
  }

  aplicarPlantilla(p: (typeof PLANTILLAS_INCIDENCIA)[number]): void {
    this.formIncidencia.patchValue({
      categoria: p.categoria,
      severidad: p.severidad,
      titulo: p.titulo,
    });
  }

  guardarIncidencia(): void {
    const cid = this.consorcioId();
    const modal = this.incidenciaModal();
    if (!cid || !modal || this.formIncidencia.invalid) {
      this.formIncidencia.markAllAsTouched();
      return;
    }
    this.agregarEtiquetas(); // absorbe texto pendiente en el input
    const v = this.formIncidencia.getRawValue();
    const body = {
      categoria: v.categoria,
      severidad: v.severidad,
      fechaEvento: v.fechaEvento ? new Date(v.fechaEvento).toISOString() : null,
      titulo: v.titulo?.trim() || null,
      descripcion: v.descripcion.trim(),
      etiquetas: this.etiquetasChips(),
    };
    this.guardando.set(true);
    this.error.set(null);
    const op = modal.incidencia
      ? this.incApi.editar(cid, this.id(), modal.incidencia.id, body)
      : this.incApi.registrar(cid, this.id(), body);
    op.subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito(modal.incidencia ? 'Incidencia actualizada' : 'Incidencia registrada exitosamente');
        this.cerrarIncidencia();
        this.recargarActividad();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo guardar la incidencia.'); this.guardando.set(false); },
    });
  }

  eliminarIncidencia(inc: IncidenciaUnidad): void {
    const cid = this.consorcioId();
    if (!cid || !confirm('¿Estás seguro que deseas eliminar esta incidencia? Esta acción no se puede deshacer.')) return;
    this.incApi.eliminar(cid, this.id(), inc.id).subscribe({
      next: () => { this.toasts.exito('Incidencia eliminada'); this.incidenciaDetalle.set(null); this.recargarActividad(); },
    });
  }

  verIncidencia(inc: IncidenciaUnidad): void {
    const cid = this.consorcioId();
    if (!cid) return;
    this.formComentario.reset({ texto: '' });
    this.incApi.obtener(cid, this.id(), inc.id).subscribe({
      next: (d) => this.incidenciaDetalle.set(d),
    });
  }

  cerrarIncidenciaDetalle(): void {
    this.incidenciaDetalle.set(null);
  }

  editarDesdeDetalle(): void {
    const d = this.incidenciaDetalle();
    if (!d) return;
    this.incidenciaDetalle.set(null);
    this.abrirIncidencia(d.incidencia);
  }

  agregarComentario(): void {
    const cid = this.consorcioId();
    const d = this.incidenciaDetalle();
    if (!cid || !d || this.formComentario.invalid) return;
    const texto = this.formComentario.getRawValue().texto.trim();
    this.guardando.set(true);
    this.incApi.comentar(cid, this.id(), d.incidencia.id, texto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.formComentario.reset({ texto: '' });
        this.toasts.exito('Nota agregada');
        this.verIncidencia(d.incidencia);
        this.recargarActividad();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo agregar la nota.'); this.guardando.set(false); },
    });
  }

  escalarIncidencia(): void {
    const cid = this.consorcioId();
    const d = this.incidenciaDetalle();
    if (!cid || !d) return;
    if (!confirm('Se creará un ticket de "Reportar problema" con la información de esta incidencia. Ambos quedarán vinculados.')) return;
    this.incApi.escalar(cid, this.id(), d.incidencia.id).subscribe({
      next: () => { this.toasts.exito('Incidencia escalada a ticket'); this.verIncidencia(d.incidencia); this.recargarActividad(); },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo escalar.'),
    });
  }

  // ---- Eliminar unidad ----
  abrirEliminarUnidad(): void {
    this.menuUnidadAbierto.set(false);
    this.confirmarEliminarTexto.set('');
    this.eliminarUnidadModal.set(true);
  }

  eliminarUnidad(): void {
    const cid = this.consorcioId();
    if (!cid || this.confirmarEliminarTexto().trim().toUpperCase() !== 'ELIMINAR') return;
    this.api.eliminar(cid, this.id()).subscribe({
      next: () => {
        this.toasts.exito('Unidad eliminada');
        this.consorcios.cargar().subscribe();
        this.router.navigate(['/panel/unidades']);
      },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar la unidad.'),
    });
  }

  fecha(iso: string): string {
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  fechaHora(iso: string): string {
    return new Date(iso).toLocaleString('es-AR', {
      day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
    }).replace(',', ' ·');
  }

  hace(iso: string): string {
    const s = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
    if (s < 60) return 'hace unos segundos';
    if (s < 3600) return `hace ${Math.floor(s / 60)} min`;
    if (s < 86400) return `hace ${Math.floor(s / 3600)} h`;
    return new Date(iso).toLocaleDateString('es-AR', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  tituloModal(): string {
    const rol = this.personaModal()?.rol;
    return rol === 'Gestor' ? 'Agregar gestor' : rol === 'Inquilino' ? 'Agregar inquilino' : 'Agregar propietario';
  }

  money(n: number | null | undefined): string {
    return '$' + (n ?? 0).toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  proximoCobro(): string {
    const d = new Date();
    d.setMonth(d.getMonth() + 1, 10);
    return d.toLocaleDateString('es-AR', { day: 'numeric', month: 'long', year: 'numeric' });
  }
}
