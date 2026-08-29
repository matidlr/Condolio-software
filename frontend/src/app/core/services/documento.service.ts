import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Analiticas, Carpeta, CategoriaDocumento, Contenido, Documento, NivelAcceso } from '../models/documento.models';

@Injectable({ providedIn: 'root' })
export class DocumentoService {
  private http = inject(HttpClient);

  private base(consorcioId: string): string {
    return `${environment.apiUrl}/consorcios/${consorcioId}/documentos`;
  }

  listar(consorcioId: string, carpetaId?: string | null): Observable<Contenido> {
    const params: Record<string, string> = carpetaId ? { carpetaId } : {};
    return this.http.get<Contenido>(this.base(consorcioId), { params });
  }

  recientes(consorcioId: string): Observable<Documento[]> {
    return this.http.get<Documento[]>(`${this.base(consorcioId)}/recientes`);
  }

  destacados(consorcioId: string): Observable<Documento[]> {
    return this.http.get<Documento[]>(`${this.base(consorcioId)}/destacados`);
  }

  porNivel(consorcioId: string, nivel: NivelAcceso): Observable<Documento[]> {
    return this.http.get<Documento[]>(`${this.base(consorcioId)}/nivel/${nivel}`);
  }

  crearCarpeta(consorcioId: string, body: { nombre: string; carpetaPadreId?: string | null; nivel: NivelAcceso }): Observable<Carpeta> {
    return this.http.post<Carpeta>(`${this.base(consorcioId)}/carpetas`, body);
  }

  renombrarCarpeta(consorcioId: string, id: string, nombre: string): Observable<void> {
    return this.http.put<void>(`${this.base(consorcioId)}/carpetas/${id}`, { nombre });
  }

  moverCarpeta(consorcioId: string, id: string, destinoId: string | null): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/carpetas/${id}/mover`, { destinoId });
  }

  todasLasCarpetas(consorcioId: string): Observable<Carpeta[]> {
    return this.http.get<Carpeta[]>(`${this.base(consorcioId)}/carpetas/todas`);
  }

  eliminarCarpeta(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/carpetas/${id}`);
  }

  subir(consorcioId: string, archivo: File, carpetaId: string | null, nivel: NivelAcceso, categoria: CategoriaDocumento): Observable<Documento> {
    const form = new FormData();
    form.append('archivo', archivo, archivo.name);
    if (carpetaId) form.append('carpetaId', carpetaId);
    form.append('nivel', nivel);
    form.append('categoria', categoria);
    return this.http.post<Documento>(this.base(consorcioId), form);
  }

  actualizar(consorcioId: string, id: string, body: { nombre: string; nivel: NivelAcceso; categoria: CategoriaDocumento; carpetaId?: string | null }): Observable<Documento> {
    return this.http.put<Documento>(`${this.base(consorcioId)}/${id}`, body);
  }

  destacar(consorcioId: string, id: string, destacar: boolean): Observable<void> {
    return this.http.post<void>(`${this.base(consorcioId)}/${id}/destacar`, { destacar });
  }

  descargar(consorcioId: string, id: string, descarga = false): Observable<Blob> {
    const params: Record<string, string> = descarga ? { descarga: 'true' } : {};
    return this.http.get(`${this.base(consorcioId)}/${id}/descargar`, { params, responseType: 'blob' as const });
  }

  eliminar(consorcioId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.base(consorcioId)}/${id}`);
  }

  analiticas(consorcioId: string): Observable<Analiticas> {
    return this.http.get<Analiticas>(`${this.base(consorcioId)}/analiticas`);
  }
}
