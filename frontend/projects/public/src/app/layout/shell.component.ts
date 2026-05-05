import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService, ContextDebugComponent, GlobalStatusBannerComponent, SessionStateService } from 'shared';
import { EditionService } from '../services/edition.service';
import { HomeContentStateService } from '../services/home-content-state.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    GlobalStatusBannerComponent,
    ContextDebugComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  readonly auth       = inject(AuthService);
  readonly sessionState = inject(SessionStateService);
  readonly editionSvc = inject(EditionService);
  private readonly homeContentState = inject(HomeContentStateService);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);
  readonly menuPages = this.homeContentState.menuPages;

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout();
    void this.router.navigateByUrl('/');
  }
}
