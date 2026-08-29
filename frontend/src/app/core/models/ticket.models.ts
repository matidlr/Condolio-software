export type CategoriaTicket =
  | 'Amenidades' | 'Seguridad' | 'Mantenimiento' | 'Mascotas'
  | 'Ruido' | 'Vecinos' | 'Servicios' | 'Otro';

export type EstadoTicket = 'Nuevo' | 'EnProgreso' | 'EsperandoInformacion' | 'PendienteAprobacion' | 'Resuelto';

export type PrioridadTicket = 'Baja' | 'Media' | 'Alta' | 'Critica';

export interface CategoriaTicketMeta {
  value: CategoriaTicket;
  label: string;
  icon: string; // inline SVG path(s)
}

export const CATEGORIAS_TICKET: CategoriaTicketMeta[] = [
  { value: 'Amenidades', label: 'Amenidades', icon: '<rect x="4" y="4" width="16" height="16" rx="2"/><path d="M8 4v16M4 12h16"/>' },
  { value: 'Seguridad', label: 'Seguridad', icon: '<path d="M12 3l7 3v6c0 5-3 7-7 9-4-2-7-4-7-9V6z"/>' },
  { value: 'Mantenimiento', label: 'Mantenimiento', icon: '<path d="M14 7a4 4 0 0 1-5 5L4 17l3 3 5-5a4 4 0 0 1 5-5 4 4 0 0 0-3-3z"/>' },
  { value: 'Mascotas', label: 'Mascotas', icon: '<path d="M12 21c4 0 7-2 7-5 0-2-2-4-7-4s-7 2-7 4c0 3 3 5 7 5z"/><circle cx="7" cy="7" r="1.6"/><circle cx="17" cy="7" r="1.6"/><circle cx="4.5" cy="11" r="1.4"/><circle cx="19.5" cy="11" r="1.4"/>' },
  { value: 'Ruido', label: 'Ruido', icon: '<path d="M4 9v6h4l5 4V5L8 9z"/><path d="M16 8a5 5 0 0 1 0 8"/>' },
  { value: 'Vecinos', label: 'Vecinos', icon: '<circle cx="9" cy="8" r="3"/><path d="M3.5 19c0-3 2.5-5 5.5-5s5.5 2 5.5 5M16 6a3 3 0 0 1 0 6"/>' },
  { value: 'Servicios', label: 'Servicios', icon: '<path d="M18 8h1a2 2 0 0 1 0 4h-1M4 8h14v6a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4zM6 2v2M10 2v2M14 2v2"/>' },
  { value: 'Otro', label: 'Otro', icon: '<circle cx="6" cy="12" r="1.5"/><circle cx="12" cy="12" r="1.5"/><circle cx="18" cy="12" r="1.5"/>' },
];

export const LABEL_CATEGORIA_TICKET: Record<CategoriaTicket, string> =
  Object.fromEntries(CATEGORIAS_TICKET.map((c) => [c.value, c.label])) as Record<CategoriaTicket, string>;

export const PRIORIDADES_TICKET: { value: PrioridadTicket; label: string; color: string }[] = [
  { value: 'Critica', label: 'Crítico', color: '#dc2626' },
  { value: 'Alta', label: 'Alto', color: '#ea580c' },
  { value: 'Media', label: 'Medio', color: '#eab308' },
  { value: 'Baja', label: 'Bajo', color: '#16a34a' },
];

export const LABEL_PRIORIDAD: Record<PrioridadTicket, string> =
  Object.fromEntries(PRIORIDADES_TICKET.map((p) => [p.value, p.label])) as Record<PrioridadTicket, string>;

export const ESTADOS_TICKET: { value: EstadoTicket; label: string }[] = [
  { value: 'Nuevo', label: 'Nuevo' },
  { value: 'EnProgreso', label: 'En Progreso' },
  { value: 'EsperandoInformacion', label: 'Esperando Información' },
  { value: 'PendienteAprobacion', label: 'Pendiente de Aprobación' },
  { value: 'Resuelto', label: 'Resuelto' },
];

export const LABEL_ESTADO_TICKET: Record<EstadoTicket, string> =
  Object.fromEntries(ESTADOS_TICKET.map((e) => [e.value, e.label])) as Record<EstadoTicket, string>;

export interface Ticket {
  id: string;
  numero: number;
  titulo?: string | null;
  descripcion: string;
  categoria: CategoriaTicket;
  estado: EstadoTicket;
  prioridad: PrioridadTicket;
  unidadId?: string | null;
  unidadNombre?: string | null;
  etiquetas: string[];
  ubicacion?: string | null;
  fechaLimite?: string | null;
  reportadoPor: string;
  reportadoUtc: string;
  asignadoA?: string | null;
  estadoDesdeUtc: string;
  ultimaActividadUtc: string;
  archivado: boolean;
}

export interface TicketLista {
  tickets: Ticket[];
  activos: number;
  archivados: number;
}

export interface TicketComentario {
  texto: string;
  autor: string;
  fechaUtc: string;
  esInterna: boolean;
}

export interface TicketDetalle {
  ticket: Ticket;
  comentarios: TicketComentario[];
}

export interface UsuarioAsignable {
  id: string;
  nombre: string;
}

export interface CrearTicket {
  titulo?: string | null;
  descripcion: string;
  categoria: CategoriaTicket;
  prioridad: PrioridadTicket;
  unidadId?: string | null;
  asignadoAUsuarioId?: string | null;
  fechaLimite?: string | null;
  etiquetas?: string[] | null;
  ubicacion?: string | null;
}

export interface ActualizarTicket {
  estado: EstadoTicket;
  prioridad: PrioridadTicket;
  asignadoAUsuarioId?: string | null;
}
