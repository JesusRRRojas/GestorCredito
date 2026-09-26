import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';

export interface CuentaInterna {
  id: number;
  numeroCuenta: string;
  moneda: 'PEN';
}

@Injectable({ providedIn: 'root' })
export class CuentasInternasService {
  private readonly http = inject(HttpClient);

  private base(tipoDocumentoId: number, numeroDocumento: string): string {
    return `${API_BASE_URL}/clientes/${tipoDocumentoId}/${numeroDocumento}/cuentas-internas`;
  }

  listar(tipoDocumentoId: number, numeroDocumento: string): Observable<CuentaInterna[]> {
    return this.http.get<CuentaInterna[]>(this.base(tipoDocumentoId, numeroDocumento));
  }

  agregar(tipoDocumentoId: number, numeroDocumento: string, numeroCuenta: string): Observable<CuentaInterna> {
    return this.http.post<CuentaInterna>(this.base(tipoDocumentoId, numeroDocumento), { numeroCuenta });
  }
}
