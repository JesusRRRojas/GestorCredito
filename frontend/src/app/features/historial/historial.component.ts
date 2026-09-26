import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClienteConCreditos, CreditosService } from '../../core/services/creditos.service';
import { FechaPePipe } from '../../shared/pipes/fecha-pe.pipe';
import { MonedaPePipe } from '../../shared/pipes/moneda.pipe';

/**
 * Créditos del Cliente (Historia 2, spec 003: FR-010 a FR-013). Evolución de la pantalla de
 * Historial de `001-gestion-creditos` (antes limitada a prospectos cerrados) — research.md §2.
 */
@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [FormsModule, FechaPePipe, MonedaPePipe],
  templateUrl: './historial.component.html'
})
export class HistorialComponent {
  private readonly creditosService = inject(CreditosService);

  readonly resultados = signal<ClienteConCreditos[]>([]);
  readonly buscado = signal(false);
  numeroDocumento = '';

  buscar(): void {
    if (!this.numeroDocumento.trim()) return;
    this.creditosService.listar(this.numeroDocumento.trim()).subscribe((datos) => {
      this.resultados.set(datos);
      this.buscado.set(true);
    });
  }
}
