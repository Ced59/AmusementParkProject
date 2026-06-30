import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { HistoryTimeline } from '@app/models/history/history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { HistoryTimelinePageViewModel } from '../models/history-view.model';
import { mapHistoryTimelineToViewModel } from '../mappers/history-view.mapper';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

@Injectable()
export class HistoryTimelineStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<HistoryTimeline>();
  private readonly currentLanguageSignal = signal('en');
  private readonly includeParkItemsSignal = signal(false);

  public readonly state = this.screenStateStore.state;
  public readonly timeline: Signal<HistoryTimelinePageViewModel | null> = computed(() => {
    const timeline: HistoryTimeline | undefined = this.screenStateStore.data();
    return timeline ? mapHistoryTimelineToViewModel(timeline, this.currentLanguageSignal()) : null;
  });
  public readonly includeParkItems: Signal<boolean> = this.includeParkItemsSignal.asReadonly();

  private currentParkId: string | null = null;
  private currentParkItemId: string | null = null;

  constructor(
    @Inject(HISTORY_DATA_PORT) private readonly historyApiService: HistoryDataPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadParkTimeline(parkId: string, includeParkItems: boolean = this.includeParkItemsSignal()): void {
    this.currentParkId = parkId;
    this.currentParkItemId = null;
    this.includeParkItemsSignal.set(includeParkItems);
    const previousData: HistoryTimeline | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.historyApiService.getParkTimeline(parkId, includeParkItems, [], anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (timeline: HistoryTimeline) => {
        this.screenStateStore.setReady(timeline);
      },
      error: (error: unknown) => {
        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('history.timeline.errorMessage', previousData);
      }
    });
  }

  loadParkItemTimeline(parkItemId: string): void {
    this.currentParkItemId = parkItemId;
    this.currentParkId = null;
    const previousData: HistoryTimeline | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.historyApiService.getParkItemTimeline(parkItemId, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (timeline: HistoryTimeline) => {
        this.screenStateStore.setReady(timeline);
      },
      error: (error: unknown) => {
        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('history.timeline.errorMessage', previousData);
      }
    });
  }

  setIncludeParkItems(includeParkItems: boolean): void {
    if (this.includeParkItemsSignal() === includeParkItems) {
      return;
    }

    this.includeParkItemsSignal.set(includeParkItems);

    if (this.currentParkId) {
      this.loadParkTimeline(this.currentParkId, includeParkItems);
      return;
    }

    if (this.currentParkItemId) {
      this.loadParkItemTimeline(this.currentParkItemId);
    }
  }
}
