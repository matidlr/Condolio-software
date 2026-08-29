export type CategoriaAnuncio = 'General' | 'Mantenimiento' | 'Urgente' | 'Evento';

export interface CategoriaAnuncioMeta {
  value: CategoriaAnuncio;
  label: string;
  icono: string;   // inline SVG path(s)
  color: string;
}

export const CATEGORIAS_ANUNCIO: CategoriaAnuncioMeta[] = [
  { value: 'General', label: 'General', color: '#2563eb',
    icono: '<path d="M3 11l18-7v16L3 13z"/><path d="M11.6 15.6a3 3 0 0 1-5.6-1.4"/>' },
  { value: 'Mantenimiento', label: 'Mantenimiento', color: '#14b8a6',
    icono: '<path d="M14 7a4 4 0 0 1-5 5L4 17l3 3 5-5a4 4 0 0 1 5-5 4 4 0 0 0-3-3z"/>' },
  { value: 'Urgente', label: 'Urgente', color: '#d97706',
    icono: '<path d="M12 3l10 18H2z"/><path d="M12 10v5M12 18h.01"/>' },
  { value: 'Evento', label: 'Evento', color: '#7c3aed',
    icono: '<path d="M5 3l2 4M15 2l1 4M20 8l-3 2M4 20l7-7"/><circle cx="16" cy="16" r="5"/>' },
];

export const META_ANUNCIO: Record<CategoriaAnuncio, CategoriaAnuncioMeta> =
  Object.fromEntries(CATEGORIAS_ANUNCIO.map((c) => [c.value, c])) as Record<CategoriaAnuncio, CategoriaAnuncioMeta>;

export interface Anuncio {
  id: string;
  titulo?: string | null;
  cuerpo: string;
  categoria: CategoriaAnuncio;
  fijado: boolean;
  publicadoUtc: string;
  eventoFechaUtc?: string | null;
  autor: string;
  imagenesIds: string[];
  totalLikes: number;
  totalComentarios: number;
  yoDiLike: boolean;
}

export interface AnuncioComentario {
  id: string;
  texto: string;
  autor: string;
  fechaUtc: string;
}

export interface AnuncioDetalle {
  anuncio: Anuncio;
  comentarios: AnuncioComentario[];
  likes: { nombre: string }[];
}

export interface LikeResultado {
  total: number;
  yoDiLike: boolean;
  likes: { nombre: string }[];
}

export interface AnuncioLista {
  anuncios: Anuncio[];
  total: number;
  general: number;
  mantenimiento: number;
  urgente: number;
  evento: number;
}

export interface GuardarAnuncio {
  titulo?: string | null;
  cuerpo: string;
  categoria: CategoriaAnuncio;
  fijado: boolean;
  publicadoUtc?: string | null;
  eventoFechaUtc?: string | null;
  imagenesIds?: string[] | null;
}
