import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TipoDocumento, TiposService } from '../../core/services/tipos.service';
import { ProspectosService } from '../../core/services/prospectos.service';

/**
 * Pantalla 1 - Validación Inicial (FR-006 a FR-010).
 */
@Component({
  selector: 'app-validacion',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './validacion.component.html'
})
export class ValidacionComponent implements OnInit {
  private readonly tiposService = inject(TiposService);
  private readonly prospectosService = inject(ProspectosService);
  private readonly router = inject(Router);

  readonly tiposDocumento = signal<TipoDocumento[]>([]);
  readonly resultado = signal<{ resultadoMock: number; aprobado: boolean } | null>(null);
  readonly mensajeError = signal<string | null>(null);
  readonly prospectoId = signal<number | null>(null);
  readonly cargando = signal(false);

  tipoDocumentoId: number | null = null;
  numeroDocumento = '';

  ngOnInit(): void {
    this.tiposService.listarTiposDocumento().subscribe((datos) => this.tiposDocumento.set(datos));
  }

  validar(): void {
    if (!this.tipoDocumentoId || !this.numeroDocumento.trim()) return;

    this.mensajeError.set(null);
    this.resultado.set(null);
    this.prospectoId.set(null);
    this.cargando.set(true);

    this.prospectosService.validar(this.tipoDocumentoId, this.numeroDocumento.trim()).subscribe({
      next: (respuesta) => {
        this.cargando.set(false);
        this.resultado.set(respuesta);
        this.prospectoId.set(respuesta.prospectoId);
      },
      error: (err) => {
        this.cargando.set(false);
        if (err.status === 409) {
          this.mensajeError.set('Ya existe un prospecto en curso para este Número de Documento.');
        } else {
          this.mensajeError.set('No se pudo completar la validación.');
        }
      }
    });
  }

  continuar(): void {
    const id = this.prospectoId();
    if (id) {
      this.router.navigate(['/simulacion', id]);
    }
  }
}
