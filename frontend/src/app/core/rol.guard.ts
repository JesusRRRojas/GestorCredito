import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Rol, RolActivoService } from './rol-activo.service';

/**
 * Fábrica de guard: solo permite entrar a la ruta si hay un rol activo y, si se
 * especifican roles permitidos, si el rol activo está entre ellos (FR-005).
 */
export function rolGuard(rolesPermitidos?: Rol[]): CanActivateFn {
  return () => {
    const rolActivo = inject(RolActivoService);
    const router = inject(Router);
    const rol = rolActivo.rol();

    if (!rol) {
      return router.parseUrl('/');
    }

    if (rolesPermitidos && !rolesPermitidos.includes(rol)) {
      return router.parseUrl('/');
    }

    return true;
  };
}
