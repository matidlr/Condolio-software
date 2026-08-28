import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-verificar',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './verificar.component.html',
  styleUrl: './login/login.component.scss',
})
export class VerificarComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  email = signal<string>(this.route.snapshot.queryParamMap.get('email') ?? '');
  digitos = signal<string[]>(['', '', '', '']);
  verificando = signal(false);
  error = signal<string | null>(null);
  reenviado = signal(false);

  codigo(): string {
    return this.digitos().join('');
  }

  onInput(i: number, ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const v = input.value.replace(/\D/g, '').slice(-1);
    this.digitos.update((d) => { const n = [...d]; n[i] = v; return n; });
    if (v && i < 3) {
      const next = input.parentElement?.children[i + 1] as HTMLInputElement | undefined;
      next?.focus();
    }
    this.error.set(null);
  }

  onKeydown(i: number, ev: KeyboardEvent): void {
    if (ev.key === 'Backspace' && !this.digitos()[i] && i > 0) {
      const prev = (ev.target as HTMLElement).parentElement?.children[i - 1] as HTMLInputElement | undefined;
      prev?.focus();
    }
  }

  onPaste(ev: ClipboardEvent): void {
    const txt = (ev.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 4);
    if (txt) {
      ev.preventDefault();
      this.digitos.set([txt[0] ?? '', txt[1] ?? '', txt[2] ?? '', txt[3] ?? '']);
    }
  }

  verificar(): void {
    if (this.codigo().length !== 4) return;
    this.verificando.set(true);
    this.error.set(null);
    this.auth.verificar(this.email(), this.codigo()).subscribe({
      next: () => this.router.navigateByUrl(this.auth.rutaInicio()),
      error: (e) => {
        this.error.set(e?.error?.message ?? 'No pudimos verificar el código.');
        this.verificando.set(false);
      },
    });
  }

  reenviar(): void {
    this.auth.reenviarCodigo(this.email()).subscribe(() => {
      this.reenviado.set(true);
      setTimeout(() => this.reenviado.set(false), 4000);
    });
  }
}
