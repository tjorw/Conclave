import { Component, computed, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HelpTopic } from '../../routing/help-routing';
import { HelpService } from '../../services/help.service';
import { HELP_PANEL_LABELS, HelpPanelKey } from '../../labels/help.labels';

@Component({
  selector: 'app-help-panel',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  template: `
    @if (!dismissed()) {
      <section class="help-inline" [class.help-inline-expanded]="expanded()">
        <div class="help-inline-head">
          <h3 class="help-inline-title">
            <mat-icon>help_outline</mat-icon>
            {{ label().title }}
          </h3>

          <div class="help-inline-actions">
            <button mat-button type="button" (click)="toggleExpanded($event)">
              {{ expanded() ? 'Dölj tips' : 'Visa tips' }}
            </button>
            <button mat-button type="button" (click)="openHelp($event)">
              Läs mer
              <mat-icon>open_in_new</mat-icon>
            </button>
            <button
              mat-icon-button
              type="button"
              aria-label="Dölj hjälprad"
              (click)="dismiss($event)">
              <mat-icon>close</mat-icon>
            </button>
          </div>
        </div>

        @if (expanded()) {
          <p>{{ label().body }}</p>
        }
      </section>
    } @else {
      <button mat-stroked-button type="button" class="help-restore" (click)="restore($event)">
        <mat-icon>help_outline</mat-icon>
        Visa hjälp
      </button>
    }
  `,
  styles: [`
    :host {
      display: block;
      margin: 0 0 12px;
    }

    .help-inline {
      padding: 8px 10px;
      border: 1px solid var(--c-border-medium);
      border-radius: 8px;
      background: #fff;
    }

    .help-inline-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      min-height: 32px;
    }

    .help-inline-title {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      margin: 0;
      color: var(--c-text-primary);
      font-size: 0.9rem;
      font-weight: 600;
      line-height: 1.3;
    }

    .help-inline-title mat-icon {
      width: 18px;
      height: 18px;
      color: var(--c-accent);
      font-size: 18px;
    }

    .help-inline-actions {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .help-inline-actions button {
      min-height: 28px;
      padding: 0 8px;
      font-size: 0.8rem;
    }

    .help-inline-actions button mat-icon {
      width: 16px;
      height: 16px;
      margin-left: 2px;
      font-size: 16px;
    }

    .help-inline p {
      margin: 8px 0 0;
      color: var(--c-text-secondary);
      font-size: 0.82rem;
      line-height: 1.45;
    }

    .help-restore {
      min-height: 30px;
      border-style: dashed;
      color: var(--c-text-secondary);
      font-size: 0.8rem;
    }

    .help-restore mat-icon {
      width: 16px;
      height: 16px;
      margin-right: 4px;
      font-size: 16px;
    }

    @media (max-width: 720px) {
      .help-inline-head {
        align-items: flex-start;
        flex-direction: column;
      }

      .help-inline-actions {
        width: 100%;
      }
    }
  `],
})
export class HelpPanelComponent {
  readonly panelKey = input.required<HelpPanelKey>();
  readonly topic = input.required<HelpTopic>();
  readonly label = computed(() => HELP_PANEL_LABELS[this.panelKey()]);
  readonly expanded = signal(false);
  readonly dismissed = signal(false);

  constructor(private readonly help: HelpService) {}

  ngOnInit(): void {
    this.expanded.set(this.loadExpanded());
    this.dismissed.set(this.loadDismissed());
  }

  toggleExpanded(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    this.setExpanded(!this.expanded());
  }

  setExpanded(expanded: boolean): void {
    this.expanded.set(expanded);
    this.saveExpanded(expanded);
  }

  openHelp(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    this.help.open(this.topic());
  }

  dismiss(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    this.dismissed.set(true);
    this.saveDismissed(true);
  }

  restore(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    this.dismissed.set(false);
    this.saveDismissed(false);
  }

  private storageKey(): string {
    return `help-panel:${this.panelKey()}`;
  }

  private loadExpanded(): boolean {
    try {
      return localStorage.getItem(this.storageKey()) === 'true';
    } catch {
      return false;
    }
  }

  private loadDismissed(): boolean {
    try {
      return localStorage.getItem(`${this.storageKey()}:dismissed`) === 'true';
    } catch {
      return false;
    }
  }

  private saveExpanded(expanded: boolean): void {
    try {
      localStorage.setItem(this.storageKey(), String(expanded));
    } catch {
      // Storage can fail in private mode; the visible panel state still updates.
    }
  }

  private saveDismissed(dismissed: boolean): void {
    try {
      localStorage.setItem(`${this.storageKey()}:dismissed`, String(dismissed));
    } catch {
      // Storage can fail in private mode; the visible panel state still updates.
    }
  }
}
