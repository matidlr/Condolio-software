import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { UnidadService } from '../../core/services/unidad.service';
import { ResidenteService } from '../../core/services/residente.service';
import { ToastService } from '../../core/services/toast.service';
import { TipoConsorcio, TIPOS_CONSORCIO } from '../../core/models/consorcio.models';

const PROVINCIAS_AR = [
  'Buenos Aires', 'Ciudad Autónoma de Buenos Aires', 'Catamarca', 'Chaco', 'Chubut', 'Córdoba',
  'Corrientes', 'Entre Ríos', 'Formosa', 'Jujuy', 'La Pampa', 'La Rioja', 'Mendoza', 'Misiones',
  'Neuquén', 'Río Negro', 'Salta', 'San Juan', 'San Luis', 'Santa Cruz', 'Santa Fe',
  'Santiago del Estero', 'Tierra del Fuego', 'Tucumán',
];

interface Datos {
  nombre: string;
  tipo: TipoConsorcio;
  provincia: string;
  ciudad: string;
  codigoPostal: string;
  direccion: string;
}

interface Edificio { nombre: string; pisos: number; unidades: number; }
interface CfgUnidades {
  varios: boolean;
  pisos: number;
  total: number;
  edificios: Edificio[];
}

@Component({
  selector: 'app-nueva-sociedad',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './nueva-sociedad.component.html',
  styleUrl: './nueva-sociedad.component.scss',
})
export class NuevaSociedadComponent {
  private consorcios = inject(ConsorcioService);
  private unidadesApi = inject(UnidadService);
  private residentesApi = inject(ResidenteService);
  private toasts = inject(ToastService);
  private router = inject(Router);
  private sanitizer = inject(DomSanitizer);

  tipos = TIPOS_CONSORCIO;
  provincias = PROVINCIAS_AR;

  readonly pasos = [
    { n: 1, label: 'Bienvenido' },
    { n: 2, label: 'Información básica' },
    { n: 3, label: 'Ubicación' },
    { n: 4, label: 'Unidades' },
    { n: 5, label: 'Residentes' },
    { n: 6, label: 'Listo' },
  ];

  paso = signal(1);
  guardando = signal(false);
  consorcioId = signal<string | null>(null);

  d = signal<Datos>({
    nombre: '', tipo: 'EdificioResidencial',
    provincia: '', ciudad: '', codigoPostal: '', direccion: '',
  });

  // ---- Ubicación / mapa ----
  coords = signal<{ lat: number; lon: number } | null>(null);
  geocodificando = signal(false);
  private geoTimer: any = null;

  set<K extends keyof Datos>(k: K, v: Datos[K]): void {
    this.d.update((x) => ({ ...x, [k]: v }));
    if (k === 'provincia' || k === 'ciudad' || k === 'direccion') this.programarGeocode();
  }

  private programarGeocode(): void {
    clearTimeout(this.geoTimer);
    if (!this.d().provincia) { this.coords.set(null); return; }
    this.geoTimer = setTimeout(() => this.geocodificar(), 700);
  }

  private async geocodificar(): Promise<void> {
    const d = this.d();
    const q = [d.direccion.trim(), d.ciudad.trim(), d.provincia, 'Argentina'].filter(Boolean).join(', ');
    this.geocodificando.set(true);
    try {
      const r = await fetch(
        `https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=ar&q=${encodeURIComponent(q)}`,
        { headers: { 'Accept-Language': 'es' } });
      const j = await r.json();
      this.coords.set(Array.isArray(j) && j.length
        ? { lat: +(+j[0].lat).toFixed(6), lon: +(+j[0].lon).toFixed(6) }
        : null);
    } catch {
      this.coords.set(null);
    } finally {
      this.geocodificando.set(false);
    }
  }

  tipoSel = computed(() => this.tipos.find((t) => t.value === this.d().tipo)!);
  esBarrio = computed(() => this.d().tipo === 'ResidencialPrivada');

  mapaUrl = computed<SafeResourceUrl | null>(() => {
    const c = this.coords();
    const d = this.d();
    let url: string;
    if (c) {
      const delta = d.direccion.trim() ? 0.004 : 0.12;
      url = `https://www.openstreetmap.org/export/embed.html?bbox=${c.lon - delta},${c.lat - delta},${c.lon + delta},${c.lat + delta}&layer=mapnik&marker=${c.lat},${c.lon}`;
    } else if (d.provincia) {
      url = `https://www.openstreetmap.org/export/embed.html?bbox=-73.6,-55.1,-53.6,-21.8&layer=mapnik`;
    } else {
      return null;
    }
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  });

  // ---- Unidades ----
  cfg = signal<CfgUnidades>({
    varios: false, pisos: 4, total: 16,
    edificios: [{ nombre: '', pisos: 1, unidades: 0 }, { nombre: '', pisos: 1, unidades: 0 }],
  });
  cfgConfirmada = signal(false);
  setCfg<K extends keyof CfgUnidades>(k: K, v: CfgUnidades[K]): void {
    this.cfg.update((x) => ({ ...x, [k]: v }));
    this.cfgConfirmada.set(false);
  }
  addEdificio(): void {
    this.cfg.update((x) => ({ ...x, edificios: [...x.edificios, { nombre: '', pisos: 1, unidades: 0 }] }));
    this.cfgConfirmada.set(false);
  }
  quitarEdificio(i: number): void {
    this.cfg.update((x) => ({ ...x, edificios: x.edificios.filter((_, j) => j !== i) }));
    this.cfgConfirmada.set(false);
  }
  setEdificio(i: number, k: keyof Edificio, v: string | number): void {
    this.cfg.update((x) => ({
      ...x,
      edificios: x.edificios.map((e, j) => j === i ? { ...e, [k]: v } : e),
    }));
    this.cfgConfirmada.set(false);
  }

  private nombresDeBloque(prefijo: string, pisos: number, unidades: number, seccion?: string) {
    const out: { nombre: string; piso: number; seccion?: string; tipo: 'Departamento' }[] = [];
    const pp = Math.max(1, pisos);
    const porPiso = Math.max(1, Math.ceil(Math.max(1, unidades) / pp));
    let cnt = 0;
    for (let f = 1; f <= pp && cnt < unidades; f++) {
      for (let u = 0; u < porPiso && cnt < unidades; u++) {
        out.push({ nombre: `${prefijo}${f}${String.fromCharCode(65 + u)}`, piso: f, seccion, tipo: 'Departamento' });
        cnt++;
      }
    }
    return out;
  }

  private plantilla(): { nombre: string; piso: number; seccion?: string }[] {
    const c = this.cfg();
    if (this.esBarrio()) {
      const total = c.varios
        ? c.edificios.reduce((s, e) => s + (+e.unidades || 0), 0)
        : Math.max(1, c.total);
      return Array.from({ length: Math.max(1, total) }, (_, i) => ({ nombre: `Casa ${i + 1}`, piso: 0 }));
    }
    if (!c.varios) return this.nombresDeBloque('', c.pisos, Math.max(1, c.total));
    return c.edificios.flatMap((e, i) => {
      const nombre = e.nombre.trim() || `Torre ${String.fromCharCode(65 + i)}`;
      return this.nombresDeBloque(`${nombre} `, e.pisos, +e.unidades || 0, nombre);
    });
  }
  cfgTotal = computed(() => { this.cfg(); return this.plantilla().length; });

  // ---- Grilla editable de unidades ----
  filas = signal<{ nombre: string; piso: number; seccion?: string }[]>([]);

  generarFilas(): void {
    this.filas.set(this.plantilla().map((p) => ({ ...p })));
    this.cfgConfirmada.set(true);
  }
  agregarFila(): void {
    this.filas.update((l) => [...l, { nombre: '', piso: 0 }]);
    this.cfgConfirmada.set(true);
  }
  quitarFila(i: number): void { this.filas.update((l) => l.filter((_, j) => j !== i)); }
  setFila(i: number, k: 'nombre' | 'piso', v: string | number): void {
    this.filas.update((l) => l.map((f, j) => j === i ? { ...f, [k]: v === '' && k === 'piso' ? 0 : v } : f));
  }
  filaValida(f: { nombre: string }): boolean { return f.nombre.trim().length > 0; }
  filasValidas = computed(() => {
    const l = this.filas();
    const vistos = new Set<string>();
    return l.length > 0 && l.every((f) => {
      const n = f.nombre.trim().toLowerCase();
      if (!n || vistos.has(n)) return false;
      vistos.add(n);
      return true;
    });
  });

  // ---- Residentes (import CSV) ----
  resFilas = signal<{ nombre: string; email: string; unidad: string; ok: boolean; motivo?: string }[]>([]);
  resError = signal<string | null>(null);
  resValidas = computed(() => this.resFilas().filter((r) => r.ok));
  quitarResFila(i: number): void { this.resFilas.update((l) => l.filter((_, j) => j !== i)); }

  descargarPlantillaResidentes(): void {
    const unidades = this.filas().map((f) => f.nombre.trim()).filter(Boolean);
    const filas = unidades.length ? unidades : ['1A', '2B'];
    const csv = 'nombre,correo,unidad\n' + filas.map((u) => `,,${u}`).join('\n') + '\n';
    const nombre = (this.d().nombre.trim() || 'Sociedad') + ' - Plantilla de Residentes.csv';
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
    a.download = nombre;
    a.click();
    URL.revokeObjectURL(a.href);
  }

  async elegirCsvResidentes(ev: Event): Promise<void> {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.resError.set(null);
    try {
      const texto = await file.text();
      this.parsearCsvResidentes(texto);
    } catch {
      this.resError.set('No se pudo leer el archivo.');
    }
  }

  private parsearCsvResidentes(texto: string): void {
    const lineas = texto.split(/\r?\n/).map((l) => l.trim()).filter(Boolean);
    if (lineas.length < 2) { this.resError.set('El CSV no tiene filas de datos.'); return; }
    const headers = lineas[0].toLowerCase().split(/[,;]/).map((h) => h.trim());
    const iNombre = headers.findIndex((h) => h.includes('nombre'));
    const iEmail = headers.findIndex((h) => h.includes('email') || h.includes('correo') || h.includes('mail'));
    const iUnidad = headers.findIndex((h) => h.includes('unidad') || h.includes('depto') || h.includes('casa'));
    if (iEmail < 0) { this.resError.set('El CSV necesita una columna de correo electrónico.'); return; }

    const unidades = new Set(this.filas().map((f) => f.nombre.trim().toLowerCase()));
    const vistos = new Set<string>();
    const filas = lineas.slice(1).map((linea) => {
      const cols = linea.split(/[,;]/).map((c) => c.trim());
      return {
        nombre: iNombre >= 0 ? (cols[iNombre] ?? '') : '',
        email: (cols[iEmail] ?? '').toLowerCase(),
        unidad: iUnidad >= 0 ? (cols[iUnidad] ?? '') : '',
      };
    })
      // ignora filas de la plantilla sin completar (sin nombre ni correo)
      .filter((r) => r.email || r.nombre)
      .map((r) => {
        let ok = true, motivo: string | undefined;
        if (!r.email.includes('@')) { ok = false; motivo = 'Falta el correo'; }
        else if (vistos.has(r.email)) { ok = false; motivo = 'Correo repetido'; }
        else if (r.unidad && unidades.size && !unidades.has(r.unidad.toLowerCase())) { ok = false; motivo = `Unidad "${r.unidad}" inexistente`; }
        vistos.add(r.email);
        return { ...r, ok, motivo };
      });

    this.resFilas.set(filas);
    if (filas.length === 0) this.resError.set('El CSV no tiene filas con datos de residentes cargados.');
    else if (filas.every((f) => !f.ok)) this.resError.set('Ninguna fila es válida. Revisá los correos y las unidades.');
  }

  // ---- Navegación ----
  puedeAvanzar = computed(() => {
    const d = this.d();
    switch (this.paso()) {
      case 2: return d.nombre.trim().length > 1;
      case 3: return !!(d.provincia && d.ciudad.trim() && d.codigoPostal.trim() && d.direccion.trim());
      case 4: return this.cfgConfirmada() && this.filasValidas();
      default: return true;
    }
  });

  progreso = computed(() => Math.round(((this.paso() - 1) / (this.pasos.length - 1)) * 100));

  // Sin volver atrás una vez creada la sociedad (paso >= 4).
  puedeVolver = computed(() => this.paso() > 1 && this.paso() < 4);
  atras(): void { if (this.puedeVolver()) this.paso.update((p) => p - 1); }

  siguiente(): void {
    if (!this.puedeAvanzar() || this.guardando()) return;
    switch (this.paso()) {
      case 3: this.crearConsorcio(); break;
      case 4: this.generarUnidades(); break;
      case 5: this.invitarYFinalizar(); break;
      default: if (this.paso() < 6) this.paso.update((p) => p + 1);
    }
  }

  private crearConsorcio(): void {
    const d = this.d();
    const c = this.coords();
    this.guardando.set(true);
    this.consorcios.crear({
      nombre: d.nombre.trim(), tipo: d.tipo, direccion: d.direccion.trim(),
      localidad: d.ciudad.trim(), provincia: d.provincia, pais: 'AR',
      codigoPostal: d.codigoPostal.trim() || null,
      latitud: c?.lat ?? null, longitud: c?.lon ?? null,
    }).subscribe({
      next: (creado) => { this.guardando.set(false); this.consorcioId.set(creado.id); this.paso.set(4); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudo crear la sociedad.'); },
    });
  }

  private generarUnidades(): void {
    const cid = this.consorcioId();
    if (!cid) { this.paso.set(5); return; }
    const payload = this.filas()
      .filter((f) => f.nombre.trim())
      .map((f) => ({
        nombre: f.nombre.trim(),
        piso: +f.piso || 0,
        tipo: 'Departamento',
        facturable: true,
        seccion: f.seccion ?? null,
      }));
    this.guardando.set(true);
    this.unidadesApi.crearLote(cid, payload as any, true).subscribe({
      next: (n) => { this.guardando.set(false); this.toasts.exito(`${n} unidades creadas`); this.paso.set(5); },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudieron crear las unidades.'); },
    });
  }

  omitirResidentes(): void { this.paso.set(6); }

  private invitarYFinalizar(): void {
    const cid = this.consorcioId();
    const validas = this.resValidas();
    if (!cid || validas.length === 0) { this.paso.set(6); return; }
    this.guardando.set(true);
    this.residentesApi.invitarLote(cid, validas.map((r) => ({
      nombre: r.nombre, email: r.email, telefono: '', unidad: r.unidad, rol: 'Propietario',
    })), true).subscribe({
      next: (res) => {
        this.guardando.set(false);
        this.toasts.exito(`${res.enviadas} invitaciones enviadas` + (res.fallidas ? `, ${res.fallidas} con error` : ''));
        this.paso.set(6);
      },
      error: (e) => { this.guardando.set(false); this.toasts.error(e?.error?.message ?? 'No se pudieron enviar las invitaciones.'); },
    });
  }

  salir(): void { this.router.navigate([this.consorcioId() ? '/panel/unidades' : '/panel/inicio']); }

  irAlPanel(): void { this.router.navigate(['/panel/unidades']); }
}
