import { HttpClient } from '@angular/common/http';
import { computed, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  DEFAULT_HELP_TOPIC,
  HELP_TOPICS,
  HelpTopic,
  HelpTopicContent,
  topicForRoute,
} from '../routing/help-routing';

@Injectable({ providedIn: 'root' })
export class HelpService {
  private readonly isOpenState = signal(false);
  private readonly currentTopicState = signal<HelpTopic>(DEFAULT_HELP_TOPIC);
  private readonly historyState = signal<HelpTopic[]>([]);
  private readonly markdownCache = new Map<HelpTopic, string>();
  private readonly markdownState = signal<Partial<Record<HelpTopic, string>>>({});

  readonly isOpen = this.isOpenState.asReadonly();
  readonly currentTopic = this.currentTopicState.asReadonly();
  readonly currentContent = computed<HelpTopicContent & { markdown: string }>(() => {
    const topic = this.currentTopicState();
    const content = HELP_TOPICS[topic];
    return {
      ...content,
      markdown: this.markdownState()[topic] ?? content.fallbackMarkdown,
    };
  });
  readonly canGoBack = computed(() => this.historyState().length > 0);

  constructor(
    private readonly router: Router,
    private readonly http: HttpClient,
  ) {}

  open(topic?: HelpTopic): void {
    this.setTopic(topic ?? topicForRoute(this.router.url));
    this.historyState.set([]);
    this.isOpenState.set(true);
  }

  close(): void {
    this.isOpenState.set(false);
    this.historyState.set([]);
  }

  goTo(topic: HelpTopic): void {
    const current = this.currentTopicState();
    if (topic === current) return;

    this.historyState.update(history => [...history, current]);
    this.setTopic(topic);
    this.isOpenState.set(true);
  }

  back(): void {
    const history = this.historyState();
    const previous = history.at(-1);
    if (!previous) return;

    this.historyState.set(history.slice(0, -1));
    this.setTopic(previous);
  }

  private setTopic(topic: HelpTopic): void {
    this.currentTopicState.set(topic);
    this.loadMarkdown(topic);
  }

  private loadMarkdown(topic: HelpTopic): void {
    const content = HELP_TOPICS[topic];
    if (!content.assetPath || this.markdownCache.has(topic)) return;

    this.http.get(content.assetPath, { responseType: 'text' }).subscribe({
      next: markdown => {
        this.markdownCache.set(topic, markdown);
        this.markdownState.update(current => ({ ...current, [topic]: markdown }));
      },
      error: () => {
        this.markdownCache.set(topic, content.fallbackMarkdown);
        this.markdownState.update(current => ({ ...current, [topic]: content.fallbackMarkdown }));
      },
    });
  }
}
