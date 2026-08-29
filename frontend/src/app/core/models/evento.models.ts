export type CategoriaEvento = 'General' | 'Reunion' | 'Mantenimiento' | 'Social' | 'Amenidad';

export const CATEGORIAS_EVENTO: { value: CategoriaEvento; label: string; color: string }[] = [
  { value: 'General', label: 'General', color: '#2563eb' },
  { value: 'Reunion', label: 'Reunión', color: '#7c3aed' },
  { value: 'Mantenimiento', label: 'Mantenimiento', color: '#14b8a6' },
  { value: 'Social', label: 'Social', color: '#db2777' },
  { value: 'Amenidad', label: 'Amenidad', color: '#d97706' },
];

export const META_EVENTO = Object.fromEntries(
  CATEGORIAS_EVENTO.map((c) => [c.value, c]),
) as Record<CategoriaEvento, { value: CategoriaEvento; label: string; color: string }>;

export interface Evento {
  id: string;
  titulo: string;
  descripcion?: string | null;
  ubicacion?: string | null;
  categoria: CategoriaEvento;
  inicioUtc: string;
  finUtc: string;
  todoElDia: boolean;
  notificoComunidad: boolean;
  creadoPor: string;
}

export interface GuardarEvento {
  titulo: string;
  descripcion?: string | null;
  ubicacion?: string | null;
  categoria: CategoriaEvento;
  inicioUtc: string;
  finUtc: string;
  todoElDia: boolean;
  notificarComunidad: boolean;
}
