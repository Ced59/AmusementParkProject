import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { PassportVisit, PassportVisitPage } from '@app/models/passport/passport-visit.models';
import { TranslationService } from '@app/services/translation.service';
import { mapPassportVisitOverviewItem } from '../mappers/passport-visits-overview.mapper';
import { PassportVisitOverviewItemViewModel } from '../models/passport-visits-overview.models';
import {
  PASSPORT_VISITS_OVERVIEW_API_PORT,
  PassportVisitsOverviewApiPort
} from './passport-visits-overview-state-data.ports';

const PASSPORT_VISITS_OVERVIEW_PAGE_SIZE = 20;

@Injectable()
export class PassportVisitsOverviewStateFacade {
  private readonly visitsSignal = signal<PassportVisit[]>([]);
  private readonly languageSignal = signal<string>('en');
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly loadMoreErrorKeySignal = signal<string | null>(null);
  private readonly nextCursorSignal = signal<string | null>(null);

  public readonly visits: Signal<PassportVisitOverviewItemViewModel[]> = computed(() =>
    this.visitsSignal().map((visit: PassportVisit) =>
      mapPassportVisitOverviewItem(visit, this.languageSignal()))
  );
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly loadMoreErrorKey: Signal<string | null> = this.loadMoreErrorKeySignal.asReadonly();
  public readonly hasMore: Signal<boolean> = computed(() => this.nextCursorSignal() !== null);
  public readonly isEmpty: Signal<boolean> = computed(() =>
    !this.loadingSignal() && this.errorKeySignal() === null && this.visitsSignal().length === 0
  );

  constructor(
    @Inject(PASSPORT_VISITS_OVERVIEW_API_PORT)
    private readonly visitsApi: PassportVisitsOverviewApiPort,
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
    this.languageSignal.set(this.translationService.getCurrentLang() || 'en');
    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => this.languageSignal.set(language || 'en'));
  }

  public load(): void {
    if (this.loadingSignal()) {
      return;
    }

    this.loadingSignal.set(true);
    this.loadingMoreSignal.set(false);
    this.errorKeySignal.set(null);
    this.loadMoreErrorKeySignal.set(null);

    this.visitsApi.listVisits(PASSPORT_VISITS_OVERVIEW_PAGE_SIZE, null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page: PassportVisitPage): void => {
          this.visitsSignal.set(deduplicateVisits(page.items));
          this.nextCursorSignal.set(normalizeCursor(page.nextCursor));
          this.loadingSignal.set(false);
        },
        error: (error: unknown): void => {
          console.error('Error loading passport visits', error);
          this.visitsSignal.set([]);
          this.nextCursorSignal.set(null);
          this.errorKeySignal.set('passport.overview.errors.load');
          this.loadingSignal.set(false);
        }
      });
  }

  public loadMore(): void {
    const cursor: string | null = this.nextCursorSignal();
    if (!cursor || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.loadingMoreSignal.set(true);
    this.loadMoreErrorKeySignal.set(null);
    this.visitsApi.listVisits(PASSPORT_VISITS_OVERVIEW_PAGE_SIZE, cursor)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (page: PassportVisitPage): void => {
          this.visitsSignal.set(deduplicateVisits([
            ...this.visitsSignal(),
            ...page.items
          ]));
          this.nextCursorSignal.set(normalizeCursor(page.nextCursor));
          this.loadingMoreSignal.set(false);
        },
        error: (error: unknown): void => {
          console.error('Error loading more passport visits', error);
          this.loadMoreErrorKeySignal.set('passport.overview.errors.loadMore');
          this.loadingMoreSignal.set(false);
        }
      });
  }
}

function normalizeCursor(cursor: string | null | undefined): string | null {
  return cursor?.trim() || null;
}

function deduplicateVisits(visits: readonly PassportVisit[]): PassportVisit[] {
  const visitsById = new Map<string, PassportVisit>();
  for (const visit of visits) {
    if (!visitsById.has(visit.id)) {
      visitsById.set(visit.id, visit);
    }
  }

  return Array.from(visitsById.values());
}
