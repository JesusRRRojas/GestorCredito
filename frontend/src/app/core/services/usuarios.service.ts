import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url';
import { Rol } from '../rol-activo.service';

export interface Usuario {
  id: number;
  nombre: string;
  rol: Rol;
  activo: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);

  listar(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(`${API_BASE_URL}/usuarios`);
  }

  crear(nombre: string, rol: Rol): Observable<Usuario> {
    return this.http.post<Usuario>(`${API_BASE_URL}/usuarios`, { nombre, rol });
  }

  editar(id: number, nombre: string, rol: Rol): Observable<Usuario> {
    return this.http.put<Usuario>(`${API_BASE_URL}/usuarios/${id}`, { nombre, rol });
  }

  deshabilitar(id: number): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/usuarios/${id}`);
  }
}
