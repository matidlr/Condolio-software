export interface AmenidadHorario {
  dia: number; // 0 = domingo ... 6 = sábado (DayOfWeek de .NET)
  cerrado: boolean;
  abreMin: number;
  cierraMin: number;
}

export interface Amenidad {
  id: string;
  nombre: string;
  descripcion?: string | null;
  imagenesIds: string[];
  reservable: boolean;
  intervaloMinutos: number; // 0 = flexible, 1440 = día completo
  limiteMensual: boolean;
  maxReservasPorUnidad: number; // 0 = ilimitadas
  tieneCosto: boolean;
  tarifa?: number | null;
  requiereAprobacion: boolean;
  reservableDesde?: string | null; // yyyy-MM-dd
  diasBloqueados: string[];
  mensajeReserva?: string | null;
  horarios: AmenidadHorario[];
  reservasProximas: number;
  creadoUtc: string;
  actualizadoUtc?: string | null;
}

export interface AmenidadLista {
  amenidades: Amenidad[];
  total: number;
  reservables: number;
  reservacionesEsteMes: number;
  aprobacionesPendientes: number;
  ingresosGenerados: number;
}

export interface GuardarAmenidad {
  nombre: string;
  descripcion?: string | null;
  imagenesIds?: string[] | null;
  reservable: boolean;
  intervaloMinutos: number;
  limiteMensual: boolean;
  maxReservasPorUnidad: number;
  tieneCosto: boolean;
  tarifa?: number | null;
  requiereAprobacion: boolean;
  reservableDesde?: string | null;
  diasBloqueados?: string[] | null;
  mensajeReserva?: string | null;
  horarios?: AmenidadHorario[] | null;
}

export const DIAS_SEMANA: { valor: number; label: string }[] = [
  { valor: 1, label: 'Lunes' },
  { valor: 2, label: 'Martes' },
  { valor: 3, label: 'Miércoles' },
  { valor: 4, label: 'Jueves' },
  { valor: 5, label: 'Viernes' },
  { valor: 6, label: 'Sábado' },
  { valor: 0, label: 'Domingo' },
];

export const INTERVALOS_RESERVA: { valor: number; label: string }[] = [
  { valor: 60, label: '1 hora' },
  { valor: 120, label: '2 horas' },
  { valor: 180, label: '3 horas' },
  { valor: 240, label: '4 horas' },
  { valor: 300, label: '5 horas' },
  { valor: 360, label: '6 horas' },
  { valor: 1440, label: 'Día completo' },
  { valor: 0, label: 'Flexible (el residente elige)' },
];

/** Opciones de hora en pasos de 30 min como "9:00 AM". */
export function opcionesHora(): { min: number; label: string }[] {
  const out: { min: number; label: string }[] = [];
  for (let m = 0; m <= 1440; m += 30) {
    const h24 = Math.floor(m / 60) % 24;
    const min = m % 60;
    const ampm = h24 < 12 ? 'AM' : 'PM';
    const h12 = h24 % 12 === 0 ? 12 : h24 % 12;
    out.push({ min: m, label: `${h12}:${String(min).padStart(2, '0')} ${ampm}` });
  }
  return out;
}

export function horaLabel(min: number): string {
  const h24 = Math.floor(min / 60) % 24;
  const m = min % 60;
  const ampm = h24 < 12 ? 'AM' : 'PM';
  const h12 = h24 % 12 === 0 ? 12 : h24 % 12;
  return `${h12}:${String(m).padStart(2, '0')} ${ampm}`;
}
