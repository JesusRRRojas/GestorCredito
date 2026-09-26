import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ClienteConCreditos,
  CreditosService,
  CronogramaCredito
} from '../../core/services/creditos.service';
import { MonedaPePipe } from '../../shared/pipes/moneda.pipe';
import { FechaPePipe } from '../../shared/pipes/fecha-pe.pipe';

const MENSAJES_ERROR: Record<string, string> = {
  NUMERO_OPERACION_REQUERIDO: 'El Número de Operación es obligatorio.',
  ARCHIVO_EVIDENCIA_INVALIDO:
    'El archivo de evidencia debe ser JPG, PNG o PDF y no superar 5 MB.',
  CREDITO_NO_TIENE_CUOTA_PENDIENTE: 'Este crédito no tiene ninguna cuota pendiente de pago.'
};

/**
 * Bandeja del Cajero (Historia 1, spec 003: FR-003 a FR-009). Rol de solo lectura salvo por
 * el registro de pago de la Cuota Actual de un crédito "Pendiente". Extendida en spec 004 con
 * el cronograma visual por cuota (Historia 1) y el Número de Operación/evidencia opcional al
 * registrar el pago (Historia 2).
 */
@Component({
  selector: 'app-creditos-cajero',
  standalone: true,
  imports: [FormsModule, MonedaPePipe, FechaPePipe],
  templateUrl: './creditos-cajero.component.html'
})
export class CreditosCajeroComponent implements OnInit {
  private readonly creditosService = inject(CreditosService);

  readonly clientes = signal<ClienteConCreditos[]>([]);
  readonly creditoSeleccionado = signal<{ numeroDocumento: string; prospectoId: number } | null>(null);
  readonly cronograma = signal<CronogramaCredito | null>(null);
  readonly mensajeCronograma = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);

  buscar = '';
  numeroOperacion = '';
  archivoEvidencia: File | null = null;

  ngOnInit(): void {
    this.recargar();
  }

  onBuscarCambia(): void {
    this.recargar();
  }

  private recargar(): void {
    this.creditosService.listar(this.buscar || undefined).subscribe((datos) => this.clientes.set(datos));
  }

  seleccionarCredito(numeroDocumento: string, prospectoId: number): void {
    this.mensaje.set(null);
    this.mensajeCronograma.set(null);
    this.cronograma.set(null);
    this.numeroOperacion = '';
    this.archivoEvidencia = null;
    this.creditoSeleccionado.set({ numeroDocumento, prospectoId });

    this.creditosService.obtenerCronograma(prospectoId).subscribe({
      next: (datos) => this.cronograma.set(datos),
      error: (err) =>
        this.mensajeCronograma.set(
          err.error?.error === 'CREDITO_SIN_CRONOGRAMA'
            ? 'Este crédito todavía no tiene cronograma (aún no ha sido desembolsado).'
            : 'No se pudo cargar el cronograma del crédito.'
        )
    });
  }

  estaSeleccionado(numeroDocumento: string, prospectoId: number): boolean {
    const s = this.creditoSeleccionado();
    return s?.numeroDocumento === numeroDocumento && s?.prospectoId === prospectoId;
  }

  onArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.archivoEvidencia = input.files?.[0] ?? null;
  }

  registrarPago(prospectoId: number): void {
    if (!this.numeroOperacion.trim()) {
      this.mensaje.set(MENSAJES_ERROR['NUMERO_OPERACION_REQUERIDO']);
      return;
    }

    this.mensaje.set(null);
    this.creditosService.pagarCuota(prospectoId, this.numeroOperacion, this.archivoEvidencia).subscribe({
      next: () => {
        this.mensaje.set('Pago registrado correctamente.');
        this.numeroOperacion = '';
        this.archivoEvidencia = null;
        this.creditoSeleccionado.set(null);
        this.cronograma.set(null);
        this.recargar();
      },
      error: (err) =>
        this.mensaje.set(MENSAJES_ERROR[err.error?.error] ?? 'No se pudo registrar el pago.')
    });
  }

  urlEvidencia(prospectoId: number, numero: number): string {
    return this.creditosService.urlEvidencia(prospectoId, numero);
  }
}
