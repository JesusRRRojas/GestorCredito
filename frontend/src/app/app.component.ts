import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { RolActivoService } from './core/rol-activo.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app.component.html'
})
export class AppComponent {
  private readonly router = inject(Router);
  protected readonly rolActivo = inject(RolActivoService);

  cambiarRol(): void {
    this.rolActivo.limpiarRol();
    this.router.navigateByUrl('/');
  }
}
