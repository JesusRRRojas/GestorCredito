import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';

export interface TipoDocumento {
  id: number;
  nombre: string;
  activo: boolean;
  tipoPersonaIds: number[];
}

export interface TipoPersona {
  id: number;
  nombre: string;
  activo: boolean;
}

@Injectable({ providedIn: 'root' })
export class TiposService {
  private readonly http = inject(HttpClient);

  listarTiposDocumento(): Observable<TipoDocumento[]> {
    return this.http.get<TipoDocumento[]>(`${API_BASE_URL}/tipos-documento`);
  }

  crearTipoDocumento(nombre: string, tipoPersonaIds: number[]): Observable<TipoDocumento> {
    return this.http.post<TipoDocumento>(`${API_BASE_URL}/tipos-documento`, { nombre, tipoPersonaIds });
  }

  editarTipoDocumento(id: number, nombre: string, activo: boolean, tipoPersonaIds: number[]): Observable<TipoDocumento> {
    return this.http.put<TipoDocumento>(`${API_BASE_URL}/tipos-documento/${id}`, { nombre, activo, tipoPersonaIds });
  }

  deshabilitarTipoDocumento(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/tipos-documento/${id}`);
  }

  listarTiposPersona(): Observable<TipoPersona[]> {
    return this.http.get<TipoPersona[]>(`${API_BASE_URL}/tipos-persona`);
  }

  crearTipoPersona(nombre: string): Observable<TipoPersona> {
    return this.http.post<TipoPersona>(`${API_BASE_URL}/tipos-persona`, { nombre });
  }

  editarTipoPersona(id: number, nombre: string, activo: boolean): Observable<TipoPersona> {
    return this.http.put<TipoPersona>(`${API_BASE_URL}/tipos-persona/${id}`, { nombre, activo });
  }

  deshabilitarTipoPersona(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/tipos-persona/${id}`);
  }
}
