import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PanelResumen {
  consorcioNombre: string;
  unidades: number;
  residentes: number;
  residentesActivos: number;
  publicacionesMes: number;
  ticketsAbiertos: number;
  reservasSemana: number;
  paquetesPendientes: number;
  pagosMes: number;
  entradasHoy: number;
  codigosQrActivos: number;
}

@Injectable({ providedIn: 'root' })
export class PanelService {
  private http = inject(HttpClient);

  resumen(consorcioId: string): Observable<PanelResumen> {
    return this.http.get<PanelResumen>(`${environment.apiUrl}/consorcios/${consorcioId}/panel/resumen`);
  }
}
