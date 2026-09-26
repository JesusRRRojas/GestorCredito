import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';

export type EstadoProspecto =
  | 'Simulacion'
  | 'Evaluacion'
  | 'Aprobacion'
  | 'Observado'
  | 'Rechazado'
  | 'Desembolso'
  | 'Finalizado';

export type TipoCuentaDesembolso = 'Interna' | 'Externa';

export type OrigenFondos = 'Ahorros' | 'Herencia' | 'VentaActivo' | 'ActividadEmpresarial' | 'Otro';

export interface CuentaExterna {
  banco: string;
  cci: string;
}

export interface DeclaracionInversion {
  montoInvertir: number;
  origenFondos: OrigenFondos;
  detalle: string | null;
}

/** Fila de la bandeja del Asesor (FR-001, spec 002). */
export interface BandejaItem {
  prospectoId: number;
  nombreCliente: string | null;
  tipoDocumento: string;
  numeroDocumento: string;
  estadoResumen: 'Bloqueado' | 'EnProceso' | 'Aprobacion' | 'Desembolsado' | 'Observado';
  pasoActual: 'Simulacion' | 'Onboarding' | 'DeclaracionInversion' | 'Requisitos' | 'Desembolso' | null;
  comentarioAprobador: string | null;
}

export interface ValidacionResponse {
  prospectoId: number | null;
  resultadoMock: number;
  aprobado: boolean;
}

export interface Cuota {
  numero: number;
  fechaPago: string;
  saldoInicial: number;
  amortizacion: number;
  interes: number;
  otros: number;
  pagoTotal: number;
  saldoFinal: number;
}

export interface SimulacionResponse {
  productoId: number;
  monto: number;
  tasa: number;
  plazo: number;
  cuotas: Cuota[];
}

export interface ProspectoDetalle {
  id: number;
  tipoDocumentoId: number;
  numeroDocumento: string;
  productoId: number | null;
  productoNombre: string | null;
  productoRequiereRequisitos: boolean;
  productoRequiereDeclaracionInversion: boolean;
  nombres: string | null;
  apellidos: string | null;
  direccion: string | null;
  tipoCuentaDesembolso: TipoCuentaDesembolso | null;
  cuentaBancariaInternaId: number | null;
  cuentaExternaBanco: string | null;
  cuentaExternaCCI: string | null;
  estado: EstadoProspecto;
  fechaCreacion: string;
  fechaCierre: string | null;
  montoSolicitado: number | null;
}

export interface ProspectoResumen {
  id: number;
  numeroDocumento: string;
  productoNombre: string | null;
  estado: EstadoProspecto;
  nombreCompleto: string | null;
  fechaCierre: string | null;
}

export interface RequisitoConEstado {
  requisitoId: number;
  nombre: string;
  adjuntado: boolean;
}

export interface ArchivoAdjunto {
  id: number;
  requisitoId: number;
  requisitoNombre: string;
  nombreArchivo: string;
  fechaCarga: string;
}

@Injectable({ providedIn: 'root' })
export class ProspectosService {
  private readonly http = inject(HttpClient);

  validar(tipoDocumentoId: number, numeroDocumento: string): Observable<ValidacionResponse> {
    return this.http.post<ValidacionResponse>(`${API_BASE_URL}/prospectos/validaciones`, {
      tipoDocumentoId,
      numeroDocumento
    });
  }

  obtener(id: number): Observable<ProspectoDetalle> {
    return this.http.get<ProspectoDetalle>(`${API_BASE_URL}/prospectos/${id}`);
  }

  listar(estado?: EstadoProspecto): Observable<ProspectoResumen[]> {
    const params: Record<string, string> = estado ? { estado } : {};
    return this.http.get<ProspectoResumen[]>(`${API_BASE_URL}/prospectos`, { params });
  }

  simular(prospectoId: number, productoId: number, monto: number, tasa: number, plazo: number): Observable<SimulacionResponse> {
    return this.http.post<SimulacionResponse>(`${API_BASE_URL}/prospectos/${prospectoId}/simulaciones`, {
      productoId,
      monto,
      tasa,
      plazo
    });
  }

  aceptarSimulacion(prospectoId: number, productoId: number, monto: number, tasa: number, plazo: number): Observable<{ simulacionId: number }> {
    return this.http.post<{ simulacionId: number }>(`${API_BASE_URL}/prospectos/${prospectoId}/simulaciones/aceptar`, {
      productoId,
      monto,
      tasa,
      plazo
    });
  }

  urlPdfCronograma(prospectoId: number): string {
    return `${API_BASE_URL}/prospectos/${prospectoId}/simulaciones/actual/pdf`;
  }

  bandeja(): Observable<BandejaItem[]> {
    return this.http.get<BandejaItem[]>(`${API_BASE_URL}/prospectos/bandeja`);
  }

  guardarOnboarding(
    prospectoId: number,
    nombres: string,
    apellidos: string,
    direccion: string,
    tipoCuentaDesembolso: TipoCuentaDesembolso,
    cuentaBancariaInternaId: number | null,
    cuentaExterna: CuentaExterna | null
  ): Observable<ProspectoDetalle> {
    return this.http.put<ProspectoDetalle>(`${API_BASE_URL}/prospectos/${prospectoId}/onboarding`, {
      nombres,
      apellidos,
      direccion,
      tipoCuentaDesembolso,
      cuentaBancariaInternaId,
      cuentaExterna
    });
  }

  obtenerDeclaracionInversion(prospectoId: number): Observable<DeclaracionInversion> {
    return this.http.get<DeclaracionInversion>(`${API_BASE_URL}/prospectos/${prospectoId}/declaracion-inversion`);
  }

  guardarDeclaracionInversion(prospectoId: number, dto: DeclaracionInversion): Observable<DeclaracionInversion> {
    return this.http.put<DeclaracionInversion>(
      `${API_BASE_URL}/prospectos/${prospectoId}/declaracion-inversion`,
      dto
    );
  }

  listarRequisitos(prospectoId: number): Observable<RequisitoConEstado[]> {
    return this.http.get<RequisitoConEstado[]>(`${API_BASE_URL}/prospectos/${prospectoId}/requisitos`);
  }

  subirArchivoRequisito(prospectoId: number, requisitoId: number, archivo: File): Observable<void> {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.post<void>(
      `${API_BASE_URL}/prospectos/${prospectoId}/requisitos/${requisitoId}/archivo`,
      formData
    );
  }

  completarRequisitos(prospectoId: number): Observable<{ estado: EstadoProspecto }> {
    return this.http.post<{ estado: EstadoProspecto }>(
      `${API_BASE_URL}/prospectos/${prospectoId}/requisitos/completar`,
      {}
    );
  }

  listarArchivosAdjuntos(prospectoId: number): Observable<ArchivoAdjunto[]> {
    return this.http.get<ArchivoAdjunto[]>(`${API_BASE_URL}/prospectos/${prospectoId}/requisitos/archivos`);
  }

  urlArchivoAdjunto(prospectoId: number, requisitoId: number): string {
    return `${API_BASE_URL}/prospectos/${prospectoId}/requisitos/${requisitoId}/archivo`;
  }

  decidir(
    prospectoId: number,
    decision: 'Aprobado' | 'Observado' | 'Rechazado',
    comentario?: string
  ): Observable<{ estado: EstadoProspecto }> {
    return this.http.post<{ estado: EstadoProspecto }>(`${API_BASE_URL}/prospectos/${prospectoId}/decision`, {
      decision,
      comentario
    });
  }

  urlPdfAprobacionFinal(prospectoId: number): string {
    return `${API_BASE_URL}/prospectos/${prospectoId}/aprobacion-final/pdf`;
  }

  cerrar(prospectoId: number): Observable<{ estado: EstadoProspecto }> {
    return this.http.post<{ estado: EstadoProspecto }>(`${API_BASE_URL}/prospectos/${prospectoId}/cerrar`, {});
  }
}
