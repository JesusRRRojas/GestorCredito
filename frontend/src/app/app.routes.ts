import { Routes } from '@angular/router';
import { rolGuard } from './core/rol.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./core/selector-rol/selector-rol.component').then((m) => m.SelectorRolComponent)
  },

  // Configuración (US1 - Administrador)
  {
    path: 'configuracion/tipos-documento',
    canActivate: [rolGuard(['Administrador'])],
    loadComponent: () =>
      import('./features/configuracion/tipos-documento/tipos-documento.component').then(
        (m) => m.TiposDocumentoComponent
      )
  },
  {
    path: 'configuracion/productos',
    canActivate: [rolGuard(['Administrador'])],
    loadComponent: () =>
      import('./features/configuracion/productos/productos.component').then(
        (m) => m.ProductosComponent
      )
  },
  {
    path: 'configuracion/productos/:productoId/requisitos',
    canActivate: [rolGuard(['Administrador'])],
    loadComponent: () =>
      import('./features/configuracion/productos/requisitos/requisitos-producto.component').then(
        (m) => m.RequisitosProductoComponent
      )
  },
  {
    path: 'configuracion/usuarios',
    canActivate: [rolGuard(['Administrador'])],
    loadComponent: () =>
      import('./features/configuracion/usuarios/usuarios.component').then(
        (m) => m.UsuariosComponent
      )
  },

  // Bandeja del Asesor (US1 - spec 002)
  {
    path: 'bandeja',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/bandeja/bandeja.component').then((m) => m.BandejaComponent)
  },

  // Validación y Simulación (US2 - Asesor)
  {
    path: 'validacion',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/validacion/validacion.component').then((m) => m.ValidacionComponent)
  },
  {
    path: 'simulacion/:prospectoId',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/simulacion/simulacion.component').then((m) => m.SimulacionComponent)
  },

  // Onboarding y Requisitos (US3 - Asesor)
  {
    path: 'onboarding/:prospectoId',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/onboarding/onboarding.component').then((m) => m.OnboardingComponent)
  },
  {
    path: 'requisitos/:prospectoId',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/requisitos/requisitos.component').then((m) => m.RequisitosComponent)
  },
  // Declaración de Inversión (US3 - Asesor - spec 002)
  {
    path: 'declaracion-inversion/:prospectoId',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/declaracion-inversion/declaracion-inversion.component').then(
        (m) => m.DeclaracionInversionComponent
      )
  },

  // Aprobación (US3 - Aprobador)
  {
    path: 'aprobacion',
    canActivate: [rolGuard(['Aprobador'])],
    loadComponent: () =>
      import('./features/aprobacion/aprobacion.component').then((m) => m.AprobacionComponent)
  },
  {
    path: 'aprobacion/:prospectoId',
    canActivate: [rolGuard(['Aprobador'])],
    loadComponent: () =>
      import('./features/aprobacion/aprobacion-detalle.component').then(
        (m) => m.AprobacionDetalleComponent
      )
  },

  // Desembolso (US4 - Asesor)
  {
    path: 'desembolso',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/desembolso/desembolso.component').then((m) => m.DesembolsoComponent)
  },
  {
    path: 'desembolso/:prospectoId',
    canActivate: [rolGuard(['Asesor'])],
    loadComponent: () =>
      import('./features/desembolso/desembolso-detalle.component').then(
        (m) => m.DesembolsoDetalleComponent
      )
  },

  // Bandeja del Cajero (US1 - spec 003)
  {
    path: 'creditos-cajero',
    canActivate: [rolGuard(['Cajero'])],
    loadComponent: () =>
      import('./features/creditos-cajero/creditos-cajero.component').then(
        (m) => m.CreditosCajeroComponent
      )
  },

  // Historial (US5 - Asesor / Administrador)
  {
    path: 'historial',
    canActivate: [rolGuard(['Asesor', 'Administrador'])],
    loadComponent: () =>
      import('./features/historial/historial.component').then((m) => m.HistorialComponent)
  },

  { path: '**', redirectTo: '' }
];
