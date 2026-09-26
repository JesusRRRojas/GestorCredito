import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OrigenFondos, ProspectoDetalle, ProspectosService } from '../../core/services/prospectos.service';
import { EncabezadoClienteComponent } from '../../shared/encabezado-cliente/encabezado-cliente.component';

/**
 * Etapa condicional de Declaración de Inversión (Historia 3, spec 002: FR-004 a FR-006).
 * Solo se muestra cuando el Producto del prospecto tiene `requiereDeclaracionInversion = true`;
 * en caso contrario, se omite automáticamente (mismo patrón de auto-salto que
 * RequisitosComponent para el Fast-Track de 001-gestion-creditos).
 */
@Component({
  selector: 'app-declaracion-inversion',
  standalone: true,
  imports: [FormsModule, EncabezadoClienteComponent],
  templateUrl: './declaracion-inversion.component.html'
})
export class DeclaracionInversionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly prospectosService = inject(ProspectosService);

  readonly prospectoId = Number(this.route.snapshot.paramMap.get('prospectoId'));
  readonly prospecto = signal<ProspectoDetalle | null>(null);
  readonly cargando = signal(true);
  readonly mensajeError = signal<string | null>(null);
  readonly guardado = signal(false);

  montoInvertir: number | null = null;
  origenFondos: OrigenFondos = 'Ahorros';
  detalle = '';

  readonly opcionesOrigenFondos: { valor: OrigenFondos; etiqueta: string }[] = [
    { valor: 'Ahorros', etiqueta: 'Ahorros' },
    { valor: 'Herencia', etiqueta: 'Herencia' },
    { valor: 'VentaActivo', etiqueta: 'Venta de un activo' },
    { valor: 'ActividadEmpresarial', etiqueta: 'Actividad empresarial' },
    { valor: 'Otro', etiqueta: 'Otro' }
  ];

  ngOnInit(): void {
    this.prospectosService.obtener(this.prospectoId).subscribe((p) => {
      this.prospecto.set(p);
      if (!p.productoRequiereDeclaracionInversion) {
        // El Producto no requiere esta etapa: se omite (FR-006).
        this.router.navigate(['/requisitos', this.prospectoId], { replaceUrl: true });
        return;
      }

      this.prospectosService.obtenerDeclaracionInversion(this.prospectoId).subscribe({
        next: (declaracion) => {
          this.montoInvertir = declaracion.montoInvertir;
          this.origenFondos = declaracion.origenFondos;
          this.detalle = declaracion.detalle ?? '';
          this.cargando.set(false);
        },
        error: () => this.cargando.set(false)
      });
    });
  }

  guardar(): void {
    this.mensajeError.set(null);
    if (!this.montoInvertir || this.montoInvertir <= 0) {
      this.mensajeError.set('El Monto a Invertir debe ser mayor a 0.');
      return;
    }
    const montoSolicitado = this.prospecto()?.montoSolicitado;
    if (montoSolicitado != null && this.montoInvertir > montoSolicitado) {
      this.mensajeError.set('El Monto a Invertir no puede superar el Monto Solicitado.');
      return;
    }

    this.prospectosService
      .guardarDeclaracionInversion(this.prospectoId, {
        montoInvertir: this.montoInvertir,
        origenFondos: this.origenFondos,
        detalle: this.detalle || null
      })
      .subscribe({
        next: () => this.guardado.set(true),
        error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo guardar la Declaración de Inversión.')
      });
  }

  continuar(): void {
    this.router.navigate(['/requisitos', this.prospectoId]);
  }
}
