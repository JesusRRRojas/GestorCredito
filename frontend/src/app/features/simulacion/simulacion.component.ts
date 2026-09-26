import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Producto, ProductosService } from '../../core/services/productos.service';
import { Cuota, ProspectosService } from '../../core/services/prospectos.service';
import { MonedaPePipe } from '../../shared/pipes/moneda.pipe';
import { FechaPePipe } from '../../shared/pipes/fecha-pe.pipe';

/**
 * Pantalla 2 - Simulación (FR-011 a FR-016).
 */
@Component({
  selector: 'app-simulacion',
  standalone: true,
  imports: [FormsModule, MonedaPePipe, FechaPePipe],
  templateUrl: './simulacion.component.html'
})
export class SimulacionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productosService = inject(ProductosService);
  private readonly prospectosService = inject(ProspectosService);

  readonly prospectoId = Number(this.route.snapshot.paramMap.get('prospectoId'));

  readonly productos = signal<Producto[]>([]);
  readonly productoSeleccionado = signal<Producto | null>(null);
  readonly cuotas = signal<Cuota[]>([]);
  readonly simulacionAceptada = signal(false);
  readonly mensajeError = signal<string | null>(null);

  /** Historia 3 (spec 003, FR-015): suma de Interés, Otros y Pago Total del cronograma. */
  readonly totales = computed(() =>
    this.cuotas().reduce(
      (acc, c) => ({
        interes: acc.interes + c.interes,
        otros: acc.otros + c.otros,
        pagoTotal: acc.pagoTotal + c.pagoTotal
      }),
      { interes: 0, otros: 0, pagoTotal: 0 }
    )
  );

  productoId: number | null = null;
  monto = 0;
  tasa = 0;
  plazo = 1;

  ngOnInit(): void {
    this.productosService.listar(true).subscribe((datos) => this.productos.set(datos));
  }

  seleccionarProducto(): void {
    const producto = this.productos().find((p) => p.id === this.productoId) ?? null;
    this.productoSeleccionado.set(producto);
    this.cuotas.set([]);
    if (producto) {
      this.monto = producto.montoMin;
      this.tasa = producto.tasaMin;
      this.plazo = producto.plazoMin;
    }
  }

  simular(): void {
    if (!this.productoId) return;
    this.mensajeError.set(null);

    this.prospectosService.simular(this.prospectoId, this.productoId, this.monto, this.tasa, this.plazo).subscribe({
      next: (respuesta) => this.cuotas.set(respuesta.cuotas),
      error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo generar la simulación.')
    });
  }

  aceptar(): void {
    if (!this.productoId) return;
    this.prospectosService
      .aceptarSimulacion(this.prospectoId, this.productoId, this.monto, this.tasa, this.plazo)
      .subscribe({
        next: () => this.simulacionAceptada.set(true),
        error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo aceptar la simulación.')
      });
  }

  descargarPdf(): void {
    window.open(this.prospectosService.urlPdfCronograma(this.prospectoId), '_blank');
  }

  continuarOnboarding(): void {
    this.router.navigate(['/onboarding', this.prospectoId]);
  }
}
