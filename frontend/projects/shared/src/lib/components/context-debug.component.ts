import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { ENVIRONMENT } from '../environment/environment.token';
import { AuthService } from '../services/auth.service';
import { ConventionContextService } from '../services/convention-context.service';

@Component({
  selector: 'app-context-debug',
  standalone: true,
  template: `
    @if (visible()) {
      <aside class="context-debug" aria-label="Debug context">
        <span title="Tenant">{{ tenantLabel() }}</span>
        <span title="Convention">{{ conventionLabel() }}</span>
      </aside>
    }
  `,
  styles: [`
    .context-debug {
      position: fixed;
      right: 10px;
      bottom: 10px;
      z-index: 1000;
      display: inline-flex;
      gap: 8px;
      max-width: calc(100vw - 20px);
      padding: 5px 8px;
      border: 1px solid rgba(15, 23, 42, 0.14);
      border-radius: 6px;
      background: rgba(255, 255, 255, 0.88);
      box-shadow: 0 2px 8px rgba(15, 23, 42, 0.12);
      color: rgba(15, 23, 42, 0.72);
      font: 11px/1.3 ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
      pointer-events: none;
    }

    .context-debug span {
      min-width: 0;
      max-width: 220px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    @media (max-width: 640px) {
      .context-debug {
        left: 10px;
        right: 10px;
        justify-content: center;
      }

      .context-debug span {
        max-width: 45vw;
      }
    }
  `],
})
export class ContextDebugComponent implements OnDestroy {
  private readonly env = inject(ENVIRONMENT);
  private readonly auth = inject(AuthService);
  private readonly conventionContext = inject(ConventionContextService);
  private readonly router = inject(Router);
  private readonly routeUrl = signal(this.router.url);
  private readonly routerSubscription: Subscription;

  readonly visible = computed(() => !this.env.production);

  readonly tenantLabel = computed(() => {
    const configuredTenant = this.env.multitenancy?.enabled
      ? this.env.multitenancy.devTenantId
      : null;
    const tenantId = configuredTenant
      ?? this.auth.claims()?.tenant_id
      ?? this.findRouteValue('tenants')
      ?? (this.env.multitenancy?.enabled ? null : 'singletenant');

    return `T:${this.shortValue(tenantId)}`;
  });

  readonly conventionLabel = computed(() => {
    const convention = this.conventionContext.convention();
    const conventionId = convention?.id
      ?? this.findRouteValue('conventions')
      ?? this.env.conventionId
      ?? null;
    const conventionName = convention?.slug || convention?.name;

    return `K:${conventionName ?? this.shortValue(conventionId)}`;
  });

  constructor() {
    this.routerSubscription = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => this.routeUrl.set(event.urlAfterRedirects));
  }

  ngOnDestroy(): void {
    this.routerSubscription.unsubscribe();
  }

  private findRouteValue(segmentName: string): string | null {
    const parts = this.routeUrl().split(/[/?#]/)[0].split('/').filter(Boolean);
    const index = parts.indexOf(segmentName);
    return index >= 0 ? parts[index + 1] ?? null : null;
  }

  private shortValue(value: string | null | undefined): string {
    if (!value) return '-';
    if (value === 'singletenant') return value;
    return value.length > 12 ? `${value.slice(0, 8)}...${value.slice(-4)}` : value;
  }
}
