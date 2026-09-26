import { Pipe, PipeTransform } from '@angular/core';

/**
 * Formato peruano de fechas (Principio II de la constitución): DD/MM/AAAA.
 */
@Pipe({ name: 'fechaPe', standalone: true })
export class FechaPePipe implements PipeTransform {
  transform(valor: string | Date | null | undefined): string {
    if (!valor) {
      return '';
    }

    // Las fechas "solo día" (DateOnly del backend, ej. "2026-10-05") se leen por
    // componentes, sin pasar por el constructor Date/UTC: interpretar ese string como
    // UTC y luego mostrarlo en la zona horaria local del navegador puede correr la
    // fecha un día hacia atrás (ej. Perú, UTC-5).
    if (typeof valor === 'string') {
      const coincidencia = /^(\d{4})-(\d{2})-(\d{2})/.exec(valor);
      if (coincidencia) {
        const [, anio, mes, dia] = coincidencia;
        return `${dia}/${mes}/${anio}`;
      }
    }

    const fecha = typeof valor === 'string' ? new Date(valor) : valor;
    if (Number.isNaN(fecha.getTime())) {
      return '';
    }
    const dia = fecha.getDate().toString().padStart(2, '0');
    const mes = (fecha.getMonth() + 1).toString().padStart(2, '0');
    const anio = fecha.getFullYear();
    return `${dia}/${mes}/${anio}`;
  }
}
