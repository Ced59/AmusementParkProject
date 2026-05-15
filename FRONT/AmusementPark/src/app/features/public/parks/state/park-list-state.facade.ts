import { Injectable, Signal, computed, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { PaginationContract } from '@shared/models/contracts';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapArray, mapCollectionResponse, mapParkToCardModel } from '@shared/utils/mapping';
import { Park } from '@app/models/parks/park';
import { ParksApiService } from '@data-access/parks/parks-api.service';

interface ParkListSourceData {
  parks: Park[];
  pagination: PaginationContract | null;
}

@Injectable()
export class ParkListStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkListSourceData>();
  private readonly currentLanguageSignal = signal('en');
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(9);

  public readonly state = this.screenStateStore.state;
  public readonly parks: Signal<ParkCardModel[]> = computed(() => {
    return mapArray(this.screenStateStore.data()?.parks, (park: Park) => mapParkToCardModel(park, this.currentLanguageSignal()));
  });
  public readonly pagination: Signal<PaginationContract | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(private readonly parksApiService: ParksApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadParks(page: number, size: number, term: string): void {
    const normalizedTerm: string = term.trim();
    const previousData: ParkListSourceData | undefined = this.screenStateStore.data();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const request$ = normalizedTerm
      ? this.parksApiService.searchParks(normalizedTerm, page, size)
      : this.parksApiService.getParksPaginated(page, size);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: ParksApiResponse) => {
        const pagedResult = mapCollectionResponse(response, (park: Park) => park);
        const sourceData: ParkListSourceData = {
          parks: pagedResult.items,
          pagination: pagedResult.pagination,
        };

        if (pagedResult.items.length === 0) {
          this.screenStateStore.setEmpty(sourceData);
          return;
        }

        this.screenStateStore.setReady(sourceData);
      },
      error: (error: unknown) => {
        console.error('Error fetching parks:', error);
        this.screenStateStore.setError('parks.errorMessage', previousData);
      }
    });
  }
}
