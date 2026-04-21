import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatDividerModule } from '@angular/material/divider';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AuthService, ContextDebugComponent, GlobalStatusBannerComponent, SessionStateService } from 'shared';
import { EditionContextService } from '../services/edition-context.service';
import { NAV } from '../labels/nav.labels';
import { ACTION } from '../labels/ui.labels';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatDividerModule,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatSelectModule,
    MatFormFieldModule,
    GlobalStatusBannerComponent,
    ContextDebugComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly editionContext = inject(EditionContextService);
  readonly sessionState = inject(SessionStateService);

  readonly NAV    = NAV;
  readonly ACTION = ACTION;

  ngOnInit(): void {
    this.editionContext.load();
  }

  onEditionChange(editionId: string): void {
    this.editionContext.setActive(editionId);
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
