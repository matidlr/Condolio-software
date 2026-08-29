import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Amenidad, AmenidadLista, GuardarAmenidad } from '../models/amenidad.models';

@Injectable({ providedIn: 'root' })
export class AmenidadService {
  private http = inject(HttpClient);

  /** Total de amenidades del consorcio activo (para el badge del menú). */
  readonly total = signal(0);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/amenidades`;
  }

  listar(consorcioId: string): Observable<AmenidadLista> {
    return this.http.get<AmenidadLista>(this.base(consorcioId));
  }

  refrescarTotal(consorcioId: string): void {
    this.listar(consorcioId).subscribe({
      next: (l) => this.total.set(l.total),
      error: () => this.total.set(0),
    });
  }

  obtener(consorcioId: string, id: string): Observable<Amenidad> {
    return this.http.get<Amenidad>(`${this.base(consorcioId)}/${id}`);
  }

  crear(consorcioId: string, body: GuardarAmenidad): Observable<Amenidad> {
    return this.http.post<Amenidad>(this.base(consorcioId), body);
  }

  actualizar(consorcioId: string, id: string, body: GuardarAmenidad): Observable<Amenidad> {
    return this.http.put<Amenidad>(`${this.base(consorcioId)}/${id}`, body);
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }
}
