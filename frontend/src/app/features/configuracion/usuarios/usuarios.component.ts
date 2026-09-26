import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Rol } from '../../../core/rol-activo.service';
import { Usuario, UsuariosService } from '../../../core/services/usuarios.service';

/**
 * FR-004: gestión administrativa de Usuarios (registro de referencia; ver
 * research.md §5 — no gatilla ninguna sesión).
 */
@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './usuarios.component.html'
})
export class UsuariosComponent implements OnInit {
  private readonly usuariosService = inject(UsuariosService);

  readonly usuarios = signal<Usuario[]>([]);
  readonly roles: Rol[] = ['Administrador', 'Asesor', 'Aprobador', 'Cajero'];

  editandoId: number | null = null;
  nombre = '';
  rol: Rol = 'Asesor';

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.usuariosService.listar().subscribe((datos) => this.usuarios.set(datos));
  }

  editar(u: Usuario): void {
    this.editandoId = u.id;
    this.nombre = u.nombre;
    this.rol = u.rol;
  }

  cancelarEdicion(): void {
    this.editandoId = null;
    this.nombre = '';
    this.rol = 'Asesor';
  }

  guardar(): void {
    if (!this.nombre.trim()) return;
    const accion = this.editandoId
      ? this.usuariosService.editar(this.editandoId, this.nombre.trim(), this.rol)
      : this.usuariosService.crear(this.nombre.trim(), this.rol);

    accion.subscribe(() => {
      this.cancelarEdicion();
      this.cargar();
    });
  }

  deshabilitar(id: number): void {
    this.usuariosService.deshabilitar(id).subscribe(() => this.cargar());
  }
}
