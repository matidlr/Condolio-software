export type TipoNotificacion =
  | 'General'
  | 'InvitacionEnviada'
  | 'MiembroSeUnio'
  | 'ComentarioPublicacion'
  | 'NuevaEncuesta'
  | 'NuevoTicket'
  | 'NuevaReserva'
  | 'DocumentoNuevo';

export const META_NOTIF: Record<TipoNotificacion, { label: string; icon: string; color: string }> = {
  General: { label: 'General', icon: '🔔', color: '#64748b' },
  InvitacionEnviada: { label: 'Invitaciones', icon: '✉️', color: '#f59e0b' },
  MiembroSeUnio: { label: 'Miembros', icon: '👤', color: '#f59e0b' },
  ComentarioPublicacion: { label: 'Comentarios', icon: '💬', color: '#3b82f6' },
  NuevaEncuesta: { label: 'Encuestas', icon: '🗳️', color: '#a855f7' },
  NuevoTicket: { label: 'Tickets', icon: '🎫', color: '#ef4444' },
  NuevaReserva: { label: 'Reservas', icon: '📅', color: '#14b8a6' },
  DocumentoNuevo: { label: 'Documentos', icon: '📄', color: '#64748b' },
};

export interface Notificacion {
  id: string;
  tipo: TipoNotificacion;
  titulo: string;
  cuerpo: string;
  enlace?: string | null;
  leida: boolean;
  fijada: boolean;
  creadoUtc: string;
  /** Solo se completa en el front cuando se listan notificaciones de varias sociedades a la vez. */
  consorcioId?: string;
  consorcioNombre?: string;
}

export interface NotificacionResumen {
  total: number;
  noLeidas: number;
}

export interface NotificacionLista {
  notificaciones: Notificacion[];
  total: number;
  noLeidas: number;
}
