import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface NotifResidente {
  id: string;
  tipo: string;
  titulo: string;
  cuerpo: string;
  enlace?: string | null;
  leida: boolean;
  fijada: boolean;
  creadoUtc: string;
}

export interface NotifLista {
  notificaciones: NotifResidente[];
  total: number;
  noLeidas: number;
}

export interface PreferenciasNotif {
  seguridadApp: boolean; seguridadMail: boolean;
  finanzasApp: boolean; finanzasMail: boolean;
  comunidadApp: boolean; comunidadMail: boolean;
  eventosApp: boolean; eventosMail: boolean;
  edificioApp: boolean; edificioMail: boolean;
}

export const CATEGORIAS_NOTIF: {
  key: 'seguridad' | 'finanzas' | 'comunidad' | 'eventos' | 'edificio';
  titulo: string; desc: string; icon: string; color: string;
}[] = [
  { key: 'seguridad', titulo: 'Seguridad y urgencias', desc: 'Alertas de seguridad y emergencias', icon: '🛡', color: '#dc2626' },
  { key: 'finanzas', titulo: 'Finanzas', desc: 'Cargos, recibos y recordatorios de pago', icon: '💲', color: '#16a34a' },
  { key: 'comunidad', titulo: 'Comunidad y anuncios', desc: 'Publicaciones, anuncios y alertas de la comunidad', icon: '📣', color: '#2563eb' },
  { key: 'eventos', titulo: 'Eventos y reservas', desc: 'Eventos, reservas de amenidades y encuestas', icon: '📅', color: '#7c3aed' },
  { key: 'edificio', titulo: 'Edificio y entregas', desc: 'Paquetes, visitantes, mantenimiento e incidencias', icon: '📦', color: '#d97706' },
];

export const META_TIPO_NOTIF: Record<string, { icon: string; color: string }> = {
  IncidenciaActualizada: { icon: '🔧', color: '#d97706' },
  RespuestaIncidencia: { icon: '💬', color: '#0891b2' },
  NuevoAnuncio: { icon: '📣', color: '#2563eb' },
  NuevaEncuesta: { icon: '📊', color: '#7c3aed' },
  NuevaReserva: { icon: '◈', color: '#3b82f6' },
  DocumentoNuevo: { icon: '📄', color: '#64748b' },
  General: { icon: '🔔', color: '#64748b' },
};

@Injectable({ providedIn: 'root' })
export class MiNotificacionService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/mi-portal/notificaciones`;

  listar(soloNoLeidas = false): Observable<NotifLista> {
    return this.http.get<NotifLista>(this.base, { params: soloNoLeidas ? { soloNoLeidas: 'true' } : {} });
  }
  marcarLeida(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/leida`, {});
  }
  marcarTodasLeidas(): Observable<void> {
    return this.http.post<void>(`${this.base}/leidas`, {});
  }
  alternarLeida(id: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.base}/${id}/alternar-leida`, {});
  }
  alternarFijada(id: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.base}/${id}/fijar`, {});
  }
  preferencias(): Observable<PreferenciasNotif> {
    return this.http.get<PreferenciasNotif>(`${this.base}/preferencias`);
  }
  guardarPreferencias(p: PreferenciasNotif): Observable<PreferenciasNotif> {
    return this.http.put<PreferenciasNotif>(`${this.base}/preferencias`, p);
  }
}
