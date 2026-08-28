import { Component, computed, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { ResidenteService } from '../../core/services/residente.service';
import { UnidadService } from '../../core/services/unidad.service';
import { ToastService } from '../../core/services/toast.service';
import { RolUnidad, Unidad } from '../../core/models/consorcio.models';

interface FilaImport {
  key: string;
  nombre: string;
  email: string;
  telefono: string;
  unidad: string;
  rol: RolUnidad;
}

@Component({
  selector: 'app-invitar-residente',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './invitar-residente.component.html',
  styleUrl: './directorio.component.scss',
})
export class InvitarResidenteComponent {
  hecho = output<void>();
  cerrar = output<void>();

  private fb = inject(FormBuilder);
  private consorcios = inject(ConsorcioService);
  private api = inject(ResidenteService);
  private unidadesApi = inject(UnidadService);
  private toasts = inject(ToastService);

  private consorcioId = computed(() => this.consorcios.activoId());

  tab = signal<'agregar' | 'importar'>('agregar');
  guardando = signal(false);
  error = signal<string | null>(null);
  unidades = signal<Unidad[]>([]);
  unidadIdSel = signal('');

  form = this.fb.nonNullable.group({
    nombre: [''],
    email: ['', [Validators.required, Validators.email]],
    unidadId: [''],
    rol: ['Propietario' as RolUnidad],
  });

  // wizard
  impPaso = signal<1 | 2 | 3>(1);
  impFilas = signal<FilaImport[]>([]);
  impSel = signal<Set<string>>(new Set());
  impBusqueda = signal('');
  impNotificar = signal(false);
  impResultado = signal<{ enviadas: number; fallidas: number; filas: { email: string; ok: boolean; motivo?: string | null }[] } | null>(null);

  ngOnInit(): void {
    const id = this.consorcioId();
    if (id) this.unidadesApi.listar(id).subscribe((u) => this.unidades.set(u));
  }

  unidadSeleccionada = computed(() =>
    this.unidades().find((u) => u.id === this.unidadIdSel()) ?? null);

  onUnidad(id: string): void {
    this.unidadIdSel.set(id);
    this.form.controls.unidadId.setValue(id);
  }

  irTab(t: 'agregar' | 'importar'): void {
    this.tab.set(t);
    this.error.set(null);
    if (t === 'agregar') {
      this.form.reset({ nombre: '', email: '', unidadId: '', rol: 'Propietario' });
      this.unidadIdSel.set('');
    } else {
      this.impPaso.set(1);
      this.impFilas.set([]);
      this.impResultado.set(null);
      this.impNotificar.set(false);
    }
  }

  enviarInvitacion(): void {
    const cid = this.consorcioId();
    if (!cid || this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    this.guardando.set(true);
    this.error.set(null);
    this.api.invitar(cid, {
      email: v.email.trim(), nombre: v.nombre.trim() || null,
      unidadId: v.unidadId || null, rol: v.rol,
    }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.toasts.exito('Invitación enviada con éxito');
        this.hecho.emit();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo enviar la invitación.'); this.guardando.set(false); },
    });
  }

  // ---- Importar ----
  descargarPlantilla(): void {
    const cons = this.consorcios.activo?.nombre ?? 'consorcio';
    const hoy = new Date().toISOString().slice(0, 10);
    const csv =
      '# Completá los datos de cada residente,,,, # Roles: owner, tenant, admin\r\n' +
      'nombre,correo,telefono,unidad,rol\r\n,,,,\r\n,,,,\r\n,,,,\r\n';
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Plantilla Invitar Residentes - ${cons} - ${hoy}.csv`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  onArchivo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      const lineas = String(reader.result ?? '')
        .split(/\r?\n/).map((l) => l.trim()).filter((l) => l && !l.startsWith('#'));
      if (lineas.length < 2) { this.error.set('El archivo no tiene filas.'); return; }
      const parse = (l: string) => l.split(',').map((c) => c.replace(/^"|"$/g, '').trim());
      const cab = parse(lineas[0]).map((h) => h.toLowerCase());
      const idx = (...a: string[]) => a.map((x) => cab.indexOf(x)).find((i) => i >= 0) ?? -1;
      const iN = idx('nombre'), iE = idx('correo', 'email'), iT = idx('telefono', 'teléfono');
      const iU = idx('unidad'), iR = idx('rol');
      const val = (c: string[], i: number) => (i >= 0 ? c[i] ?? '' : '');
      const mapRol = (r: string): RolUnidad => {
        const x = r.trim().toLowerCase();
        if (x === 'tenant' || x === 'inquilino') return 'Inquilino';
        if (x === 'admin' || x === 'gestor') return 'Gestor';
        return 'Propietario';
      };
      const filas: FilaImport[] = lineas.slice(1).map((l, n) => {
        const c = parse(l);
        return { key: 'r' + n, nombre: val(c, iN), email: val(c, iE), telefono: val(c, iT), unidad: val(c, iU), rol: mapRol(val(c, iR)) };
      }).filter((f) => f.email || f.nombre);
      if (!filas.length) { this.error.set('No se encontraron residentes en el archivo.'); return; }
      this.error.set(null);
      this.impFilas.set(filas);
      this.impSel.set(new Set(filas.filter((f) => this.filaValida(f) === null).map((f) => f.key)));
      this.impPaso.set(2);
    };
    reader.readAsText(file);
  }

  filaValida(f: FilaImport): string | null {
    if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(f.email)) return 'Correo inválido';
    const tel = f.telefono.replace(/[\s()+-]/g, '');
    if (tel && !/^\d{8,15}$/.test(tel)) return 'Número de teléfono inválido';
    if (f.unidad && !this.unidades().some((u) => u.nombre.toLowerCase() === f.unidad.toLowerCase()))
      return `La unidad "${f.unidad}" no existe`;
    return null;
  }

  editarFila(key: string, campo: keyof FilaImport, valor: string): void {
    this.impFilas.update((fs) => fs.map((f) => (f.key === key ? { ...f, [campo]: valor } : f)));
    const fila = this.impFilas().find((f) => f.key === key);
    if (fila) {
      const ok = this.filaValida(fila) === null;
      this.impSel.update((s) => { const n = new Set(s); ok ? n.add(key) : n.delete(key); return n; });
    }
  }

  quitarFila(key: string): void {
    this.impFilas.update((fs) => fs.filter((f) => f.key !== key));
    this.impSel.update((s) => { const n = new Set(s); n.delete(key); return n; });
  }

  toggleFila(key: string): void {
    this.impSel.update((s) => { const n = new Set(s); n.has(key) ? n.delete(key) : n.add(key); return n; });
  }

  impVisibles = computed(() => {
    const q = this.impBusqueda().trim().toLowerCase();
    return this.impFilas().filter((f) => !q
      || f.nombre.toLowerCase().includes(q) || f.email.toLowerCase().includes(q));
  });

  impSeleccionadasValidas = computed(() =>
    this.impFilas().filter((f) => this.impSel().has(f.key) && this.filaValida(f) === null));

  confirmarImport(): void {
    const cid = this.consorcioId();
    const filas = this.impSeleccionadasValidas();
    if (!cid || !filas.length) return;
    this.guardando.set(true);
    this.error.set(null);
    this.api.invitarLote(cid, filas.map((f) => ({
      nombre: f.nombre, email: f.email, telefono: f.telefono, unidad: f.unidad, rol: f.rol,
    })), this.impNotificar()).subscribe({
      next: (res) => {
        this.guardando.set(false);
        this.impResultado.set(res);
        this.toasts.exito(`${res.enviadas} residente(s) importado(s)`);
        this.hecho.emit();
      },
      error: (e) => { this.error.set(e?.error?.message ?? 'No se pudo importar.'); this.guardando.set(false); },
    });
  }
}
