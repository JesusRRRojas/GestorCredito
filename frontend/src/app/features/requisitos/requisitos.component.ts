import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProspectoDetalle, ProspectosService, RequisitoConEstado } from '../../core/services/prospectos.service';
import { EncabezadoClienteComponent } from '../../shared/encabezado-cliente/encabezado-cliente.component';

/**
 * Pantalla 4 - Requisitos (Bifurcación Condicional, FR-020 a FR-022).
 */
@Component({
  selector: 'app-requisitos',
  standalone: true,
  imports: [EncabezadoClienteComponent],
  templateUrl: './requisitos.component.html'
})
export class RequisitosComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly prospectosService = inject(ProspectosService);

  readonly prospectoId = Number(this.route.snapshot.paramMap.get('prospectoId'));
  readonly prospecto = signal<ProspectoDetalle | null>(null);
  readonly requisitos = signal<RequisitoConEstado[]>([]);
  readonly mensajeError = signal<string | null>(null);
  readonly completado = signal(false);

  ngOnInit(): void {
    this.prospectosService.obtener(this.prospectoId).subscribe((p) => {
      this.prospecto.set(p);
      if (p.productoRequiereRequisitos) {
        this.cargarRequisitos();
      } else {
        // Caso B: se omite esta pantalla y se autogenera el Desembolso (FR-022).
        this.completarAutomaticamente();
      }
    });
  }

  private cargarRequisitos(): void {
    this.prospectosService.listarRequisitos(this.prospectoId).subscribe((datos) => this.requisitos.set(datos));
  }

  private completarAutomaticamente(): void {
    this.prospectosService.completarRequisitos(this.prospectoId).subscribe(() => this.completado.set(true));
  }

  subirArchivo(requisitoId: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    if (!archivo) return;

    this.mensajeError.set(null);
    this.prospectosService.subirArchivoRequisito(this.prospectoId, requisitoId, archivo).subscribe({
      next: () => this.cargarRequisitos(),
      error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo subir el archivo.')
    });
  }

  todosAdjuntados(): boolean {
    return this.requisitos().length > 0 && this.requisitos().every((r) => r.adjuntado);
  }

  completar(): void {
    this.mensajeError.set(null);
    this.prospectosService.completarRequisitos(this.prospectoId).subscribe({
      next: () => this.completado.set(true),
      error: (err) => this.mensajeError.set(err.error?.error ?? 'No se pudo completar.')
    });
  }

  irADesembolso(): void {
    this.router.navigateByUrl('/desembolso');
  }
}
