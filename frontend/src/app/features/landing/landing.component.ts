import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

interface FeatureCard {
  n: string;
  titulo: string;
  items: string[];
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss',
})
export class LandingComponent {
  anio = new Date().getFullYear();

  // ---- Calculadora de precio ----
  private readonly precioPorUnidad = 900; // ARS + IVA por unidad/mes
  unidades = signal(50);

  mensual = computed(() => this.fmt(this.unidades() * this.precioPorUnidad));
  anual = computed(() => this.fmt(this.unidades() * this.precioPorUnidad * 10)); // plan anual: pagás 10 meses

  private fmt(n: number): string {
    return '$' + n.toLocaleString('es-AR');
  }

  setUnidades(v: string): void {
    const n = Number(v);
    this.unidades.set(Number.isFinite(n) && n > 0 ? Math.min(n, 5000) : 1);
  }

  readonly experiencias = [
    {
      rol: 'Residente',
      chip: 'Pago enviado',
      texto: 'Paga las expensas desde el celular, reserva amenities e invita visitas.',
    },
    {
      rol: 'Administrador',
      chip: 'Estado de cuenta listo',
      texto: 'Liquida expensas, lleva la contabilidad y el directorio de residentes.',
    },
    {
      rol: 'Portería',
      chip: 'Acceso autorizado',
      texto: 'Valida el ingreso con QR y deja todo registrado, sin planillas de papel.',
    },
  ];

  readonly residente: FeatureCard[] = [
    { n: '01', titulo: 'Finanzas', items: ['Estado de cuenta en tiempo real', 'Pago online con comprobante', 'Historial completo de expensas'] },
    { n: '02', titulo: 'Accesos', items: ['QR para visitas y proveedores', 'Registro de vehículos', 'Aviso instantáneo al llegar'] },
    { n: '03', titulo: 'Reservas', items: ['Reserva de amenities', 'Calendario de eventos del consorcio', 'Documentos disponibles'] },
    { n: '04', titulo: 'Comunidad', items: ['Muro con comunicados oficiales', 'Contactos de emergencia', 'Novedades del edificio'] },
    { n: '05', titulo: 'Día a día', items: ['Reclamos con seguimiento', 'Aviso de encomiendas', 'Datos de la unidad'] },
  ];

  readonly administrador: FeatureCard[] = [
    { n: '01', titulo: 'Contabilidad', items: ['Panel de ingresos y egresos', 'Cargos recurrentes y extraordinarios', 'Liquidación de expensas'] },
    { n: '02', titulo: 'Morosidad', items: ['Seguimiento de unidades al día y en mora', 'Cálculo automático de intereses', 'Recordatorios de pago'] },
    { n: '03', titulo: 'Padrón', items: ['Alta de unidades, propietarios e inquilinos', 'Coeficientes y cuotas', 'Registro de mascotas y vehículos'] },
    { n: '04', titulo: 'Seguridad', items: ['Bitácora de accesos digital', 'Registro de autorizaciones', 'Códigos QR y alertas'] },
  ];

  readonly porteria: FeatureCard[] = [
    { n: '01', titulo: 'Ingreso', items: ['Validación por escaneo de QR', 'Carga manual cuando hace falta', 'Vista de ocupación en tiempo real'] },
    { n: '02', titulo: 'Encomiendas', items: ['Aviso al residente al recibir el paquete', 'Confirmación de entrega con fecha y hora', 'Sin registros por WhatsApp'] },
  ];

  readonly pasos = [
    { n: 1, titulo: 'Creá tu cuenta', texto: 'Nombre, email y contraseña. Sin tarjeta de crédito.' },
    { n: 2, titulo: 'Importá las unidades', texto: 'Carga masiva con plantilla Excel/CSV.' },
    { n: 3, titulo: 'Revisá el padrón', texto: 'Ajustá los datos de cada unidad antes de arrancar.' },
    { n: 4, titulo: 'Invitá a los residentes', texto: 'Un clic y cada unidad recibe su acceso.' },
    { n: 5, titulo: 'Configurá las expensas', texto: 'Montos, fechas de vencimiento e intereses por mora.' },
  ];

  readonly faqs = [
    { q: '¿Cómo funciona la prueba gratis?', a: 'Tenés 2 meses sin cargo con todas las funciones. No pedimos tarjeta y podés cancelar cuando quieras.' },
    { q: '¿Cómo es el precio?', a: 'Una sola tarifa por unidad por mes, con descuento por volumen. Sin contratos forzosos.' },
    { q: '¿Es seguro?', a: 'Cifrado SSL/TLS en tránsito y en reposo, backups diarios y cumplimiento de la Ley 25.326 de Protección de Datos Personales.' },
    { q: '¿Cuánto tarda la implementación?', a: 'De cero a consorcio operando en minutos: importás las unidades, invitás a los residentes y listo.' },
    { q: '¿Qué soporte tienen?', a: 'Soporte por email y WhatsApp en días hábiles.' },
    { q: '¿En qué dispositivos funciona?', a: 'La administración funciona en cualquier navegador; los residentes y la portería tienen app móvil.' },
  ];
}
