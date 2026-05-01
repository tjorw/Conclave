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

  readonly isOpen = this.isOpenState.asReadonly();
  readonly currentTopic = this.currentTopicState.asReadonly();
  readonly currentContent = computed<HelpTopicContent>(() => HELP_TOPICS[this.currentTopicState()]);
  readonly canGoBack = computed(() => this.historyState().length > 0);

  constructor(private readonly router: Router) {}

  open(topic?: HelpTopic): void {
    this.currentTopicState.set(topic ?? topicForRoute(this.router.url));
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
    this.currentTopicState.set(topic);
    this.isOpenState.set(true);
  }

  back(): void {
    const history = this.historyState();
    const previous = history.at(-1);
    if (!previous) return;

    this.historyState.set(history.slice(0, -1));
    this.currentTopicState.set(previous);
  }
}
