import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProspectoResumen, ProspectosService } from '../../core/services/prospectos.service';

/**
 * Pantalla 6 - Desembolso: bandeja de prospectos listos.
 */
@Component({
  selector: 'app-desembolso',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './desembolso.component.html'
})
export class DesembolsoComponent implements OnInit {
  private readonly prospectosService = inject(ProspectosService);
  readonly prospectos = signal<ProspectoResumen[]>([]);

  ngOnInit(): void {
    this.prospectosService.listar('Desembolso').subscribe((datos) => this.prospectos.set(datos));
  }
}
