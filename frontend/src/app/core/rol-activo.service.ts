import { Injectable, signal } from '@angular/core';

export type Rol = 'Administrador' | 'Asesor' | 'Aprobador' | 'Cajero';

const CLAVE_SESION = 'gestor-credito.rol-activo';

/**
 * FR-005: guarda el rol elegido por el usuario para la sesión del navegador. No hay
 * usuario ni contraseña (research.md §4): el rol solo controla qué pantallas se
 * muestran en el frontend, la API no valida identidad ni rol del lado del servidor.
 */
@Injectable({ providedIn: 'root' })
export class RolActivoService {
  private readonly rolSignal = signal<Rol | null>(this.leerDeSesion());

  readonly rol = this.rolSignal.asReadonly();

  establecerRol(rol: Rol): void {
    this.rolSignal.set(rol);
    try {
      sessionStorage.setItem(CLAVE_SESION, rol);
    } catch {
      // sessionStorage puede no estar disponible (modo privado); el rol sigue
      // funcionando en memoria durante la sesión actual.
    }
  }

  limpiarRol(): void {
    this.rolSignal.set(null);
    try {
      sessionStorage.removeItem(CLAVE_SESION);
    } catch {
      // ignorar
    }
  }

  private leerDeSesion(): Rol | null {
    try {
      const valor = sessionStorage.getItem(CLAVE_SESION);
      return valor === 'Administrador' || valor === 'Asesor' || valor === 'Aprobador' ||
        valor === 'Cajero'
        ? valor
        : null;
    } catch {
      return null;
    }
  }
}
