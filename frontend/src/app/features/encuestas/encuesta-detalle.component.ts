import { Component, computed, effect, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ConsorcioService } from '../../core/services/consorcio.service';
import { EncuestaService } from '../../core/services/encuesta.service';
import { ToastService } from '../../core/services/toast.service';
import {
  EncuestaDetalle, EstadoEncuesta, ICON_CAT_ENCUESTA, LABEL_CAT_ENCUESTA,
  LABEL_MODO_VOTO, META_ESTADO,
} from '../../core/models/encuesta.models';

type ExportTipo = 'basico' | 'detallado' | 'agrupado';

@Component({
  selector: 'app-encuesta-detalle',
  standalone: true,
  imports: [DatePipe, DecimalPipe],
  templateUrl: './encuesta-detalle.component.html',
  styleUrl: './encuesta-detalle.component.scss',
  host: { '(document:click)': 'exportAbierto.set(false)' },
})
export class EncuestaDetalleComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private consorcios = inject(ConsorcioService);
  private api = inject(EncuestaService);
  private toasts = inject(ToastService);

  metaEstado = META_ESTADO;
  iconCat = ICON_CAT_ENCUESTA;
  labelCat = LABEL_CAT_ENCUESTA;
  labelModo = LABEL_MODO_VOTO;

  cargando = signal(true);
  detalle = signal<EncuestaDetalle | null>(null);

  unidadesAbierto = signal(false);
  exportAbierto = signal(false);
  confirmCerrar = signal(false);
  confirmEliminar = signal(false);
  procesando = signal(false);

  private id = signal<string>(this.route.snapshot.paramMap.get('id') ?? '');
  private consorcioId = computed(() => this.consorcios.activoId());

  encuesta = computed(() => this.detalle()?.encuesta ?? null);

  esPorUnidad = computed(() => {
    const m = this.encuesta()?.modoVotacion;
    return m === 'PorUnidad' || m === 'PonderadoPorAlicuota';
  });

  tasaParticipacion = computed(() => {
    const d = this.detalle();
    if (!d || d.unidadesTotales === 0) return 0;
    return Math.round((d.encuesta.totalVotantes / d.unidadesTotales) * 100);
  });

  tiempoRestante = computed(() => {
    const e = this.encuesta();
    if (!e) return '—';
    if (e.estado === 'Cerrada') return 'Finalizada';
    if (!e.cierreUtc) return 'Sin límite';
    const ms = new Date(e.cierreUtc).getTime() - Date.now();
    if (ms <= 0) return 'Finalizada';
    const horas = Math.floor(ms / 3_600_000);
    const dias = Math.floor(horas / 24);
    const h = horas % 24;
    if (dias > 0) return `${dias}d ${h}h`;
    if (horas > 0) return `${horas}h`;
    return `${Math.max(1, Math.floor(ms / 60_000))}m`;
  });

  unidadLabel(votos: number): string {
    if (this.esPorUnidad()) return `${votos} unidad${votos === 1 ? '' : 'es'}`;
    return `${votos} voto${votos === 1 ? '' : 's'}`;
  }

  ganadoraId = computed(() => {
    const ops = this.encuesta()?.opciones ?? [];
    if (!ops.length) return null;
    const max = Math.max(...ops.map((o) => o.votos));
    return max > 0 ? ops.find((o) => o.votos === max)?.id ?? null : null;
  });

  constructor() {
    effect(() => { if (this.consorcioId()) this.cargar(); });
  }

  private cargar(): void {
    const cid = this.consorcioId();
    if (!cid || !this.id()) return;
    this.cargando.set(true);
    this.api.obtener(cid, this.id()).subscribe({
      next: (d) => { this.detalle.set(d); this.cargando.set(false); },
      error: () => { this.toasts.error('No se pudo abrir la encuesta.'); this.cargando.set(false); },
    });
  }

  volver(): void {
    this.router.navigate(['/panel/encuestas']);
  }

  cambiarEstado(estado: EstadoEncuesta): void {
    const cid = this.consorcioId();
    if (!cid || this.procesando()) return;
    this.procesando.set(true);
    this.api.cambiarEstado(cid, this.id(), estado).subscribe({
      next: () => {
        this.procesando.set(false);
        this.confirmCerrar.set(false);
        this.toasts.exito(estado === 'Cerrada' ? 'Encuesta cerrada' : 'Encuesta reabierta');
        this.cargar();
      },
      error: (err) => {
        this.procesando.set(false);
        this.toasts.error(err?.error?.message ?? 'No se pudo actualizar.');
      },
    });
  }

  eliminar(): void {
    const cid = this.consorcioId();
    if (!cid || this.procesando()) return;
    this.procesando.set(true);
    this.api.eliminar(cid, this.id()).subscribe({
      next: () => {
        this.procesando.set(false);
        this.toasts.exito('Encuesta eliminada');
        this.router.navigate(['/panel/encuestas']);
      },
      error: (err) => {
        this.procesando.set(false);
        this.toasts.error(err?.error?.message ?? 'No se pudo eliminar.');
      },
    });
  }

  toggleExport(ev: Event): void {
    ev.stopPropagation();
    this.exportAbierto.update((v) => !v);
  }

  exportar(tipo: ExportTipo): void {
    const d = this.detalle();
    const e = this.encuesta();
    if (!d || !e) return;
    this.exportAbierto.set(false);

    let filas: string[][];
    if (tipo === 'basico') {
      filas = [['Opción', 'Votos', 'Porcentaje']];
      for (const o of e.opciones) filas.push([o.texto, String(o.votos), `${o.porcentaje}%`]);
    } else if (tipo === 'detallado') {
      filas = [['Votante', 'Unidad', 'Opción', 'Fecha']];
      for (const v of d.votantes) {
        filas.push([v.nombre, v.unidad, v.opcion, new Date(v.fechaUtc).toLocaleString('es-AR')]);
      }
    } else {
      filas = [['Opción', 'Votantes']];
      for (const o of e.opciones) {
        const nombres = d.votantes.filter((v) => v.opcion === o.texto).map((v) => v.nombre);
        filas.push([o.texto, nombres.join(' | ') || '(sin votantes)']);
      }
    }

    const csv = filas
      .map((f) => f.map((c) => `"${(c ?? '').replace(/"/g, '""')}"`).join(','))
      .join('\r\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `encuesta-${e.titulo.replace(/[^\w\- ]+/g, '').trim() || 'resultados'}-${tipo}.csv`;
    a.click();
    URL.revokeObjectURL(url);
    this.toasts.exito('Exportación generada');
  }
}
