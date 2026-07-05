import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { HistoryArticle } from '@app/models/history/history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { HistoryArticlePageViewModel } from '../models/history-view.model';
import { mapHistoryArticleToViewModel } from '../mappers/history-view.mapper';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

@Injectable()
export class HistoryArticleStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<HistoryArticle>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly article: Signal<HistoryArticlePageViewModel | null> = computed(() => {
    const article: HistoryArticle | undefined = this.screenStateStore.data();
    return article ? mapHistoryArticleToViewModel(article, this.currentLanguageSignal()) : null;
  });

  constructor(
    @Inject(HISTORY_DATA_PORT) private readonly historyApiService: HistoryDataPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  setResolvedArticle(article: HistoryArticle | null): void {
    if (article) {
      this.screenStateStore.setReady(article);
      return;
    }

    this.screenStateStore.setError('history.article.errorMessage', this.screenStateStore.data());
  }

  loadArticle(eventId: string): void {
    const previousData: HistoryArticle | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.historyApiService.getArticle(eventId, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (article: HistoryArticle) => {
        this.screenStateStore.setReady(article);
      },
      error: (error: unknown) => {
        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('history.article.errorMessage', previousData);
      }
    });
  }
}
