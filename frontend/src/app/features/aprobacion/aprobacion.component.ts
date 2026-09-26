import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProspectoResumen, ProspectosService } from '../../core/services/prospectos.service';

/**
 * Pantalla 5 - Aprobación: bandeja de prospectos pendientes (FR-023).
 */
@Component({
  selector: 'app-aprobacion',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './aprobacion.component.html'
})
export class AprobacionComponent implements OnInit {
  private readonly prospectosService = inject(ProspectosService);
  readonly prospectos = signal<ProspectoResumen[]>([]);

  ngOnInit(): void {
    this.prospectosService.listar('Aprobacion').subscribe((datos) => this.prospectos.set(datos));
  }
}
