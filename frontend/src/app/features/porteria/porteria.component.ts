import { Component, ElementRef, OnDestroy, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

interface Contexto { consorcioId: string; consorcioNombre: string; casetaNombre: string; }
interface Verificacion {
  valido: boolean; motivo?: string | null; visitanteNombre: string; tipoVisita: string;
  patente?: string | null; unidadNombre: string; consorcioNombre: string; usosRestantes: number;
}

@Component({
  selector: 'app-porteria',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './porteria.component.html',
  styleUrl: './porteria.component.scss',
})
export class PorteriaComponent implements OnDestroy {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toasts = inject(ToastService);

  video = viewChild<ElementRef<HTMLVideoElement>>('video');

  ctx = signal<Contexto | null>(null);
  estado = signal<'idle' | 'escaneando' | 'verificando' | 'resultado'>('idle');
  resultado = signal<Verificacion | null>(null);
  manual = signal('');
  soportaCamara = signal('BarcodeDetector' in window);

  private stream: MediaStream | null = null;
  private detector: any = null;
  private raf = 0;
  private ultimoToken = '';

  nombre = computed(() => this.auth.nombre());

  constructor() {
    this.http.get<Contexto>(`${environment.apiUrl}/porteria/contexto`).subscribe({
      next: (c) => this.ctx.set(c),
      error: (e) => this.toasts.error(e?.error?.message ?? 'No se pudo cargar la caseta.'),
    });
  }

  async escanear(): Promise<void> {
    if (!this.soportaCamara()) { this.estado.set('escaneando'); return; }
    this.estado.set('escaneando');
    this.resultado.set(null);
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
      const v = this.video()?.nativeElement;
      if (v) { v.srcObject = this.stream; await v.play(); }
      this.detector = new (window as any).BarcodeDetector({ formats: ['qr_code'] });
      this.loop();
    } catch {
      this.toasts.error('No pudimos abrir la cámara. Ingresá el código a mano.');
      this.pararCamara();
      this.soportaCamara.set(false);
    }
  }

  private loop = async (): Promise<void> => {
    const v = this.video()?.nativeElement;
    if (!v || this.estado() !== 'escaneando') return;
    try {
      const codes = await this.detector.detect(v);
      if (codes.length && codes[0].rawValue && codes[0].rawValue !== this.ultimoToken) {
        this.ultimoToken = codes[0].rawValue;
        this.verificar(codes[0].rawValue);
        return;
      }
    } catch { /* frame no listo */ }
    this.raf = requestAnimationFrame(this.loop);
  };

  verificarManual(): void {
    const t = this.manual().trim();
    if (t) this.verificar(t);
  }

  private verificar(token: string): void {
    this.pararCamara();
    this.estado.set('verificando');
    this.http.post<Verificacion>(`${environment.apiUrl}/porteria/verificar`, { token }).subscribe({
      next: (r) => { this.resultado.set(r); this.estado.set('resultado'); },
      error: (e) => {
        this.toasts.error(e?.error?.message ?? 'No se pudo verificar el código.');
        this.estado.set('idle');
      },
    });
  }

  siguiente(): void {
    this.resultado.set(null);
    this.manual.set('');
    this.ultimoToken = '';
    this.estado.set('idle');
  }

  private pararCamara(): void {
    cancelAnimationFrame(this.raf);
    this.stream?.getTracks().forEach((t) => t.stop());
    this.stream = null;
  }

  salir(): void {
    this.pararCamara();
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void { this.pararCamara(); }
}
