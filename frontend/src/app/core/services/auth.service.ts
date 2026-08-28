import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, RegistroRequest, RegistroResponse, Rol } from '../models/auth.models';

const STORAGE_KEY = 'condolio.auth';

interface SesionGuardada extends LoginResponse {}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _sesion = signal<SesionGuardada | null>(this.leerStorage());

  readonly sesion = this._sesion.asReadonly();
  readonly autenticado = computed(() => this._sesion() !== null);
  readonly roles = computed<Rol[]>(() => this._sesion()?.roles ?? []);
  readonly nombre = computed(() => this._sesion()?.nombre ?? '');

  constructor(private http: HttpClient) {}

  get token(): string | null {
    return this._sesion()?.token ?? null;
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, body).pipe(
      tap((res) => this.guardarSesion(res)),
    );
  }

  registrar(body: RegistroRequest): Observable<RegistroResponse> {
    return this.http.post<RegistroResponse>(`${environment.apiUrl}/auth/register`, body);
  }

  verificar(email: string, codigo: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/verificar`, { email, codigo }).pipe(
      tap((res) => this.guardarSesion(res)),
    );
  }

  reenviarCodigo(email: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/auth/reenviar-codigo`, { email });
  }

  aceptarInvitacion(token: string, body: {
    nombre: string; apellido: string; telefono?: string | null; password: string;
  }): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/invitaciones/${token}/aceptar`, body).pipe(
      tap((res) => this.guardarSesion(res)),
    );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._sesion.set(null);
  }

  tieneRol(...roles: Rol[]): boolean {
    const actuales = this.roles();
    return roles.some((r) => actuales.includes(r));
  }

  /** Ruta de inicio según el rol principal del usuario. */
  rutaInicio(): string {
    if (this.tieneRol('SuperAdmin')) return '/admin-saas';
    if (this.tieneRol('Administrador')) return '/panel';
    if (this.tieneRol('Residente')) return '/mi-unidad';
    return '/';
  }

  private guardarSesion(res: LoginResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(res));
    this._sesion.set(res);
  }

  private leerStorage(): SesionGuardada | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const s = JSON.parse(raw) as SesionGuardada;
      if (new Date(s.expiraUtc).getTime() < Date.now()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return s;
    } catch {
      return null;
    }
  }
}
