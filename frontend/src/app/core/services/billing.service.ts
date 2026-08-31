import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type EstadoSuscripcion = 'Trial' | 'Activa' | 'PagoPendiente' | 'Suspendida' | 'Cancelada';

export interface EstadoSuscripcionDto {
  estado: EstadoSuscripcion;
  trialFinUtc: string;
  proximoCobroUtc?: string | null;
  unidadesFacturadas: number;
  importeMensual: number;
  moneda: string;
  accesoPermitido: boolean;
  unidadesActuales: number;
  precioPorUnidad: number;
  importeMensualEstimado: number;
  importeAnualEstimado: number;
  diasTrialRestantes: number;
}

@Injectable({ providedIn: 'root' })
export class BillingService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/billing`;

  estado(): Observable<EstadoSuscripcionDto> {
    return this.http.get<EstadoSuscripcionDto>(`${this.base}/estado`);
  }
}
