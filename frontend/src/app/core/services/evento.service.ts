import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Evento, GuardarEvento } from '../models/evento.models';

@Injectable({ providedIn: 'root' })
export class EventoService {
  private http = inject(HttpClient);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/eventos`;
  }

  listar(consorcioId: string, desde: string, hasta: string): Observable<Evento[]> {
    return this.http.get<Evento[]>(this.base(consorcioId), { params: { desde, hasta } });
  }

  crear(consorcioId: string, body: GuardarEvento): Observable<Evento> {
    return this.http.post<Evento>(this.base(consorcioId), body);
  }

  actualizar(consorcioId: string, id: string, body: GuardarEvento): Observable<Evento> {
    return this.http.put<Evento>(`${this.base(consorcioId)}/${id}`, body);
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }
}
