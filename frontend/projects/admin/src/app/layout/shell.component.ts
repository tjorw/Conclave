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

const COLLAPSED_NAV_SECTIONS_STORAGE_KEY = 'admin_collapsed_nav_sections';
const NAV_SECTIONS: readonly NavSection[] = ['editions', 'persons', 'events', 'staffing', 'visitors'];
const DEFAULT_COLLAPSED_NAV_SECTIONS: Record<NavSection, boolean> = {
  editions: true,
  persons: true,
  events: true,
  staffing: true,
  visitors: true,
};

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
  readonly collapsedNavSections: Record<NavSection, boolean> = this.loadCollapsedNavSections();

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
    this.saveCollapsedNavSections();
  }

  navSectionAriaLabel(sectionLabel: string, section: NavSection): string {
    const action = this.isNavSectionCollapsed(section) ? this.NAV.showSection : this.NAV.hideSection;
    return `${action}: ${sectionLabel}`;
  }

  logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }

  private loadCollapsedNavSections(): Record<NavSection, boolean> {
    const collapsedNavSections = { ...DEFAULT_COLLAPSED_NAV_SECTIONS };

    try {
      const stored = localStorage.getItem(COLLAPSED_NAV_SECTIONS_STORAGE_KEY);
      if (!stored) return collapsedNavSections;

      const parsed = JSON.parse(stored) as Partial<Record<NavSection, unknown>> | null;
      if (parsed === null) return collapsedNavSections;

      for (const section of NAV_SECTIONS) {
        if (typeof parsed[section] === 'boolean') {
          collapsedNavSections[section] = parsed[section];
        }
      }
    } catch {
      try {
        localStorage.removeItem(COLLAPSED_NAV_SECTIONS_STORAGE_KEY);
      } catch {
        // Ignore storage failures; menu defaults are still safe.
      }
    }

    return collapsedNavSections;
  }

  private saveCollapsedNavSections(): void {
    try {
      localStorage.setItem(COLLAPSED_NAV_SECTIONS_STORAGE_KEY, JSON.stringify(this.collapsedNavSections));
    } catch {
      // Ignore storage failures; the current view still updates.
    }
  }
}
