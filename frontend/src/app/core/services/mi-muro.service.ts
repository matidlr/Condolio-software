import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Anuncio, AnuncioDetalle, LikeResultado } from '../models/anuncio.models';

export interface MuroComentario {
  id: string;
  texto: string;
  autor: string;
  fechaUtc: string;
  esMio: boolean;
}
export interface MuroDetalle {
  anuncio: Anuncio;
  comentarios: MuroComentario[];
  likes: { nombre: string }[];
}

@Injectable({ providedIn: 'root' })
export class MiMuroService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/muro`;

  feed(): Observable<Anuncio[]> {
    return this.http.get<Anuncio[]>(this.base);
  }
  publicacion(id: string): Observable<MuroDetalle> {
    return this.http.get<MuroDetalle>(`${this.base}/${id}`);
  }
  publicar(cuerpo: string, imagenes: File[]): Observable<Anuncio> {
    const form = new FormData();
    form.append('cuerpo', cuerpo);
    for (const f of imagenes) form.append('imagenes', f, f.name);
    return this.http.post<Anuncio>(this.base, form);
  }
  comentar(id: string, texto: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/comentarios`, { texto });
  }
  editarComentario(id: string, comentarioId: string, texto: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/comentarios/${comentarioId}`, { texto });
  }
  eliminarComentario(id: string, comentarioId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/comentarios/${comentarioId}`);
  }
  toggleLike(id: string): Observable<LikeResultado> {
    return this.http.post<LikeResultado>(`${this.base}/${id}/like`, {});
  }
  imagenUrl(adjuntoId: string): string {
    return `${this.base}/adjuntos/${adjuntoId}`;
  }
}
