export type EstadoReserva = 'Pendiente' | 'Confirmada' | 'Rechazada' | 'Cancelada';

export const LABEL_ESTADO_RESERVA: Record<EstadoReserva, string> = {
  Pendiente: 'Pendiente',
  Confirmada: 'Aprobada',
  Rechazada: 'Rechazada',
  Cancelada: 'Cancelada',
};

export interface Reserva {
  id: string;
  amenidadId: string;
  amenidadNombre: string;
  unidadId?: string | null;
  unidadNombre?: string | null;
  solicitante: string;
  inicio: string;
  fin: string;
  estado: EstadoReserva;
  importe?: number | null;
  nota?: string | null;
  creadoUtc: string;
}

export interface ReservaLista {
  reservas: Reserva[];
  pendientes: number;
  aprobadas: number;
  rechazadas: number;
  hoyConfirmadas: number;
}

export interface CrearReserva {
  amenidadId: string;
  unidadId?: string | null;
  inicio: string;
  fin: string;
  nota?: string | null;
}
