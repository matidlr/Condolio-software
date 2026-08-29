import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Anuncio, AnuncioDetalle, AnuncioLista, GuardarAnuncio, LikeResultado,
} from '../models/anuncio.models';

@Injectable({ providedIn: 'root' })
export class AnuncioService {
  private http = inject(HttpClient);

  readonly total = signal(0);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/anuncios`;
  }

  listar(consorcioId: string): Observable<AnuncioLista> {
    return this.http.get<AnuncioLista>(this.base(consorcioId));
  }

  refrescarTotal(consorcioId: string): void {
    this.listar(consorcioId).subscribe({
      next: (l) => this.total.set(l.total),
      error: () => this.total.set(0),
    });
  }

  obtener(consorcioId: string, id: string): Observable<AnuncioDetalle> {
    return this.http.get<AnuncioDetalle>(`${this.base(consorcioId)}/${id}`);
  }

  comentar(consorcioId: string, id: string, texto: string): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/${id}/comentarios`, { texto });
  }

  editarComentario(consorcioId: string, id: string, comentarioId: string, texto: string): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/${id}/comentarios/${comentarioId}`, { texto });
  }

  eliminarComentario(consorcioId: string, id: string, comentarioId: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}/comentarios/${comentarioId}`);
  }

  toggleLike(consorcioId: string, id: string): Observable<LikeResultado> {
    return this.http.post<LikeResultado>(`${this.base(consorcioId)}/${id}/like`, {});
  }

  crear(consorcioId: string, body: GuardarAnuncio): Observable<Anuncio> {
    return this.http.post<Anuncio>(this.base(consorcioId), body);
  }

  actualizar(consorcioId: string, id: string, body: GuardarAnuncio): Observable<Anuncio> {
    return this.http.put<Anuncio>(`${this.base(consorcioId)}/${id}`, body);
  }

  fijar(consorcioId: string, id: string, fijar: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/${id}/fijar`, { fijar });
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }
}
