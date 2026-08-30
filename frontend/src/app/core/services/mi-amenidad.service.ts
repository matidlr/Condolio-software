import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AmenidadHorario {
  dia: number;
  cerrado: boolean;
  abreMin: number;
  cierraMin: number;
}

export interface MiAmenidad {
  id: string;
  nombre: string;
  descripcion?: string | null;
  imagenesIds: string[];
  reservable: boolean;
  intervaloMinutos: number;
  limiteMensual: boolean;
  maxReservasPorUnidad: number;
  tieneCosto: boolean;
  tarifa?: number | null;
  requiereAprobacion: boolean;
  diasBloqueados: string[];
  mensajeReserva?: string | null;
  horarios: AmenidadHorario[];
}

export interface Slot {
  inicio: string;
  fin: string;
}

export interface MiReserva {
  id: string;
  amenidadId: string;
  amenidad: string;
  imagenesIds: string[];
  inicio: string;
  fin: string;
  estado: 'Pendiente' | 'Confirmada' | 'Rechazada' | 'Cancelada';
  nota?: string | null;
  creadoUtc: string;
}

export interface MisReservas {
  activas: MiReserva[];
  previas: MiReserva[];
}

@Injectable({ providedIn: 'root' })
export class MiAmenidadService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal`;

  amenidades(): Observable<MiAmenidad[]> {
    return this.http.get<MiAmenidad[]>(`${this.base}/amenidades`);
  }

  amenidad(id: string): Observable<MiAmenidad> {
    return this.http.get<MiAmenidad>(`${this.base}/amenidades/${id}`);
  }

  slots(id: string, fecha: string): Observable<Slot[]> {
    return this.http.get<Slot[]>(`${this.base}/amenidades/${id}/slots`, { params: { fecha } });
  }

  misReservas(): Observable<MisReservas> {
    return this.http.get<MisReservas>(`${this.base}/reservas`);
  }

  solicitar(body: { amenidadId: string; inicio: string; fin: string; nota?: string | null }): Observable<MiReserva> {
    return this.http.post<MiReserva>(`${this.base}/reservas`, body);
  }

  cancelar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/reservas/${id}`);
  }
}
