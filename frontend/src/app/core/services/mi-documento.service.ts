import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Contenido } from '../models/documento.models';

@Injectable({ providedIn: 'root' })
export class MiDocumentoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/documentos`;

  listar(carpetaId?: string | null): Observable<Contenido> {
    const params: Record<string, string> = carpetaId ? { carpetaId } : {};
    return this.http.get<Contenido>(this.base, { params });
  }

  descargar(id: string, descarga = false): Observable<Blob> {
    const params: Record<string, string> = descarga ? { descarga: 'true' } : {};
    return this.http.get(`${this.base}/${id}/descargar`, { params, responseType: 'blob' as const });
  }
}
