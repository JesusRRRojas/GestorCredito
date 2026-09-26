import { Pipe, PipeTransform } from '@angular/core';

/**
 * Formato peruano de montos (Principio II de la constitución): símbolo S/ y 2
 * decimales. Esta versión solo soporta Soles (research.md §7).
 */
@Pipe({ name: 'monedaPe', standalone: true })
export class MonedaPePipe implements PipeTransform {
  transform(valor: number | null | undefined): string {
    if (valor === null || valor === undefined) {
      return '';
    }
    const formateado = valor.toLocaleString('es-PE', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
    return `S/ ${formateado}`;
  }
}
