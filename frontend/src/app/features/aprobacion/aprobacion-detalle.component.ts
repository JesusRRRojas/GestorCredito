import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ArchivoAdjunto,
  ProspectoDetalle,
  ProspectosService
} from '../../core/services/prospectos.service';
import { descripcionCuentaDesembolso } from '../../shared/cuenta-desembolso';
import { EncabezadoClienteComponent } from '../../shared/encabezado-cliente/encabezado-cliente.component';

/**
 * Pantalla 5 - Aprobación: detalle y decisión del Aprobador (FR-023 a FR-026 de 001;
 * FR-012/FR-013 de 002: comentario obligatorio para Observado/Rechazado).
 */
@Component({
  selector: 'app-aprobacion-detalle',
  standalone: true,
  imports: [RouterLink, FormsModule, EncabezadoClienteComponent],
  templateUrl: './aprobacion-detalle.component.html'
})
export class AprobacionDetalleComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly prospectosService = inject(ProspectosService);

  readonly prospectoId = Number(this.route.snapshot.paramMap.get('prospectoId'));
  readonly prospecto = signal<ProspectoDetalle | null>(null);
  readonly archivos = signal<ArchivoAdjunto[]>([]);
  readonly decidido = signal(false);
  readonly mensajeError = signal<string | null>(null);

  comentario = '';

  ngOnInit(): void {
    this.prospectosService.obtener(this.prospectoId).subscribe((p) => this.prospecto.set(p));
    this.prospectosService.listarArchivosAdjuntos(this.prospectoId).subscribe((datos) => this.archivos.set(datos));
  }

  urlArchivo(requisitoId: number): string {
    return this.prospectosService.urlArchivoAdjunto(this.prospectoId, requisitoId);
  }

  descripcionCuenta(p: ProspectoDetalle): string {
    return descripcionCuentaDesembolso(p);
  }

  puedeObservarORechazar(): boolean {
    return this.comentario.trim().length > 0 && this.comentario.length <= 1000;
  }

  decidir(decision: 'Aprobado' | 'Observado' | 'Rechazado'): void {
    this.mensajeError.set(null);
    this.prospectosService.decidir(this.prospectoId, decision, this.comentario || undefined).subscribe({
      next: () => this.decidido.set(true),
      error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo registrar la decisión.')
    });
  }

  volver(): void {
    this.router.navigateByUrl('/aprobacion');
  }
}
