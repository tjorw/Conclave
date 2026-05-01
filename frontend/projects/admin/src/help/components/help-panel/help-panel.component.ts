import { Component, computed, input, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { HelpTopic } from '../../routing/help-routing';
import { HelpService } from '../../services/help.service';
import { HELP_PANEL_LABELS, HelpPanelKey } from '../../labels/help.labels';

@Component({
  selector: 'app-help-panel',
  standalone: true,
  imports: [MatButtonModule, MatExpansionModule, MatIconModule],
  template: `
    <mat-expansion-panel
      class="help-panel"
      [expanded]="expanded()"
      (opened)="setExpanded(true)"
      (closed)="setExpanded(false)">
      <mat-expansion-panel-header>
        <mat-panel-title>
          <mat-icon>help_outline</mat-icon>
          {{ label().title }}
        </mat-panel-title>
      </mat-expansion-panel-header>

      <p>{{ label().body }}</p>
      <button mat-button type="button" (click)="openHelp()">
        Läs mer
        <mat-icon>open_in_new</mat-icon>
      </button>
    </mat-expansion-panel>
  `,
  styles: [`
    :host {
      display: block;
      margin: 0 0 16px;
    }

    .help-panel {
      border: 1px solid var(--c-border-medium);
      border-radius: 8px;
      background: #fff;
      box-shadow: none;
    }

    mat-panel-title {
      display: flex;
      align-items: center;
      gap: 8px;
      color: var(--c-text-primary);
      font-weight: 500;
    }

    mat-panel-title mat-icon {
      color: var(--c-accent);
      flex: 0 0 auto;
    }

    p {
      margin: 0 0 10px;
      color: var(--c-text-secondary);
      font-size: 0.875rem;
      line-height: 1.55;
    }

    button {
      padding-left: 0;
    }

    button mat-icon {
      width: 18px;
      height: 18px;
      margin-left: 4px;
      font-size: 18px;
    }
  `],
})
export class HelpPanelComponent {
  readonly panelKey = input.required<HelpPanelKey>();
  readonly topic = input.required<HelpTopic>();
  readonly label = computed(() => HELP_PANEL_LABELS[this.panelKey()]);
  readonly expanded = signal(false);

  constructor(private readonly help: HelpService) {}

  ngOnInit(): void {
    this.expanded.set(this.loadExpanded());
  }

  setExpanded(expanded: boolean): void {
    this.expanded.set(expanded);
    this.saveExpanded(expanded);
  }

  openHelp(): void {
    this.help.open(this.topic());
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

  private saveExpanded(expanded: boolean): void {
    try {
      localStorage.setItem(this.storageKey(), String(expanded));
    } catch {
      // Storage can fail in private mode; the visible panel state still updates.
    }
  }
}
