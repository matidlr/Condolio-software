import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ActividadUnidad,
  GuardarIncidencia,
  IncidenciaDetalle,
  IncidenciaUnidad,
} from '../models/consorcio.models';

@Injectable({ providedIn: 'root' })
export class IncidenciaUnidadService {
  private http = inject(HttpClient);

  private base(consorcioId: string, unidadId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/unidades/${unidadId}`;
  }

  actividad(consorcioId: string, unidadId: string): Observable<ActividadUnidad[]> {
    return this.http.get<ActividadUnidad[]>(`${this.base(consorcioId, unidadId)}/actividad`);
  }

  listar(consorcioId: string, unidadId: string): Observable<IncidenciaUnidad[]> {
    return this.http.get<IncidenciaUnidad[]>(`${this.base(consorcioId, unidadId)}/incidencias`);
  }

  obtener(consorcioId: string, unidadId: string, id: string): Observable<IncidenciaDetalle> {
    return this.http.get<IncidenciaDetalle>(`${this.base(consorcioId, unidadId)}/incidencias/${id}`);
  }

  comentar(consorcioId: string, unidadId: string, id: string, texto: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId, unidadId)}/incidencias/${id}/comentarios`, { texto });
  }

  escalar(consorcioId: string, unidadId: string, id: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId, unidadId)}/incidencias/${id}/escalar`, {});
  }

  registrar(consorcioId: string, unidadId: string, body: GuardarIncidencia): Observable<IncidenciaUnidad> {
    return this.http.post<IncidenciaUnidad>(`${this.base(consorcioId, unidadId)}/incidencias`, body);
  }

  editar(consorcioId: string, unidadId: string, id: string, body: GuardarIncidencia): Observable<IncidenciaUnidad> {
    return this.http.put<IncidenciaUnidad>(`${this.base(consorcioId, unidadId)}/incidencias/${id}`, body);
  }

  eliminar(consorcioId: string, unidadId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId, unidadId)}/incidencias/${id}`);
  }
}
