import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Encuesta, EncuestaDetalle } from '../models/encuesta.models';

@Injectable({ providedIn: 'root' })
export class MiEncuestaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/encuestas`;

  readonly pendientes = signal(0);

  listar(): Observable<Encuesta[]> {
    return this.http.get<Encuesta[]>(this.base);
  }
  obtener(id: string): Observable<EncuestaDetalle> {
    return this.http.get<EncuestaDetalle>(`${this.base}/${id}`);
  }
  votar(id: string, opcionesIds: string[]): Observable<Encuesta> {
    return this.http.post<Encuesta>(`${this.base}/${id}/votar`, { opcionesIds });
  }
}
