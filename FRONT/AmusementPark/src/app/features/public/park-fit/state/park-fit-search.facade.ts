import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

import {
  ParkFitSearchPark,
  ParkFitSearchRequest,
  ParkFitSearchResponse
} from '@app/models/park-fit/park-fit-search.models';
import { ParkFitComparisonSelection } from '../models/park-fit-comparison.models';
import { PARK_FIT_SEARCH_DATA_PORT, ParkFitSearchDataPort } from './park-fit-search-data.ports';

export type ParkFitSearchStatus = 'idle' | 'loading' | 'success' | 'error';

@Injectable({ providedIn: 'root' })
export class ParkFitSearchFacade {
  private readonly statusSignal = signal<ParkFitSearchStatus>('idle');
  private readonly responseSignal = signal<ParkFitSearchResponse | null>(null);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly lastRequestSignal = signal<ParkFitSearchRequest | null>(null);
  private readonly comparisonParkIdsSignal = signal<string[]>([]);

  public readonly status: Signal<ParkFitSearchStatus> = this.statusSignal.asReadonly();
  public readonly response: Signal<ParkFitSearchResponse | null> = this.responseSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly lastRequest: Signal<ParkFitSearchRequest | null> = this.lastRequestSignal.asReadonly();
  public readonly comparisonParkIds: Signal<string[]> = this.comparisonParkIdsSignal.asReadonly();
  public readonly visibleParks: Signal<ParkFitSearchPark[]> = computed(() =>
    this.responseSignal()?.parks.filter(
      (park: ParkFitSearchPark): boolean => park.scoreState !== 'Excluded'
    ) ?? []
  );
  public readonly firstPark: Signal<ParkFitSearchPark | null> = computed(() =>
    this.visibleParks()[0] ?? null
  );
  public readonly comparisonSelections: Signal<ParkFitComparisonSelection[]> = computed(() => {
    const selectedIds: ReadonlySet<string> = new Set(this.comparisonParkIdsSignal());
    return this.visibleParks()
      .map((park: ParkFitSearchPark, index: number): ParkFitComparisonSelection => ({
        park,
        resultRank: index + 1
      }))
      .filter((selection: ParkFitComparisonSelection): boolean => selectedIds.has(selection.park.parkId));
  });
  public readonly comparisonParks: Signal<ParkFitSearchPark[]> = computed(() =>
    this.comparisonSelections().map((selection: ParkFitComparisonSelection): ParkFitSearchPark => selection.park)
  );
  public readonly canCompare: Signal<boolean> = computed(() => {
    const count: number = this.comparisonParks().length;
    return count >= 2 && count <= 4;
  });
  public readonly comparisonLimitReached: Signal<boolean> = computed(() => this.comparisonParks().length >= 4);

  constructor(
    @Inject(PARK_FIT_SEARCH_DATA_PORT) private readonly dataPort: ParkFitSearchDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  search(request: ParkFitSearchRequest): void {
    if (this.statusSignal() === 'loading') {
      return;
    }

    this.lastRequestSignal.set(copyRequest(request));
    this.comparisonParkIdsSignal.set([]);
    this.responseSignal.set(null);
    this.errorKeySignal.set(null);
    this.statusSignal.set('loading');

    this.dataPort.search(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: ParkFitSearchResponse): void => {
          this.responseSignal.set(response);
          this.statusSignal.set('success');
        },
        error: (error: unknown): void => {
          this.responseSignal.set(null);
          this.errorKeySignal.set(resolveErrorKey(error));
          this.statusSignal.set('error');
        }
      });
  }

  toggleComparisonPark(parkId: string): void {
    const selectedIds: string[] = this.comparisonParkIdsSignal();
    if (selectedIds.includes(parkId)) {
      this.comparisonParkIdsSignal.set(selectedIds.filter((selectedId: string): boolean => selectedId !== parkId));
      return;
    }

    const isVisible: boolean = this.visibleParks().some((park: ParkFitSearchPark): boolean => park.parkId === parkId);
    if (!isVisible || selectedIds.length >= 4) {
      return;
    }

    this.comparisonParkIdsSignal.set([...selectedIds, parkId]);
  }
}

function resolveErrorKey(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 429) {
    return 'parkFit.feedback.rateLimited';
  }

  if (error instanceof HttpErrorResponse && error.status === 400) {
    return 'parkFit.feedback.invalid';
  }

  return 'parkFit.feedback.error';
}

function copyRequest(request: ParkFitSearchRequest): ParkFitSearchRequest {
  return {
    ...request,
    originLatitude: null,
    originLongitude: null,
    members: request.members.map((member) => ({ ...member })),
    preferredAttractionTypes: [...request.preferredAttractionTypes]
  };
}
