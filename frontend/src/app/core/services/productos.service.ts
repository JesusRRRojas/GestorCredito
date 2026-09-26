import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';

export type DiaPago = 'Dia5' | 'Dia25';

export interface Producto {
  id: number;
  nombre: string;
  moneda: 'PEN';
  montoMin: number;
  montoMax: number;
  tasaMin: number;
  tasaMax: number;
  plazoMin: number;
  plazoMax: number;
  frecuenciaPago: 'Mensual';
  diaPago: DiaPago;
  cargoOtrosPorCuota: number;
  requiereRequisitos: boolean;
  requiereDeclaracionInversion: boolean;
  activo: boolean;
}

export interface ProductoGuardar {
  nombre: string;
  montoMin: number;
  montoMax: number;
  tasaMin: number;
  tasaMax: number;
  plazoMin: number;
  plazoMax: number;
  diaPago: DiaPago;
  cargoOtrosPorCuota: number;
  requiereRequisitos: boolean;
  requiereDeclaracionInversion: boolean;
}

export interface Requisito {
  id: number;
  productoId: number;
  nombre: string;
  activo: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductosService {
  private readonly http = inject(HttpClient);

  listar(soloActivos = false): Observable<Producto[]> {
    const params: Record<string, string> = soloActivos ? { activos: 'true' } : {};
    return this.http.get<Producto[]>(`${API_BASE_URL}/productos`, { params });
  }

  obtener(id: number): Observable<Producto> {
    return this.http.get<Producto>(`${API_BASE_URL}/productos/${id}`);
  }

  crear(dto: ProductoGuardar): Observable<Producto> {
    return this.http.post<Producto>(`${API_BASE_URL}/productos`, dto);
  }

  editar(id: number, dto: ProductoGuardar): Observable<Producto> {
    return this.http.put<Producto>(`${API_BASE_URL}/productos/${id}`, dto);
  }

  deshabilitar(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/productos/${id}`);
  }

  listarRequisitos(productoId: number): Observable<Requisito[]> {
    return this.http.get<Requisito[]>(`${API_BASE_URL}/productos/${productoId}/requisitos`);
  }

  crearRequisito(productoId: number, nombre: string): Observable<Requisito> {
    return this.http.post<Requisito>(`${API_BASE_URL}/productos/${productoId}/requisitos`, { nombre });
  }

  editarRequisito(id: number, nombre: string, activo: boolean): Observable<Requisito> {
    return this.http.put<Requisito>(`${API_BASE_URL}/requisitos/${id}`, { nombre, activo });
  }

  deshabilitarRequisito(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/requisitos/${id}`);
  }
}
