import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { ToastService } from '../../core/services/toast.service';
import {
  GuardarUnidad,
  OCUPACIONES,
  TipoOcupacion,
  TipoUnidad,
  Unidad,
} from '../../core/models/consorcio.models';

type SortKey = 'nombre' | 'piso' | 'cuotaMantenimiento' | 'propietarios' | 'inquilinos' | 'gestores';
type Filtro = 'Todas' | 'SinResidentes' | 'SinPropietarios' | 'SinInquilinos' | 'SinCuota';

const TIPOS: TipoUnidad[] = ['Departamento', 'Local', 'Cochera', 'Baulera'];

interface FilaMasiva {
  id: string;
  nombre: string;
  tipo: TipoUnidad;
  ocupacion: TipoOcupacion;
  piso: number;
  areaM2: number | null;
  coeficiente: number | null;
  cuotaMantenimiento: number | null;
  seccion: string | null;
}

type ColMasiva = 'tipo' | 'piso' | 'area' | 'indiviso' | 'ocupacion';

interface ImportPreviewRow {
  status: 'new' | 'updated' | 'remove';
  nombre: string;
  cuota: number | null;
  indiviso: number | null;
  piso: number;
  cambios: string[];
}

const FILTROS: { value: Filtro; label: string }[] = [
  { value: 'Todas', label: 'Todas' },
  { value: 'SinResidentes', label: 'Sin residentes' },
  { value: 'SinPropietarios', label: 'Sin propietarios' },
  { value: 'SinInquilinos', label: 'Sin inquilinos' },
  { value: 'SinCuota', label: 'Sin cuota' },
];

@Component({
  selector: 'app-unidades',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './unidades.component.html',
  styleUrl: './unidades.component.scss',
  host: { '(document:click)': 'onDocClick($event)' },
})
export class UnidadesComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private toasts = inject(ToastService);
  consorcios = inject(ConsorcioService);
  private unidadesApi = inject(UnidadService);

  readonly tipos = TIPOS;
  readonly filtros = FILTROS;

  unidades = signal<Unidad[]>([]);
  cargando = signal(false);
  error = signal<string | null>(null);

  busqueda = signal('');
  filtro = signal<Filtro>('Todas');
  sortKey = signal<SortKey>('nombre');
  sortDir = signal<1 | -1>(1);
  seleccion = signal<Set<string>>(new Set());
  menuOpciones = signal(false);
  menuFiltro = signal(false);

  // ---- Importación ----
  ieTab = signal<'importar' | 'exportar'>('importar');
  importFilas = signal<GuardarUnidad[]>([]);
  importPreview = signal<ImportPreviewRow[] | null>(null);
  importConfirmar = signal(false);
  importEntiendo = signal(false);

  modo = signal<'cerrado' | 'una' | 'varias' | 'importar' | 'masivo'>('cerrado');
  tabModal = signal<'info' | 'cuota'>('info');

  // ---- Edición masiva ----
  readonly ocupaciones = OCUPACIONES;
  readonly colsMasivas: { key: ColMasiva; label: string }[] = [
    { key: 'tipo', label: 'Tipo' },
    { key: 'piso', label: 'Piso' },
    { key: 'area', label: 'Área' },
    { key: 'indiviso', label: '% Indiviso' },
    { key: 'ocupacion', label: 'Tipo de Ocupación' },
  ];
  masivoFilas = signal<FilaMasiva[]>([]);
  masivoOriginal = new Map<string, string>();
  masivoCols = signal<Set<ColMasiva>>(new Set(['tipo', 'piso', 'area', 'indiviso', 'ocupacion']));
  menuColumnas = signal(false);
  editando = signal<Unidad | null>(null);
  guardando = signal(false);

  // ---- Formularios ----
  formUnidad = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    tipo: ['Departamento' as TipoUnidad, [Validators.required]],
    piso: [0],
    areaM2: [null as number | null],
    coeficiente: [null as number | null],
    seccion: [''],
    cuotaMantenimiento: [null as number | null],
    facturable: [true],
  });

  formLote = this.fb.nonNullable.group({
    texto: ['', [Validators.required]],
    tipo: ['Departamento' as TipoUnidad],
  });

  formConsorcio = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    direccion: ['', [Validators.required]],
    localidad: [''],
    provincia: [''],
  });

  // ---- Derivados ----
  consorcioId = computed(() => this.consorcios.activoId());

  faltanCuotas = computed(() => this.unidades().some((u) => u.cuotaMantenimiento == null));

  private pasaFiltro(u: Unidad): boolean {
    switch (this.filtro()) {
      case 'SinResidentes': return u.propietarios.length + u.inquilinos.length === 0;
      case 'SinPropietarios': return u.propietarios.length === 0;
      case 'SinInquilinos': return u.inquilinos.length === 0;
      case 'SinCuota': return u.cuotaMantenimiento == null;
      default: return true;
    }
  }

  private valorSort(u: Unidad, key: SortKey): string | number {
    switch (key) {
      case 'nombre': return u.nombre;
      case 'piso': return u.piso;
      case 'cuotaMantenimiento': return u.cuotaMantenimiento ?? -1;
      case 'propietarios': return u.propietarios.length;
      case 'inquilinos': return u.inquilinos.length;
      case 'gestores': return u.gestores.length;
    }
  }

  visibles = computed(() => {
    const q = this.busqueda().trim().toLowerCase();
    const key = this.sortKey();
    const dir = this.sortDir();
    return this.unidades()
      .filter((u) => this.pasaFiltro(u))
      .filter((u) => !q || u.nombre.toLowerCase().includes(q))
      .sort((a, b) => {
        const va = this.valorSort(a, key);
        const vb = this.valorSort(b, key);
        if (typeof va === 'string' && typeof vb === 'string') {
          return va.localeCompare(vb, 'es', { numeric: true }) * dir;
        }
        return ((va as number) - (vb as number)) * dir;
      });
  });

  etiquetaFiltro = computed(() =>
    FILTROS.find((f) => f.value === this.filtro())?.label ?? 'Todas');

  todasSeleccionadas = computed(() =>
    this.visibles().length > 0 && this.visibles().every((u) => this.seleccion().has(u.id)),
  );

  constructor() {
    effect(() => {
      const id = this.consorcioId();
      if (id) this.cargar(id);
      else this.unidades.set([]);
    });
  }

  cargar(consorcioId: string): void {
    this.cargando.set(true);
    this.error.set(null);
    this.unidadesApi.listar(consorcioId).subscribe({
      next: (list) => {
        this.unidades.set(list);
        this.seleccion.set(new Set());
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No pudimos cargar las unidades.');
        this.cargando.set(false);
      },
    });
  }

  refrescar(): void {
    const id = this.consorcioId();
    if (id) this.cargar(id);
  }

  onDocClick(e: MouseEvent): void {
    const t = e.target as HTMLElement;
    if (!t.closest('.u-dropdown')) {
      this.menuOpciones.set(false);
      this.menuFiltro.set(false);
    }
    if (!t.closest('.u-cols')) this.menuColumnas.set(false);
  }

  // ---- Edición masiva ----
  abrirMasivo(): void {
    this.menuOpciones.set(false);
    this.error.set(null);
    const filas: FilaMasiva[] = this.unidades().map((u) => ({
      id: u.id,
      nombre: u.nombre,
      tipo: u.tipo,
      ocupacion: u.ocupacion,
      piso: u.piso,
      areaM2: u.areaM2 ?? null,
      coeficiente: u.coeficiente ?? null,
      cuotaMantenimiento: u.cuotaMantenimiento ?? null,
      seccion: u.seccion ?? null,
    }));
    this.masivoFilas.set(filas);
    this.masivoOriginal = new Map(filas.map((f) => [f.id, JSON.stringify(f)]));
    this.modo.set('masivo');
  }

  masivoCambios = computed(() =>
    this.masivoFilas().filter((f) => this.masivoOriginal.get(f.id) !== JSON.stringify(f)).length);

  colVisible(k: ColMasiva): boolean {
    return this.masivoCols().has(k);
  }

  filaSucia(f: FilaMasiva): boolean {
    return this.masivoOriginal.get(f.id) !== JSON.stringify(f);
  }

  celdaSucia(f: FilaMasiva, campo: keyof FilaMasiva): boolean {
    const orig = this.masivoOriginal.get(f.id);
    if (!orig) return false;
    return JSON.parse(orig)[campo] !== f[campo];
  }

  toggleCol(k: ColMasiva): void {
    const s = new Set(this.masivoCols());
    s.has(k) ? s.delete(k) : s.add(k);
    this.masivoCols.set(s);
  }

  editarCelda(id: string, campo: keyof FilaMasiva, valor: string): void {
    this.masivoFilas.update((filas) =>
      filas.map((f) => {
        if (f.id !== id) return f;
        const num = valor === '' ? null : Number(valor);
        switch (campo) {
          case 'nombre': return { ...f, nombre: valor };
          case 'tipo': return { ...f, tipo: valor as TipoUnidad };
          case 'ocupacion': return { ...f, ocupacion: valor as TipoOcupacion };
          case 'piso': return { ...f, piso: Number(valor) || 0 };
          case 'areaM2': return { ...f, areaM2: num };
          case 'coeficiente': return { ...f, coeficiente: num };
          case 'cuotaMantenimiento': return { ...f, cuotaMantenimiento: num };
          default: return f;
        }
      }),
    );
  }

  guardarMasivo(): void {
    const id = this.consorcioId();
    if (!id || this.masivoCambios() === 0) return;
    this.guardando.set(true);
    this.error.set(null);
    this.unidadesApi.editarMasivo(id, this.masivoFilas()).subscribe({
      next: (n) => {
        this.guardando.set(false);
        this.toasts.exito(`${n} unidad(es) actualizada(s) exitosamente`);
        this.cerrarModal();
        this.cargar(id);
        this.consorcios.cargar().subscribe();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo guardar.'); this.guardando.set(false); },
    });
  }

  // ---- Importar / Exportar ----
  abrirImportar(): void {
    this.menuOpciones.set(false);
    this.error.set(null);
    this.ieTab.set('importar');
    this.importFilas.set([]);
    this.importPreview.set(null);
    this.importConfirmar.set(false);
    this.importEntiendo.set(false);
    this.modo.set('importar');
  }

  private resumenImport = computed(() => {
    const rows = this.importPreview() ?? [];
    return {
      nuevas: rows.filter((r) => r.status === 'new').length,
      actualizadas: rows.filter((r) => r.status === 'updated').length,
      aQuitar: rows.filter((r) => r.status === 'remove').length,
      totalDespues: rows.filter((r) => r.status !== 'remove').length,
    };
  });
  resumen = this.resumenImport;

  private toCsv(rows: string[][]): string {
    return rows.map((r) => r.map((c) => `"${(c ?? '').replace(/"/g, '""')}"`).join(',')).join('\r\n');
  }

  private nombreArchivo(): string {
    const cons = this.consorcios.activo?.nombre ?? 'consorcio';
    const hoy = new Date().toISOString().slice(0, 10);
    return `Plantilla Unidades - ${cons} - ${hoy}.csv`;
  }

  descargarPlantilla(): void {
    const csv = this.toCsv([
      ['nombre', 'cuota mensual', '% indiviso', 'saldo inicial', 'piso'],
      ['1A', '25000', '2.5', '0', '1'],
      ['1B', '25000', '2.5', '0', '1'],
      ['Local 1', '40000', '5', '0', '0'],
    ]);
    this.bajarArchivo(this.nombreArchivo(), csv);
  }

  exportarCsv(): void {
    const ordenadas = [...this.unidades()].sort((a, b) =>
      a.nombre.localeCompare(b.nombre, 'es', { numeric: true }));
    const rows = [
      ['nombre', 'cuota mensual', '% indiviso', 'saldo inicial', 'piso'],
      ...ordenadas.map((u) => [
        u.nombre,
        u.cuotaMantenimiento != null ? String(u.cuotaMantenimiento) : '',
        u.coeficiente != null ? String(u.coeficiente) : '',
        '0',
        String(u.piso),
      ]),
    ];
    this.bajarArchivo(this.nombreArchivo(), this.toCsv(rows));
  }

  private bajarArchivo(nombre: string, contenido: string): void {
    const blob = new Blob(['﻿' + contenido], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombre;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  importarCsv(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => {
      const texto = String(reader.result ?? '');
      const filas = texto.split(/\r?\n/).map((l) => l.trim()).filter(Boolean);
      if (filas.length < 2) { this.error.set('El archivo no tiene filas.'); return; }

      const parseLinea = (l: string): string[] => {
        const out: string[] = [];
        let cur = '';
        let enComillas = false;
        for (let i = 0; i < l.length; i++) {
          const ch = l[i];
          if (enComillas) {
            if (ch === '"' && l[i + 1] === '"') { cur += '"'; i++; }
            else if (ch === '"') enComillas = false;
            else cur += ch;
          } else if (ch === '"') enComillas = true;
          else if (ch === ',') { out.push(cur.trim()); cur = ''; }
          else cur += ch;
        }
        out.push(cur.trim());
        return out;
      };

      const cab = parseLinea(filas[0]).map((h) => h.toLowerCase());
      const idx = (...alias: string[]) => alias.map((a) => cab.indexOf(a)).find((i) => i >= 0) ?? -1;
      const iNombre = idx('nombre');
      const iCuota = idx('cuota mensual', 'cuota', 'cuota_mensual');
      const iIndiviso = idx('% indiviso', 'indiviso', 'coeficiente');
      const iPiso = idx('piso');
      const val = (c: string[], i: number) => (i >= 0 ? c[i] : undefined);

      const unidades: GuardarUnidad[] = filas.slice(1).map((l) => {
        const c = parseLinea(l);
        return {
          nombre: val(c, iNombre) ?? '',
          piso: Number(val(c, iPiso)) || 0,
          tipo: 'Departamento' as TipoUnidad,
          cuotaMantenimiento: val(c, iCuota) ? Number(val(c, iCuota)) : null,
          coeficiente: val(c, iIndiviso) ? Number(val(c, iIndiviso)) : null,
          facturable: true,
          seccion: null,
        };
      }).filter((u) => u.nombre);

      if (!unidades.length) { this.error.set('No se encontraron unidades válidas.'); return; }

      this.error.set(null);
      this.importFilas.set(unidades);
      this.importPreview.set(this.construirPreview(unidades));
    };
    reader.readAsText(file);
  }

  private construirPreview(entrantes: GuardarUnidad[]): ImportPreviewRow[] {
    const actuales = new Map(this.unidades().map((u) => [u.nombre.toLowerCase(), u]));
    const entrantesSet = new Set(entrantes.map((u) => u.nombre.toLowerCase()));
    const fmt = (n: number | null | undefined) => (n == null ? '—' : String(n));

    const rows: ImportPreviewRow[] = entrantes.map((e) => {
      const prev = actuales.get(e.nombre.toLowerCase());
      if (!prev) {
        return { status: 'new', nombre: e.nombre, cuota: e.cuotaMantenimiento ?? null, indiviso: e.coeficiente ?? null, piso: e.piso, cambios: [] };
      }
      const cambios: string[] = [];
      if ((prev.cuotaMantenimiento ?? null) !== (e.cuotaMantenimiento ?? null))
        cambios.push(`Cuota: $${fmt(prev.cuotaMantenimiento)} → $${fmt(e.cuotaMantenimiento)}`);
      if ((prev.coeficiente ?? null) !== (e.coeficiente ?? null))
        cambios.push(`Indiviso: ${fmt(prev.coeficiente)}% → ${fmt(e.coeficiente)}%`);
      if (prev.piso !== e.piso) cambios.push(`Piso: ${prev.piso} → ${e.piso}`);
      return {
        status: cambios.length ? 'updated' : 'updated',
        nombre: e.nombre, cuota: e.cuotaMantenimiento ?? null, indiviso: e.coeficiente ?? null, piso: e.piso,
        cambios: cambios.length ? cambios : ['Sin cambios'],
      };
    });

    for (const u of this.unidades()) {
      if (!entrantesSet.has(u.nombre.toLowerCase())) {
        rows.push({ status: 'remove', nombre: u.nombre, cuota: u.cuotaMantenimiento ?? null, indiviso: u.coeficiente ?? null, piso: u.piso, cambios: [] });
      }
    }
    return rows;
  }

  confirmarImport(): void {
    const id = this.consorcioId();
    const filas = this.importFilas();
    if (!id || !filas.length) return;

    this.guardando.set(true);
    this.error.set(null);
    this.unidadesApi.importar(id, filas, true).subscribe({
      next: (res) => {
        this.guardando.set(false);
        this.toasts.exito(
          res.eliminadas > 0
            ? `Se eliminaron ${res.eliminadas} unidades y se crearon ${res.nuevas} unidades nuevas`
            : `Se agregaron ${res.nuevas} unidades al consorcio`,
        );
        this.cerrarModal();
        this.cargar(id);
        this.consorcios.cargar().subscribe();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo importar.'); this.guardando.set(false); },
    });
  }

  ordenarPor(key: SortKey): void {
    if (this.sortKey() === key) this.sortDir.set(this.sortDir() === 1 ? -1 : 1);
    else {
      this.sortKey.set(key);
      this.sortDir.set(1);
    }
  }

  toggleTodas(): void {
    const s = new Set(this.seleccion());
    if (this.todasSeleccionadas()) this.visibles().forEach((u) => s.delete(u.id));
    else this.visibles().forEach((u) => s.add(u.id));
    this.seleccion.set(s);
  }

  toggleUna(id: string): void {
    const s = new Set(this.seleccion());
    s.has(id) ? s.delete(id) : s.add(id);
    this.seleccion.set(s);
  }

  // ---- Alta / edición ----
  private resetUnidad(u?: Unidad): void {
    this.formUnidad.reset({
      nombre: u?.nombre ?? '',
      tipo: u?.tipo ?? 'Departamento',
      piso: u?.piso ?? 0,
      areaM2: u?.areaM2 ?? null,
      coeficiente: u?.coeficiente ?? null,
      seccion: u?.seccion ?? '',
      cuotaMantenimiento: u?.cuotaMantenimiento ?? null,
      facturable: u?.facturable ?? true,
    });
  }

  abrirNueva(): void {
    this.editando.set(null);
    this.resetUnidad();
    this.tabModal.set('info');
    this.error.set(null);
    this.modo.set('una');
  }

  abrirVarias(): void {
    this.formLote.reset({ texto: '', tipo: 'Departamento' });
    this.error.set(null);
    this.modo.set('varias');
  }

  abrirFicha(u: Unidad): void {
    this.router.navigate(['/panel/unidades', u.id]);
  }

  editar(u: Unidad): void {
    this.editando.set(u);
    this.resetUnidad(u);
    this.tabModal.set('info');
    this.error.set(null);
    this.modo.set('una');
  }

  cerrarModal(): void {
    this.modo.set('cerrado');
    this.guardando.set(false);
  }

  guardarUnidad(): void {
    const id = this.consorcioId();
    if (!id || this.formUnidad.invalid) {
      this.formUnidad.markAllAsTouched();
      return;
    }
    const v = this.formUnidad.getRawValue();
    const body: GuardarUnidad = {
      nombre: v.nombre, tipo: v.tipo, piso: v.piso,
      areaM2: v.areaM2, coeficiente: v.coeficiente,
      seccion: v.seccion?.trim() || null,
      cuotaMantenimiento: v.cuotaMantenimiento, facturable: v.facturable,
    };
    this.guardando.set(true);
    this.error.set(null);

    const editando = this.editando();
    const op = editando
      ? this.unidadesApi.actualizar(id, editando.id, body)
      : this.unidadesApi.crear(id, body);

    op.subscribe({
      next: () => {
        this.toasts.exito(editando ? 'Unidad actualizada exitosamente' : 'Unidad creada exitosamente');
        this.cerrarModal();
        this.cargar(id);
        this.consorcios.cargar().subscribe();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo guardar la unidad.'); this.guardando.set(false); },
    });
  }

  guardarLote(): void {
    const id = this.consorcioId();
    if (!id || this.formLote.invalid) return;

    const tipo = this.formLote.getRawValue().tipo;
    const filas = this.formLote.getRawValue().texto
      .split('\n')
      .map((l) => l.trim())
      .filter(Boolean)
      .map<GuardarUnidad>((l) => {
        const [nombre, piso, cuota] = l.split(',').map((p) => p.trim());
        return {
          nombre,
          piso: Number(piso) || 0,
          tipo,
          cuotaMantenimiento: cuota ? Number(cuota) : null,
          coeficiente: null,
          facturable: true,
        };
      })
      .filter((u) => u.nombre);

    if (!filas.length) { this.error.set('Escribí al menos una unidad.'); return; }

    this.guardando.set(true);
    this.error.set(null);
    this.unidadesApi.crearLote(id, filas).subscribe({
      next: (n) => {
        this.toasts.exito(`${n} unidad${n === 1 ? '' : 'es'} agregada${n === 1 ? '' : 's'}`);
        this.cerrarModal();
        this.cargar(id);
        this.consorcios.cargar().subscribe();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo agregar el lote.'); this.guardando.set(false); },
    });
  }

  eliminar(u: Unidad): void {
    const id = this.consorcioId();
    if (!id || !confirm(`¿Eliminar la unidad "${u.nombre}"?`)) return;
    this.unidadesApi.eliminar(id, u.id).subscribe({
      next: () => {
        this.toasts.exito('Unidad eliminada');
        this.cargar(id);
        this.consorcios.cargar().subscribe();
      },
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo eliminar.'),
    });
  }

  // ---- Alta de consorcio (cuando no hay ninguno) ----
  crearConsorcio(): void {
    if (this.formConsorcio.invalid) {
      this.formConsorcio.markAllAsTouched();
      return;
    }
    this.guardando.set(true);
    this.consorcios.crear(this.formConsorcio.getRawValue()).subscribe({
      next: () => { this.toasts.exito('Consorcio creado exitosamente'); this.guardando.set(false); },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo crear el consorcio.'); this.guardando.set(false); },
    });
  }
}
