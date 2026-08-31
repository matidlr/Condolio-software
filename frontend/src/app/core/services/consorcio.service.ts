import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActualizarConsorcio, Consorcio, ConsorcioDetalle, CrearConsorcio } from '../models/consorcio.models';

const ACTIVO_KEY = 'condolio.consorcioActivo';

@Injectable({ providedIn: 'root' })
export class ConsorcioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/consorcios`;

  readonly consorcios = signal<Consorcio[]>([]);
  readonly activoId = signal<string | null>(localStorage.getItem(ACTIVO_KEY));

  cargar(): Observable<Consorcio[]> {
    return this.http.get<Consorcio[]>(this.base).pipe(
      tap((list) => {
        this.consorcios.set(list);
        if (!this.activoId() && list.length) this.setActivo(list[0].id);
        if (this.activoId() && !list.some((c) => c.id === this.activoId())) {
          this.setActivo(list.length ? list[0].id : null);
        }
      }),
    );
  }

  crear(body: CrearConsorcio): Observable<Consorcio> {
    return this.http.post<Consorcio>(this.base, body).pipe(
      tap((c) => {
        this.consorcios.update((l) => [...l, c]);
        this.setActivo(c.id);
      }),
    );
  }

  detalle(id: string): Observable<ConsorcioDetalle> {
    return this.http.get<ConsorcioDetalle>(`${this.base}/${id}/detalle`);
  }

  actualizar(id: string, body: ActualizarConsorcio): Observable<ConsorcioDetalle> {
    return this.http.put<ConsorcioDetalle>(`${this.base}/${id}`, body).pipe(
      tap((c) => this.consorcios.update((l) => l.map((x) => x.id === id ? { ...x, ...c } : x))),
    );
  }

  eliminar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  setActivo(id: string | null): void {
    this.activoId.set(id);
    if (id) localStorage.setItem(ACTIVO_KEY, id);
    else localStorage.removeItem(ACTIVO_KEY);
  }

  get activo(): Consorcio | null {
    return this.consorcios().find((c) => c.id === this.activoId()) ?? null;
  }
}
