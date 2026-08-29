import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Adjunto, AdjuntoOwner } from '../models/consorcio.models';

@Injectable({ providedIn: 'root' })
export class AdjuntoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/adjuntos`;

  listar(ownerTipo: AdjuntoOwner, ownerId: string): Observable<Adjunto[]> {
    return this.http.get<Adjunto[]>(this.base, { params: { ownerTipo, ownerId } });
  }

  subir(ownerTipo: AdjuntoOwner, ownerId: string, archivo: File): Observable<Adjunto> {
    const form = new FormData();
    form.append('ownerTipo', ownerTipo);
    form.append('ownerId', ownerId);
    form.append('archivo', archivo, archivo.name);
    return this.http.post<Adjunto>(this.base, form);
  }

  renombrar(id: string, nombre: string): Observable<Adjunto> {
    return this.http.patch<Adjunto>(`${this.base}/${id}`, { nombre });
  }

  eliminar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  descargar(id: string): Observable<Blob> {
    return this.http.get(`${this.base}/${id}`, { responseType: 'blob' });
  }
}
