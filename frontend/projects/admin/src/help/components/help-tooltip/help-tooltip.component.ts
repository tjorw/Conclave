import { Component, computed, HostListener, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltip, MatTooltipModule } from '@angular/material/tooltip';
import { HELP_TOOLTIP_LABELS, HelpTooltipKey } from '../../labels/help.labels';

@Component({
  selector: 'app-help-tooltip',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatTooltipModule],
  template: `
    <button
      mat-icon-button
      type="button"
      class="help-tooltip-trigger"
      [attr.aria-label]="ariaLabel()"
      [matTooltip]="label()"
      matTooltipPosition="above"
      matTooltipShowDelay="150"
      matTooltipHideDelay="100"
      #tooltip="matTooltip"
      (click)="toggleTooltip(tooltip, $event)">
      <mat-icon>info</mat-icon>
    </button>
  `,
  styles: [`
    :host {
      display: inline-flex;
      flex: 0 0 auto;
      vertical-align: middle;
    }

    .help-tooltip-trigger {
      width: 28px;
      height: 28px;
      padding: 0;
      color: var(--c-text-light);
      line-height: 1;
    }

    .help-tooltip-trigger mat-icon {
      width: 18px;
      height: 18px;
      font-size: 18px;
    }
  `],
})
export class HelpTooltipComponent {
  readonly helpKey = input.required<HelpTooltipKey>();
  readonly label = computed(() => HELP_TOOLTIP_LABELS[this.helpKey()]);
  readonly ariaLabel = computed(() => `Hjälp: ${this.label()}`);

  private openTooltip: MatTooltip | null = null;

  toggleTooltip(tooltip: MatTooltip, event: MouseEvent): void {
    event.stopPropagation();

    if (this.openTooltip === tooltip) {
      tooltip.hide(0);
      this.openTooltip = null;
      return;
    }

    this.openTooltip?.hide(0);
    tooltip.show(0);
    this.openTooltip = tooltip;
  }

  @HostListener('document:click')
  closeTooltip(): void {
    this.openTooltip?.hide(0);
    this.openTooltip = null;
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.closeTooltip();
  }
}
