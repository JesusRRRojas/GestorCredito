import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Producto, ProductosService, Requisito } from '../../../../core/services/productos.service';

/**
 * FR-003: Requisitos documentales de un Producto (solo relevante si
 * requiere_requisitos = true).
 */
@Component({
  selector: 'app-requisitos-producto',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './requisitos-producto.component.html'
})
export class RequisitosProductoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productosService = inject(ProductosService);

  private readonly productoId = Number(this.route.snapshot.paramMap.get('productoId'));

  readonly producto = signal<Producto | null>(null);
  readonly requisitos = signal<Requisito[]>([]);
  nuevoNombre = '';

  ngOnInit(): void {
    this.productosService.obtener(this.productoId).subscribe((p) => this.producto.set(p));
    this.cargarRequisitos();
  }

  cargarRequisitos(): void {
    this.productosService.listarRequisitos(this.productoId).subscribe((datos) => this.requisitos.set(datos));
  }

  crear(): void {
    if (!this.nuevoNombre.trim()) return;
    this.productosService.crearRequisito(this.productoId, this.nuevoNombre.trim()).subscribe(() => {
      this.nuevoNombre = '';
      this.cargarRequisitos();
    });
  }

  deshabilitar(id: number): void {
    this.productosService.deshabilitarRequisito(id).subscribe(() => this.cargarRequisitos());
  }
}
