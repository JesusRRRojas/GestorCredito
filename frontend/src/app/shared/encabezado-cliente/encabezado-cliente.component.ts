import { Component, Input } from '@angular/core';
import { ProspectoDetalle } from '../../core/services/prospectos.service';
import { MonedaPePipe } from '../pipes/moneda.pipe';

/**
 * Encabezado "Nombre — Monto Solicitado" (Historia 4, spec 003: FR-017). No renderiza nada
 * si el prospecto, su Nombre o su Monto Solicitado todavía no existen.
 */
@Component({
  selector: 'app-encabezado-cliente',
  standalone: true,
  imports: [MonedaPePipe],
  templateUrl: './encabezado-cliente.component.html'
})
export class EncabezadoClienteComponent {
  @Input() prospecto: ProspectoDetalle | null = null;

  get visible(): boolean {
    return !!this.prospecto?.nombres && this.prospecto?.montoSolicitado != null;
  }
}
