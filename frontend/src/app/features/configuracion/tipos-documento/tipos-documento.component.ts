import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TipoDocumento, TipoPersona, TiposService } from '../../../core/services/tipos.service';

/**
 * FR-001: CRUD de Tipos de Documento y Tipos de Persona, y su asociación.
 */
@Component({
  selector: 'app-tipos-documento',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './tipos-documento.component.html'
})
export class TiposDocumentoComponent implements OnInit {
  private readonly tiposService = inject(TiposService);

  readonly tiposDocumento = signal<TipoDocumento[]>([]);
  readonly tiposPersona = signal<TipoPersona[]>([]);

  nuevoNombreDocumento = '';
  nuevosTipoPersonaIds: number[] = [];
  nuevoNombrePersona = '';

  ngOnInit(): void {
    this.cargarTiposPersona();
    this.cargarTiposDocumento();
  }

  cargarTiposDocumento(): void {
    this.tiposService.listarTiposDocumento().subscribe((datos) => this.tiposDocumento.set(datos));
  }

  cargarTiposPersona(): void {
    this.tiposService.listarTiposPersona().subscribe((datos) => this.tiposPersona.set(datos));
  }

  nombrePersona(id: number): string {
    return this.tiposPersona().find((p) => p.id === id)?.nombre ?? '';
  }

  crearTipoDocumento(): void {
    if (!this.nuevoNombreDocumento.trim()) return;
    this.tiposService
      .crearTipoDocumento(this.nuevoNombreDocumento.trim(), this.nuevosTipoPersonaIds)
      .subscribe(() => {
        this.nuevoNombreDocumento = '';
        this.nuevosTipoPersonaIds = [];
        this.cargarTiposDocumento();
      });
  }

  deshabilitarTipoDocumento(id: number): void {
    this.tiposService.deshabilitarTipoDocumento(id).subscribe(() => this.cargarTiposDocumento());
  }

  crearTipoPersona(): void {
    if (!this.nuevoNombrePersona.trim()) return;
    this.tiposService.crearTipoPersona(this.nuevoNombrePersona.trim()).subscribe(() => {
      this.nuevoNombrePersona = '';
      this.cargarTiposPersona();
    });
  }

  deshabilitarTipoPersona(id: number): void {
    this.tiposService.deshabilitarTipoPersona(id).subscribe(() => this.cargarTiposPersona());
  }
}
