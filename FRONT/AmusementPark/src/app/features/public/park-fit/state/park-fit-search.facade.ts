import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';

import {
  ParkFitSearchPark,
  ParkFitSearchRequest,
  ParkFitSearchResponse
} from '@app/models/park-fit/park-fit-search.models';
import { PARK_FIT_SEARCH_DATA_PORT, ParkFitSearchDataPort } from './park-fit-search-data.ports';

export type ParkFitSearchStatus = 'idle' | 'loading' | 'success' | 'error';

@Injectable({ providedIn: 'root' })
export class ParkFitSearchFacade {
  private readonly statusSignal = signal<ParkFitSearchStatus>('idle');
  private readonly responseSignal = signal<ParkFitSearchResponse | null>(null);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly lastRequestSignal = signal<ParkFitSearchRequest | null>(null);

  public readonly status: Signal<ParkFitSearchStatus> = this.statusSignal.asReadonly();
  public readonly response: Signal<ParkFitSearchResponse | null> = this.responseSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly lastRequest: Signal<ParkFitSearchRequest | null> = this.lastRequestSignal.asReadonly();
  public readonly firstPark: Signal<ParkFitSearchPark | null> = computed(() =>
    this.responseSignal()?.parks.find((park: ParkFitSearchPark): boolean => park.scoreState !== 'Excluded') ?? null
  );

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
    members: request.members.map((member) => ({ ...member })),
    preferredAttractionTypes: [...request.preferredAttractionTypes]
  };
}
