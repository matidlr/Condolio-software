import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearPase, PaseAcceso } from '../models/pase-acceso.models';

@Injectable({ providedIn: 'root' })
export class PaseAccesoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/pases`;

  mis(): Observable<PaseAcceso[]> {
    return this.http.get<PaseAcceso[]>(this.base);
  }

  obtener(id: string): Observable<PaseAcceso> {
    return this.http.get<PaseAcceso>(`${this.base}/${id}`);
  }

  crear(body: CrearPase): Observable<PaseAcceso> {
    return this.http.post<PaseAcceso>(this.base, body);
  }

  revocar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
