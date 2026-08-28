import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotaUnidad } from '../models/consorcio.models';

@Injectable({ providedIn: 'root' })
export class NotaUnidadService {
  private http = inject(HttpClient);

  private base(consorcioId: string, unidadId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/unidades/${unidadId}/notas`;
  }

  listar(consorcioId: string, unidadId: string): Observable<NotaUnidad[]> {
    return this.http.get<NotaUnidad[]>(this.base(consorcioId, unidadId));
  }

  agregar(consorcioId: string, unidadId: string, texto: string): Observable<NotaUnidad> {
    return this.http.post<NotaUnidad>(this.base(consorcioId, unidadId), { texto });
  }

  editar(consorcioId: string, unidadId: string, notaId: string, texto: string): Observable<NotaUnidad> {
    return this.http.put<NotaUnidad>(`${this.base(consorcioId, unidadId)}/${notaId}`, { texto });
  }

  eliminar(consorcioId: string, unidadId: string, notaId: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId, unidadId)}/${notaId}`);
  }
}
