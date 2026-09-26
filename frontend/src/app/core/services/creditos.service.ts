import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';

export interface CuotaActual {
  numero: number;
  fechaPago: string;
  pagoTotal: number;
}

export type EstadoCredito = 'EnProceso' | 'Pendiente' | 'Cancelado';

export interface CreditoResumen {
  prospectoId: number;
  productoNombre: string | null;
  montoSolicitado: number;
  estadoCredito: EstadoCredito;
  cuotaActual: CuotaActual | null;
}

export interface ClienteConCreditos {
  tipoDocumentoId: number;
  tipoDocumento: string;
  numeroDocumento: string;
  nombreCompleto: string;
  creditos: CreditoResumen[];
}

export type EstadoCuota = 'Pagada' | 'Actual' | 'Futura';

export interface CronogramaCuota {
  numero: number;
  fechaPago: string;
  pagoTotal: number;
  estadoCuota: EstadoCuota;
  numeroOperacion: string | null;
  tieneEvidencia: boolean;
}

export interface CronogramaCredito {
  estadoCredito: EstadoCredito;
  cuotasPagadas: number;
  totalCuotas: number;
  cuotas: CronogramaCuota[];
}

/** Créditos (Historias 1 y 2, spec 003; cronograma y comprobante de pago, spec 004): concepto
 * derivado de Prospecto + simulación aceptada. */
@Injectable({ providedIn: 'root' })
export class CreditosService {
  private readonly http = inject(HttpClient);

  listar(buscar?: string): Observable<ClienteConCreditos[]> {
    const params: Record<string, string> = buscar ? { buscar } : {};
    return this.http.get<ClienteConCreditos[]>(`${API_BASE_URL}/creditos`, { params });
  }

  obtenerCronograma(prospectoId: number): Observable<CronogramaCredito> {
    return this.http.get<CronogramaCredito>(`${API_BASE_URL}/creditos/${prospectoId}/cronograma`);
  }

  pagarCuota(
    prospectoId: number,
    numeroOperacion: string,
    archivo: File | null
  ): Observable<{ estadoCredito: EstadoCredito; cuotaActual: CuotaActual | null }> {
    const formData = new FormData();
    formData.append('numeroOperacion', numeroOperacion);
    if (archivo) {
      formData.append('archivo', archivo);
    }
    return this.http.post<{ estadoCredito: EstadoCredito; cuotaActual: CuotaActual | null }>(
      `${API_BASE_URL}/creditos/${prospectoId}/pagar-cuota`,
      formData
    );
  }

  urlEvidencia(prospectoId: number, numero: number): string {
    return `${API_BASE_URL}/creditos/${prospectoId}/cuotas/${numero}/evidencia`;
  }
}
