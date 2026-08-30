import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CalendarioItem {
  id: string;
  titulo: string;
  descripcion?: string | null;
  ubicacion?: string | null;
  inicio: string;
  fin: string;
  todoElDia: boolean;
  tipo: 'Evento' | 'Reserva';
  categoria?: string | null;
}

export interface CrearEvento {
  titulo: string;
  descripcion?: string | null;
  ubicacion?: string | null;
  categoria: string;
  inicio: string;
  fin: string;
  todoElDia: boolean;
}

export const CATEGORIAS_EVENTO = [
  { value: 'General', label: 'General' },
  { value: 'Reunion', label: 'Reunión' },
  { value: 'Mantenimiento', label: 'Mantenimiento' },
  { value: 'Social', label: 'Social' },
  { value: 'Amenidad', label: 'Amenidad' },
];

@Injectable({ providedIn: 'root' })
export class MiCalendarioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal`;

  items(desde: string, hasta: string): Observable<CalendarioItem[]> {
    return this.http.get<CalendarioItem[]>(`${this.base}/calendario`, { params: { desde, hasta } });
  }

  crearEvento(body: CrearEvento): Observable<CalendarioItem> {
    return this.http.post<CalendarioItem>(`${this.base}/eventos`, body);
  }
}
