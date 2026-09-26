import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { BandejaItem, ProspectosService } from '../../core/services/prospectos.service';

/**
 * Bandeja del Asesor (Historia 1, spec 002: FR-001 a FR-003). Lista los prospectos con su
 * estado resumido y navega al paso exacto donde quedó cada uno. También ofrece, para los
 * estados que lo permiten, la edición de los datos del cliente (Historia 5, FR-015 a FR-017)
 * y el tooltip con el comentario del Aprobador (Historia 4, FR-014).
 */
@Component({
  selector: 'app-bandeja',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './bandeja.component.html'
})
export class BandejaComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly prospectosService = inject(ProspectosService);

  readonly items = signal<BandejaItem[]>([]);
  readonly detalleExpandidoId = signal<number | null>(null);

  private readonly RUTA_POR_PASO: Record<string, string> = {
    Simulacion: '/simulacion',
    Onboarding: '/onboarding',
    DeclaracionInversion: '/declaracion-inversion',
    Requisitos: '/requisitos',
    Desembolso: '/desembolso'
  };

  ngOnInit(): void {
    this.cargar();
  }

  private cargar(): void {
    this.prospectosService.bandeja().subscribe((datos) => this.items.set(datos));
  }

  etiquetaEstado(item: BandejaItem): string {
    switch (item.estadoResumen) {
      case 'Bloqueado':
        return 'Bloqueado';
      case 'EnProceso':
        return 'En proceso';
      case 'Aprobacion':
        return 'Aprobación';
      case 'Desembolsado':
        return 'Desembolsado';
      case 'Observado':
        return 'Observado';
    }
  }

  claseBadge(item: BandejaItem): string {
    switch (item.estadoResumen) {
      case 'Bloqueado':
        return 'text-bg-danger';
      case 'EnProceso':
        return 'text-bg-primary';
      case 'Aprobacion':
        return 'text-bg-info';
      case 'Desembolsado':
        return 'text-bg-success';
      case 'Observado':
        return 'text-bg-warning';
    }
  }

  esNavegable(item: BandejaItem): boolean {
    return item.estadoResumen === 'EnProceso' || item.estadoResumen === 'Observado';
  }

  /** FR-015/FR-016/FR-017 (Historia 5): editable en cualquier estado activo salvo Aprobación. */
  esEditable(item: BandejaItem): boolean {
    return item.estadoResumen === 'EnProceso' || item.estadoResumen === 'Observado';
  }

  continuar(item: BandejaItem): void {
    if (!item.pasoActual) return;
    const ruta = this.RUTA_POR_PASO[item.pasoActual];
    if (ruta) {
      this.router.navigate([ruta, item.prospectoId]);
    }
  }

  editarCliente(item: BandejaItem): void {
    this.router.navigate(['/onboarding', item.prospectoId]);
  }

  alternarDetalle(item: BandejaItem): void {
    this.detalleExpandidoId.set(this.detalleExpandidoId() === item.prospectoId ? null : item.prospectoId);
  }
}
