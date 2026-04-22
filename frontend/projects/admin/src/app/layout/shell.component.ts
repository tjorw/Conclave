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

type NavSection = 'editions' | 'persons' | 'events' | 'staffing' | 'visitors';

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
  readonly collapsedNavSections: Record<NavSection, boolean> = {
    editions: true,
    persons: true,
    events: true,
    staffing: true,
    visitors: true,
  };

  ngOnInit(): void {
    this.editionContext.load();
  }

  onEditionChange(editionId: string): void {
    this.editionContext.setActive(editionId);
  }

  isNavSectionCollapsed(section: NavSection): boolean {
    return this.collapsedNavSections[section];
  }

  toggleNavSection(section: NavSection): void {
    this.collapsedNavSections[section] = !this.collapsedNavSections[section];
  }

  navSectionAriaLabel(sectionLabel: string, section: NavSection): string {
    const action = this.isNavSectionCollapsed(section) ? this.NAV.showSection : this.NAV.hideSection;
    return `${action}: ${sectionLabel}`;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
