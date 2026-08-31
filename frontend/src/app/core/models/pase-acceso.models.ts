export type TipoPase = 'UnaEntrada' | 'Temporal' | 'PaseFiesta';
export type TipoVisita =
  | 'Familia' | 'Amigo' | 'Huesped' | 'EntregaComida'
  | 'EntregaDomicilio' | 'ProveedorServicios' | 'Empleado'
  | 'Visita' | 'Taxi' | 'Residente' | 'Otro';
export type TipoVehiculo = 'SinVehiculo' | 'Auto' | 'Motocicleta';
export type EstadoPase = 'Activo' | 'Usado' | 'Vencido' | 'Revocado';

export const TIPOS_PASE: { value: TipoPase; label: string; icon: string }[] = [
  { value: 'UnaEntrada', label: 'Una sola entrada', icon: '🕐' },
  { value: 'Temporal', label: 'QR temporal', icon: '📅' },
  { value: 'PaseFiesta', label: 'Pase de Fiesta', icon: '✦' },
];

export const TIPOS_VISITA: { value: TipoVisita; label: string; icon: string }[] = [
  { value: 'Visita', label: 'Visita', icon: '👋' },
  { value: 'Familia', label: 'Familia', icon: '👪' },
  { value: 'Amigo', label: 'Amigo', icon: '🧑' },
  { value: 'Huesped', label: 'Huésped', icon: '🛏' },
  { value: 'Empleado', label: 'Empleado', icon: '🛠' },
  { value: 'Taxi', label: 'Taxi / transporte', icon: '🚕' },
  { value: 'EntregaComida', label: 'Entrega de comida', icon: '🍴' },
  { value: 'EntregaDomicilio', label: 'Entrega de paquetería', icon: '📦' },
  { value: 'ProveedorServicios', label: 'Proveedor de servicios', icon: '🔧' },
  { value: 'Residente', label: 'Residente', icon: '🏠' },
  { value: 'Otro', label: 'Otro', icon: '···' },
];

export const TIPOS_VEHICULO: { value: TipoVehiculo; label: string; icon: string }[] = [
  { value: 'SinVehiculo', label: 'Sin vehículo', icon: '👣' },
  { value: 'Auto', label: 'Auto', icon: '🚗' },
  { value: 'Motocicleta', label: 'Motocicleta', icon: '🏍' },
];

export const LABEL_VISITA: Record<TipoVisita, string> =
  Object.fromEntries(TIPOS_VISITA.map((t) => [t.value, t.label])) as Record<TipoVisita, string>;
export const ICON_VISITA: Record<TipoVisita, string> =
  Object.fromEntries(TIPOS_VISITA.map((t) => [t.value, t.icon])) as Record<TipoVisita, string>;
export const LABEL_PASE: Record<TipoPase, string> =
  Object.fromEntries(TIPOS_PASE.map((t) => [t.value, t.label])) as Record<TipoPase, string>;

export interface PaseAcceso {
  id: string;
  tipoPase: TipoPase;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  visitanteNombre: string;
  patente?: string | null;
  fechaEntrada: string;
  validoHastaUtc?: string | null;
  estado: EstadoPase;
  token: string;
  creadoPor: string;
  creadoUtc: string;
  usosCount: number;
  usosMax: number;
  consorcioNombre: string;
  unidadNombre: string;
  qrPngBase64: string;
}

export interface CrearPase {
  tipoPase: TipoPase;
  tipoVisita: TipoVisita;
  vehiculo: TipoVehiculo;
  visitanteNombre: string;
  patente?: string | null;
  fechaEntrada: string;
  validoHasta?: string | null;
}
