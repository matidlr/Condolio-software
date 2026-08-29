import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of, switchMap, tap } from 'rxjs';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { AmenidadService } from '../../core/services/amenidad.service';
import { AdjuntoService } from '../../core/services/adjunto.service';
import { ToastService } from '../../core/services/toast.service';
import {
  AmenidadHorario, DIAS_SEMANA, INTERVALOS_RESERVA, opcionesHora, horaLabel,
} from '../../core/models/amenidad.models';

interface ImagenStage { file?: File; url: string; adjuntoId?: string; }

@Component({
  selector: 'app-nueva-amenidad',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './nueva-amenidad.component.html',
  styleUrl: './amenidades.component.scss',
})
export class NuevaAmenidadComponent {
  private router = inject(Router);
  private ruta = inject(ActivatedRoute);
  private consorcios = inject(ConsorcioService);
  private api = inject(AmenidadService);
  private adjuntos = inject(AdjuntoService);
  private toasts = inject(ToastService);

  dias = DIAS_SEMANA;
  intervalos = INTERVALOS_RESERVA;
  horas = opcionesHora();
  horaLabel = horaLabel;

  paso = signal(1);
  guardando = signal(false);
  editarId = signal<string | null>(null);
  progreso = signal<{ label: string; icono: string; hecho: boolean }[] | null>(null);
  progresoHechos = computed(() => this.progreso()?.filter((p) => p.hecho).length ?? 0);
  exito = signal(false);

  readonly pasos = [
    'Información Básica', 'Configuración de Reservaciones', 'Horario Semanal', 'Revisión y Documentos',
  ];

  // Paso 1
  nombre = signal('');
  descripcion = signal('');
  imagenes = signal<ImagenStage[]>([]);

  // Paso 2
  reservable = signal(false);
  intervaloMinutos = signal(60);
  limiteMensual = signal(false);
  maxReservas = signal(1);
  tieneCosto = signal(false);
  tarifa = signal<number | null>(null);
  requiereAprobacion = signal(false);
  reservableDesde = signal(new Date().toISOString().slice(0, 10));
  diasBloqueados = signal<string[]>([]);
  mensajeReserva = signal('');

  // Paso 3
  horarios = signal<AmenidadHorario[]>(
    DIAS_SEMANA.map((d) => ({ dia: d.valor, cerrado: d.valor === 0 || d.valor === 6 ? false : false, abreMin: 9 * 60, cierraMin: 18 * 60 })),
  );

  // Paso 4
  documentos = signal<File[]>([]);
  docModal = signal(false);
  docPendiente = signal<File | null>(null);
  docNombre = signal('');

  // Calendario de días bloqueados
  mesCal = signal(new Date());

  puedeAvanzar = computed(() => this.paso() !== 1 || this.nombre().trim().length > 0);
  diasActivos = computed(() => this.horarios().filter((h) => !h.cerrado).length);
  intervaloLabel = computed(() =>
    INTERVALOS_RESERVA.find((o) => o.valor === this.intervaloMinutos())?.label ?? '—');
  limiteLabel = computed(() => {
    const n = this.maxReservas();
    const cant = n === 0 ? 'ilimitadas' : `${n} ${n === 1 ? 'reserva' : 'reservas'}`;
    return `${cant} ${this.limiteMensual() ? 'por mes / unidad' : 'simultáneas / residente'}`;
  });

  constructor() {
    const id = this.ruta.snapshot.queryParamMap.get('editar');
    if (id) {
      this.editarId.set(id);
      const cid = this.consorcios.activoId();
      if (cid) this.api.obtener(cid, id).subscribe((a) => this.precargar(a));
    }
  }

  private precargar(a: import('../../core/models/amenidad.models').Amenidad): void {
    this.nombre.set(a.nombre);
    this.descripcion.set(a.descripcion ?? '');
    this.imagenes.set(a.imagenesIds.map((adjuntoId) => ({ adjuntoId, url: '' })));
    this.imagenes().forEach((img) => {
      if (img.adjuntoId) this.adjuntos.descargar(img.adjuntoId).subscribe((b) => {
        img.url = URL.createObjectURL(b);
        this.imagenes.set([...this.imagenes()]);
      });
    });
    this.reservable.set(a.reservable);
    this.intervaloMinutos.set(a.intervaloMinutos);
    this.limiteMensual.set(a.limiteMensual);
    this.maxReservas.set(a.maxReservasPorUnidad);
    this.tieneCosto.set(a.tieneCosto);
    this.tarifa.set(a.tarifa ?? null);
    this.requiereAprobacion.set(a.requiereAprobacion);
    this.reservableDesde.set(a.reservableDesde ?? this.reservableDesde());
    this.diasBloqueados.set([...a.diasBloqueados]);
    this.mensajeReserva.set(a.mensajeReserva ?? '');
    if (a.horarios.length) {
      this.horarios.set(DIAS_SEMANA.map((d) =>
        a.horarios.find((h) => h.dia === d.valor)
          ?? { dia: d.valor, cerrado: true, abreMin: 9 * 60, cierraMin: 18 * 60 }));
    }
  }

  // ---- navegación ----
  irA(n: number): void { if (n >= 1 && n <= 4) this.paso.set(n); }
  siguiente(): void { if (this.puedeAvanzar()) this.paso.update((p) => Math.min(4, p + 1)); }
  atras(): void { this.paso.update((p) => Math.max(1, p - 1)); }
  volver(): void { this.router.navigate(['/panel/amenidades/directorio']); }

  // ---- imágenes ----
  onImagenes(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    if (!input.files) return;
    for (const f of Array.from(input.files)) {
      if (f.type.startsWith('image/')) this.imagenes.update((l) => [...l, { file: f, url: URL.createObjectURL(f) }]);
    }
    input.value = '';
  }
  quitarImagen(i: number): void { this.imagenes.update((l) => l.filter((_, idx) => idx !== i)); }
  hacerPortada(i: number): void {
    this.imagenes.update((l) => { const c = [...l]; const [x] = c.splice(i, 1); c.unshift(x); return c; });
  }

  // ---- horario ----
  toggleDia(dia: number): void {
    this.horarios.update((l) => l.map((h) => h.dia === dia ? { ...h, cerrado: !h.cerrado } : h));
  }
  setHora(dia: number, campo: 'abreMin' | 'cierraMin', valor: number): void {
    this.horarios.update((l) => l.map((h) => h.dia === dia ? { ...h, [campo]: valor } : h));
  }
  durLabel(h: AmenidadHorario): string {
    const min = Math.max(0, h.cierraMin - h.abreMin);
    const hrs = min / 60;
    return `(${Number.isInteger(hrs) ? hrs : hrs.toFixed(1)}h)`;
  }

  // ---- calendario días bloqueados ----
  grillaMes = computed(() => {
    const base = this.mesCal();
    const primero = new Date(base.getFullYear(), base.getMonth(), 1);
    const arranque = new Date(primero);
    arranque.setDate(1 - ((primero.getDay() + 6) % 7)); // lunes primero
    const celdas: { fecha: string; dia: number; otroMes: boolean }[] = [];
    for (let i = 0; i < 42; i++) {
      const d = new Date(arranque);
      d.setDate(arranque.getDate() + i);
      celdas.push({
        fecha: d.toISOString().slice(0, 10),
        dia: d.getDate(),
        otroMes: d.getMonth() !== base.getMonth(),
      });
    }
    return celdas;
  });
  mesLabel = computed(() =>
    this.mesCal().toLocaleDateString('es-AR', { month: 'long', year: 'numeric' }));
  cambiarMes(delta: number): void {
    const d = new Date(this.mesCal());
    d.setMonth(d.getMonth() + delta);
    this.mesCal.set(d);
  }
  toggleBloqueado(fecha: string): void {
    this.diasBloqueados.update((l) => l.includes(fecha) ? l.filter((x) => x !== fecha) : [...l, fecha].sort());
  }
  fechaChip(f: string): string {
    return new Date(f + 'T00:00:00').toLocaleDateString('es-AR', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  // ---- documentos ----
  abrirDocModal(): void { this.docPendiente.set(null); this.docNombre.set(''); this.docModal.set(true); }
  onDocElegido(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const f = input.files?.[0];
    if (f && f.type === 'application/pdf' && f.size <= 10 * 1024 * 1024) {
      this.docPendiente.set(f);
      this.docNombre.set(f.name.replace(/\.pdf$/i, ''));
    } else if (f) {
      this.toasts.error('Solo PDF de hasta 10 MB.');
    }
    input.value = '';
  }
  confirmarDoc(): void {
    const f = this.docPendiente();
    if (f) {
      const nombre = (this.docNombre().trim() || f.name.replace(/\.pdf$/i, '')) + '.pdf';
      this.documentos.update((l) => [...l, new File([f], nombre, { type: 'application/pdf' })]);
    }
    this.docModal.set(false);
    this.docPendiente.set(null);
  }
  quitarDocumento(i: number): void { this.documentos.update((l) => l.filter((_, idx) => idx !== i)); }
  renombrarDocumento(i: number): void {
    const actual = this.documentos()[i];
    const nuevo = prompt('Nombre del documento', actual.name.replace(/\.pdf$/i, ''));
    if (nuevo && nuevo.trim()) {
      this.documentos.update((l) => l.map((f, idx) =>
        idx === i ? new File([f], nuevo.trim() + '.pdf', { type: 'application/pdf' }) : f));
    }
  }
  tamano(f: File): string { return (f.size / 1024 / 1024).toFixed(2) + ' MB'; }

  // ---- guardar ----
  private cuerpo(imagenesIds: string[]) {
    return {
      nombre: this.nombre().trim(),
      descripcion: this.descripcion().trim() || null,
      imagenesIds,
      reservable: this.reservable(),
      intervaloMinutos: this.intervaloMinutos(),
      limiteMensual: this.limiteMensual(),
      maxReservasPorUnidad: this.maxReservas(),
      tieneCosto: this.tieneCosto(),
      tarifa: this.tieneCosto() ? this.tarifa() : null,
      requiereAprobacion: this.requiereAprobacion(),
      reservableDesde: this.reservable() ? this.reservableDesde() : null,
      diasBloqueados: this.diasBloqueados(),
      mensajeReserva: this.mensajeReserva().trim() || null,
      horarios: this.horarios(),
    };
  }

  cerrarExito(): void {
    this.exito.set(false);
    this.router.navigate(['/panel/amenidades/directorio']);
  }

  guardar(): void {
    const cid = this.consorcios.activoId();
    if (!cid || !this.nombre().trim() || this.guardando()) return;
    this.guardando.set(true);

    const yaSubidas = this.imagenes().filter((i) => i.adjuntoId).map((i) => i.adjuntoId!);
    const nuevas = this.imagenes().filter((i) => i.file);
    const editId = this.editarId();

    const items = [
      ...nuevas.map((_, idx) => ({ label: idx === 0 && yaSubidas.length === 0 ? 'Imagen de portada' : 'Imagen', icono: '🖼', hecho: false })),
      ...this.documentos().map((d) => ({ label: d.name, icono: '📄', hecho: false })),
    ];
    this.progreso.set(items.length ? items : null);

    const marcar = (i: number) => this.progreso.update((p) => {
      if (!p) return p;
      const c = [...p]; if (c[i]) c[i] = { ...c[i], hecho: true }; return c;
    });

    const crear$ = editId
      ? this.api.actualizar(cid, editId, this.cuerpo(yaSubidas))
      : this.api.crear(cid, this.cuerpo([]));

    crear$.pipe(
      switchMap((amenidad) => {
        const subirImgs = nuevas.map((i, idx) =>
          this.adjuntos.subir('Amenidad', amenidad.id, i.file!).pipe(tap(() => marcar(idx))));
        const subirDocs = this.documentos().map((d, idx) =>
          this.adjuntos.subir('Amenidad', amenidad.id, d).pipe(tap(() => marcar(nuevas.length + idx))));
        const todo = [...subirImgs, ...subirDocs];
        return (todo.length ? forkJoin(todo) : of([])).pipe(
          switchMap((subidos) => {
            const idsImgNuevas = subidos.slice(0, subirImgs.length).map((s) => s.id);
            const ordenFinal = this.imagenes()
              .map((i) => i.adjuntoId ?? idsImgNuevas.shift())
              .filter((x): x is string => !!x);
            return this.api.actualizar(cid, amenidad.id, this.cuerpo(ordenFinal));
          }),
        );
      }),
    ).subscribe({
      next: () => {
        this.guardando.set(false);
        this.progreso.set(null);
        this.api.refrescarTotal(cid);
        this.exito.set(true);
      },
      error: (e) => {
        this.guardando.set(false);
        this.progreso.set(null);
        this.toasts.error(e?.error?.message ?? 'No se pudo guardar la amenidad.');
      },
    });
  }
}
