import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CrearPase, PaseAcceso, TipoVisita, TipoVehiculo } from '../models/pase-acceso.models';

export interface Visita {
  id: string;
  visitanteNombre: string;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  patente?: string | null;
  ingresoUtc: string;
  egresoUtc?: string | null;
  registradoPor: string;
  nota?: string | null;
}

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

  visitas(): Observable<Visita[]> {
    return this.http.get<Visita[]>(`${environment.apiUrl}/mi-portal/visitas`);
  }
}
