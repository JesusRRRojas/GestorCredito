import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CuentaInterna, CuentasInternasService } from '../../core/services/cuentas-internas.service';
import {
  ProspectoDetalle,
  ProspectosService,
  TipoCuentaDesembolso
} from '../../core/services/prospectos.service';
import { EncabezadoClienteComponent } from '../../shared/encabezado-cliente/encabezado-cliente.component';

/**
 * Pantalla 3 - Onboarding (FR-017 a FR-019 de 001; FR-007 a FR-010, FR-015 a FR-018 de 002).
 * Se reutiliza para: guardado inicial, corrección tras "Observado" y edición desde la
 * bandeja del Asesor (Historia 5, research.md §3).
 */
@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [FormsModule, EncabezadoClienteComponent],
  templateUrl: './onboarding.component.html'
})
export class OnboardingComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly prospectosService = inject(ProspectosService);
  private readonly cuentasInternasService = inject(CuentasInternasService);

  readonly prospectoId = Number(this.route.snapshot.paramMap.get('prospectoId'));
  readonly prospecto = signal<ProspectoDetalle | null>(null);
  readonly mensajeError = signal<string | null>(null);
  readonly guardado = signal(false);
  readonly cuentasInternas = signal<CuentaInterna[]>([]);
  readonly agregandoCuentaNueva = signal(false);

  nombres = '';
  apellidos = '';
  direccion = '';
  tipoCuentaDesembolso: TipoCuentaDesembolso = 'Interna';
  cuentaBancariaInternaId: number | null = null;
  numeroCuentaNueva = '';
  cuentaExternaBanco = '';
  cuentaExternaCci = '';

  ngOnInit(): void {
    this.prospectosService.obtener(this.prospectoId).subscribe((p) => {
      this.prospecto.set(p);
      this.nombres = p.nombres ?? '';
      this.apellidos = p.apellidos ?? '';
      this.direccion = p.direccion ?? '';
      this.tipoCuentaDesembolso = p.tipoCuentaDesembolso ?? 'Interna';
      this.cuentaBancariaInternaId = p.cuentaBancariaInternaId;
      this.cuentaExternaBanco = p.cuentaExternaBanco ?? '';
      this.cuentaExternaCci = p.cuentaExternaCCI ?? '';
      this.cargarCuentasInternas(p);
    });
  }

  private cargarCuentasInternas(p: ProspectoDetalle): void {
    this.cuentasInternasService.listar(p.tipoDocumentoId, p.numeroDocumento).subscribe((cuentas) => {
      this.cuentasInternas.set(cuentas);
    });
  }

  mostrarFormularioCuentaNueva(): void {
    this.agregandoCuentaNueva.set(true);
  }

  agregarCuentaInterna(): void {
    const p = this.prospecto();
    if (!p || !this.numeroCuentaNueva.trim()) return;

    this.mensajeError.set(null);
    this.cuentasInternasService.agregar(p.tipoDocumentoId, p.numeroDocumento, this.numeroCuentaNueva).subscribe({
      next: (cuenta) => {
        this.cuentasInternas.update((lista) => [...lista, cuenta]);
        this.cuentaBancariaInternaId = cuenta.id;
        this.numeroCuentaNueva = '';
        this.agregandoCuentaNueva.set(false);
      },
      error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo agregar la cuenta.')
    });
  }

  guardar(): void {
    this.mensajeError.set(null);
    const cuentaExterna =
      this.tipoCuentaDesembolso === 'Externa'
        ? { banco: this.cuentaExternaBanco, cci: this.cuentaExternaCci }
        : null;
    const cuentaInternaId = this.tipoCuentaDesembolso === 'Interna' ? this.cuentaBancariaInternaId : null;

    this.prospectosService
      .guardarOnboarding(
        this.prospectoId,
        this.nombres,
        this.apellidos,
        this.direccion,
        this.tipoCuentaDesembolso,
        cuentaInternaId,
        cuentaExterna
      )
      .subscribe({
        next: () => this.guardado.set(true),
        error: (err) => {
          if (err.status === 409) {
            this.mensajeError.set(
              'No se puede editar: el prospecto está en Aprobación o el proceso ya fue cerrado.'
            );
          } else {
            this.mensajeError.set(err.error?.error ?? 'No se pudo guardar el Onboarding.');
          }
        }
      });
  }

  continuar(): void {
    const p = this.prospecto();
    if (p?.productoRequiereDeclaracionInversion) {
      this.router.navigate(['/declaracion-inversion', this.prospectoId]);
    } else {
      this.router.navigate(['/requisitos', this.prospectoId]);
    }
  }

  volverABandeja(): void {
    this.router.navigateByUrl('/bandeja');
  }
}
