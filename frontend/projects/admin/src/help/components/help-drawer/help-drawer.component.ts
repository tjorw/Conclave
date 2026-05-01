import { Component, ElementRef, HostListener, ViewChild, effect, inject } from '@angular/core';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MarkdownComponent } from 'ngx-markdown';
import { HelpService } from '../../services/help.service';
import { HELP_TOPIC_ORDER, HELP_TOPICS, HelpTopic } from '../../routing/help-routing';

@Component({
  selector: 'app-help-drawer',
  standalone: true,
  imports: [CdkTrapFocus, MatButtonModule, MatIconModule, MarkdownComponent],
  template: `
    @if (help.isOpen()) {
      <div class="help-backdrop" (click)="help.close()"></div>
      <aside
        class="help-drawer"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="help.currentContent().title"
        cdkTrapFocus
        [cdkTrapFocusAutoCapture]="true">
        <header class="help-drawer-header">
          <button
            mat-icon-button
            type="button"
            [disabled]="!help.canGoBack()"
            aria-label="Gå tillbaka i hjälpen"
            (click)="goBack()">
            <mat-icon>arrow_back</mat-icon>
          </button>
          <h2>{{ help.currentContent().title }}</h2>
          <button mat-icon-button type="button" aria-label="Stäng hjälp" (click)="help.close()">
            <mat-icon>close</mat-icon>
          </button>
        </header>

        <nav class="help-topic-nav" aria-label="Hjälpämnen">
          @for (topic of topics; track topic) {
            <button
              type="button"
              [class.active-topic]="topic === help.currentTopic()"
              (click)="openTopic(topic)">
              {{ topicTitle(topic) }}
            </button>
          }
        </nav>

        <div class="help-drawer-content" #contentRoot (click)="handleContentClick($event)">
          <markdown [data]="help.currentContent().markdown" />
        </div>
      </aside>
    }
  `,
  styles: [`
    .help-backdrop {
      position: fixed;
      inset: 0;
      z-index: 20;
      background: rgba(0, 0, 0, 0.28);
    }

    .help-drawer {
      position: fixed;
      top: 0;
      right: 0;
      bottom: 0;
      z-index: 21;
      display: flex;
      flex-direction: column;
      width: min(420px, 100vw);
      background: #fff;
      box-shadow: -8px 0 24px rgba(0, 0, 0, 0.18);
    }

    .help-drawer-header {
      display: grid;
      grid-template-columns: 40px 1fr 40px;
      align-items: center;
      gap: 8px;
      padding: 12px;
      border-bottom: 1px solid var(--c-border-medium);
    }

    .help-drawer-header h2 {
      margin: 0;
      color: var(--c-text-primary);
      font-size: 1.05rem;
      font-weight: 600;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .help-topic-nav {
      display: flex;
      gap: 6px;
      overflow-x: auto;
      padding: 10px 12px;
      border-bottom: 1px solid var(--c-border-medium);
    }

    .help-topic-nav button {
      flex: 0 0 auto;
      border: 1px solid var(--c-border-medium);
      border-radius: 8px;
      background: #fff;
      color: var(--c-text-secondary);
      cursor: pointer;
      font: inherit;
      font-size: 0.78rem;
      padding: 6px 10px;
      white-space: nowrap;
    }

    .help-topic-nav button:hover,
    .help-topic-nav button:focus-visible,
    .help-topic-nav .active-topic {
      border-color: var(--c-accent-border);
      background: var(--c-accent-bg);
      color: var(--c-accent);
    }

    .help-drawer-content {
      flex: 1;
      overflow: auto;
      padding: 18px 22px 28px;
      color: var(--c-text-primary);
      font-size: 0.92rem;
      line-height: 1.65;
    }

    .help-drawer-content :where(h2, h3) {
      margin: 0 0 10px;
      line-height: 1.25;
    }

    .help-drawer-content :where(p, ul, ol) {
      margin: 0 0 14px;
    }
  `],
})
export class HelpDrawerComponent {
  readonly help = inject(HelpService);
  readonly topics = HELP_TOPIC_ORDER;

  @ViewChild('contentRoot') private contentRoot?: ElementRef<HTMLElement>;

  private readonly scrollToTop = effect(() => {
    this.help.currentTopic();
    queueMicrotask(() => this.contentRoot?.nativeElement.scrollTo({ top: 0 }));
  });

  topicTitle(topic: HelpTopic): string {
    return HELP_TOPICS[topic].title;
  }

  openTopic(topic: HelpTopic): void {
    this.help.goTo(topic);
  }

  goBack(): void {
    this.help.back();
  }

  handleContentClick(event: MouseEvent): void {
    const anchor = (event.target as HTMLElement).closest('a');
    const href = anchor?.getAttribute('href');
    if (!href?.startsWith('help:')) return;

    const topic = href.substring('help:'.length) as HelpTopic;
    if (topic in HELP_TOPICS) {
      event.preventDefault();
      this.help.goTo(topic);
    }
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    if (this.help.isOpen()) {
      this.help.close();
    }
  }
}
