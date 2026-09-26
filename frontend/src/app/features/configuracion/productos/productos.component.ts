import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DiaPago, Producto, ProductoGuardar, ProductosService } from '../../../core/services/productos.service';
import { MonedaPePipe } from '../../../shared/pipes/moneda.pipe';

const PRODUCTO_VACIO: ProductoGuardar = {
  nombre: '',
  montoMin: 0,
  montoMax: 0,
  tasaMin: 0,
  tasaMax: 0,
  plazoMin: 1,
  plazoMax: 1,
  diaPago: 'Dia5',
  cargoOtrosPorCuota: 0,
  requiereRequisitos: false,
  requiereDeclaracionInversion: false
};

/**
 * FR-002: CRUD de Productos de crédito.
 */
@Component({
  selector: 'app-productos',
  standalone: true,
  imports: [FormsModule, RouterLink, MonedaPePipe],
  templateUrl: './productos.component.html'
})
export class ProductosComponent implements OnInit {
  private readonly productosService = inject(ProductosService);

  readonly productos = signal<Producto[]>([]);
  readonly error = signal<string | null>(null);

  editandoId: number | null = null;
  form: ProductoGuardar = { ...PRODUCTO_VACIO };

  readonly diasPago: DiaPago[] = ['Dia5', 'Dia25'];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.productosService.listar().subscribe((datos) => this.productos.set(datos));
  }

  editar(producto: Producto): void {
    this.editandoId = producto.id;
    this.form = {
      nombre: producto.nombre,
      montoMin: producto.montoMin,
      montoMax: producto.montoMax,
      tasaMin: producto.tasaMin,
      tasaMax: producto.tasaMax,
      plazoMin: producto.plazoMin,
      plazoMax: producto.plazoMax,
      diaPago: producto.diaPago,
      cargoOtrosPorCuota: producto.cargoOtrosPorCuota,
      requiereRequisitos: producto.requiereRequisitos,
      requiereDeclaracionInversion: producto.requiereDeclaracionInversion
    };
  }

  cancelarEdicion(): void {
    this.editandoId = null;
    this.form = { ...PRODUCTO_VACIO };
  }

  guardar(): void {
    this.error.set(null);
    const accion = this.editandoId
      ? this.productosService.editar(this.editandoId, this.form)
      : this.productosService.crear(this.form);

    accion.subscribe({
      next: () => {
        this.cancelarEdicion();
        this.cargar();
      },
      error: (err) => this.error.set(err.error?.error ?? 'No se pudo guardar el Producto.')
    });
  }

  deshabilitar(id: number): void {
    this.productosService.deshabilitar(id).subscribe(() => this.cargar());
  }
}
