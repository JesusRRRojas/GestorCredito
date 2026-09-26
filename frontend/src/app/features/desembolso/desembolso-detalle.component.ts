import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProspectoDetalle, ProspectosService } from '../../core/services/prospectos.service';
import { descripcionCuentaDesembolso } from '../../shared/cuenta-desembolso';
import { EncabezadoClienteComponent } from '../../shared/encabezado-cliente/encabezado-cliente.component';

/**
 * Pantalla 6 - Desembolso: generación del PDF final y cierre del proceso
 * (FR-027/FR-028).
 */
@Component({
  selector: 'app-desembolso-detalle',
  standalone: true,
  imports: [RouterLink, EncabezadoClienteComponent],
  templateUrl: './desembolso-detalle.component.html'
})
export class DesembolsoDetalleComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly prospectosService = inject(ProspectosService);

  readonly prospectoId = Number(this.route.snapshot.paramMap.get('prospectoId'));
  readonly prospecto = signal<ProspectoDetalle | null>(null);
  readonly pdfGenerado = signal(false);
  readonly cerrado = signal(false);

  ngOnInit(): void {
    this.prospectosService.obtener(this.prospectoId).subscribe((p) => this.prospecto.set(p));
  }

  generarPdf(): void {
    window.open(this.prospectosService.urlPdfAprobacionFinal(this.prospectoId), '_blank');
    this.pdfGenerado.set(true);
  }

  cerrarProceso(): void {
    this.prospectosService.cerrar(this.prospectoId).subscribe(() => this.cerrado.set(true));
  }

  volver(): void {
    this.router.navigateByUrl('/desembolso');
  }

  descripcionCuenta(p: ProspectoDetalle): string {
    return descripcionCuentaDesembolso(p);
  }
}
