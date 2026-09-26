import { ProspectoDetalle } from '../core/services/prospectos.service';

/** Describe la Cuenta Bancaria de Desembolso (Interna o Externa) de un Prospecto (FR-011, spec 002). */
export function descripcionCuentaDesembolso(p: ProspectoDetalle): string {
  if (p.tipoCuentaDesembolso === 'Interna') {
    return `Interna — Cuenta N° ${p.cuentaBancariaInternaId ?? ''}`;
  }
  if (p.tipoCuentaDesembolso === 'Externa') {
    return `Externa — ${p.cuentaExternaBanco} (CCI ${p.cuentaExternaCCI})`;
  }
  return 'Sin datos';
}
