import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Encuesta, EncuestaDetalle, EncuestaLista, EstadoEncuesta, GuardarEncuesta,
} from '../models/encuesta.models';

@Injectable({ providedIn: 'root' })
export class EncuestaService {
  private http = inject(HttpClient);

  readonly activas = signal(0);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/encuestas`;
  }

  listar(consorcioId: string): Observable<EncuestaLista> {
    return this.http.get<EncuestaLista>(this.base(consorcioId));
  }

  refrescarActivas(consorcioId: string): void {
    this.listar(consorcioId).subscribe({
      next: (l) => this.activas.set(l.estadisticas.activas),
      error: () => this.activas.set(0),
    });
  }

  obtener(consorcioId: string, id: string): Observable<EncuestaDetalle> {
    return this.http.get<EncuestaDetalle>(`${this.base(consorcioId)}/${id}`);
  }

  crear(consorcioId: string, body: GuardarEncuesta): Observable<Encuesta> {
    return this.http.post<Encuesta>(this.base(consorcioId), body);
  }

  actualizar(consorcioId: string, id: string, body: GuardarEncuesta): Observable<Encuesta> {
    return this.http.put<Encuesta>(`${this.base(consorcioId)}/${id}`, body);
  }

  cambiarEstado(consorcioId: string, id: string, estado: EstadoEncuesta): Observable<Encuesta> {
    return this.http.post<Encuesta>(`${this.base(consorcioId)}/${id}/estado`, { estado });
  }

  votar(consorcioId: string, id: string, opcionesIds: string[]): Observable<Encuesta> {
    return this.http.post<Encuesta>(`${this.base(consorcioId)}/${id}/votar`, { opcionesIds });
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }
}
