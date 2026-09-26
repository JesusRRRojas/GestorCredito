import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Rol, RolActivoService } from '../rol-activo.service';

/**
 * FR-005: pantalla mostrada al ingresar a la aplicación (o al cambiar de rol) donde el
 * usuario elige con qué rol va a trabajar, sin usuario ni contraseña.
 */
@Component({
  selector: 'app-selector-rol',
  standalone: true,
  templateUrl: './selector-rol.component.html'
})
export class SelectorRolComponent {
  private readonly rolActivo = inject(RolActivoService);
  private readonly router = inject(Router);

  elegirRol(rol: Rol): void {
    this.rolActivo.establecerRol(rol);

    const destinoPorRol: Record<Rol, string> = {
      Administrador: '/configuracion/tipos-documento',
      Asesor: '/bandeja',
      Aprobador: '/aprobacion',
      Cajero: '/creditos-cajero'
    };

    this.router.navigateByUrl(destinoPorRol[rol]);
  }
}
