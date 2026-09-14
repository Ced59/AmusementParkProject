import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

import {
  ParkFitSearchPark,
  ParkFitSearchRequest,
  ParkFitSearchResponse
} from '@app/models/park-fit/park-fit-search.models';
import {
  ParkFitPilotComparisonSize,
  ParkFitPilotDurationBand,
  ParkFitPilotFailureKind,
  ParkFitPilotResultBand,
  ParkFitPilotUnknownLevel
} from '@app/models/park-fit/park-fit-pilot-observation.model';
import { ParkFitComparisonSelection } from '../models/park-fit-comparison.models';
import { PARK_FIT_SEARCH_DATA_PORT, ParkFitSearchDataPort } from './park-fit-search-data.ports';
import {
  PARK_FIT_PILOT_TELEMETRY_PORT,
  ParkFitPilotTelemetryPort
} from './park-fit-pilot-telemetry.port';

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
    @Inject(PARK_FIT_PILOT_TELEMETRY_PORT)
    private readonly telemetryPort: ParkFitPilotTelemetryPort,
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
    const startedAt: number = Date.now();
    this.telemetryPort.track({ eventKind: 'SearchStarted' });

    this.dataPort.search(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: ParkFitSearchResponse): void => {
          this.responseSignal.set(response);
          this.statusSignal.set('success');
          const visibleParks: ParkFitSearchPark[] = response.parks.filter(
            (park: ParkFitSearchPark): boolean => park.scoreState !== 'Excluded'
          );
          this.telemetryPort.track({
            eventKind: 'SearchCompleted',
            resultBand: resultBand(visibleParks.length),
            unknownLevel: unknownLevel(visibleParks),
            durationBand: durationBand(Date.now() - startedAt),
            methodVersion: response.methodVersion,
            qualityIssues: Object.entries(response.qualityIssueCounts)
              .filter((entry: [string, number]): boolean => entry[1] > 0)
              .map((entry: [string, number]): string => entry[0])
          });
        },
        error: (error: unknown): void => {
          this.responseSignal.set(null);
          this.errorKeySignal.set(resolveErrorKey(error));
          this.statusSignal.set('error');
          this.telemetryPort.track({
            eventKind: 'SearchFailed',
            durationBand: durationBand(Date.now() - startedAt),
            failureKind: failureKind(error)
          });
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

  trackExplanationViewed(): void {
    this.telemetryPort.track({ eventKind: 'ExplanationViewed' });
  }

  trackComparisonOpened(): void {
    const count: number = this.comparisonParks().length;
    const comparisonSize: ParkFitPilotComparisonSize | null = count === 2
      ? 'Two'
      : count === 3
        ? 'Three'
        : count === 4
          ? 'Four'
          : null;
    if (comparisonSize) {
      this.telemetryPort.track({ eventKind: 'ComparisonOpened', comparisonSize });
    }
  }
}

function resultBand(count: number): ParkFitPilotResultBand {
  if (count === 0) {
    return 'None';
  }

  if (count === 1) {
    return 'One';
  }

  return count <= 4 ? 'TwoToFour' : 'FiveOrMore';
}

function unknownLevel(parks: readonly ParkFitSearchPark[]): ParkFitPilotUnknownLevel {
  if (parks.length === 0) {
    return 'None';
  }

  const unknownCount: number = parks.reduce(
    (total: number, park: ParkFitSearchPark): number => total + park.unknownCount,
    0
  );
  if (unknownCount === 0) {
    return 'None';
  }

  return parks.some((park: ParkFitSearchPark): boolean => park.coveragePercent < 75)
    || unknownCount >= parks.length * 2
    ? 'Significant'
    : 'Limited';
}

function durationBand(durationMilliseconds: number): ParkFitPilotDurationBand {
  if (durationMilliseconds < 500) {
    return 'UnderHalfSecond';
  }

  if (durationMilliseconds < 1500) {
    return 'UnderOneAndHalfSeconds';
  }

  return durationMilliseconds < 3000 ? 'UnderThreeSeconds' : 'ThreeSecondsOrMore';
}

function failureKind(error: unknown): ParkFitPilotFailureKind {
  if (error instanceof HttpErrorResponse && error.status === 400) {
    return 'Validation';
  }

  return error instanceof HttpErrorResponse && error.status === 429
    ? 'RateLimited'
    : 'Technical';
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
