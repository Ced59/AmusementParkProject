import { Injectable, Signal, computed, signal } from '@angular/core';

import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ScreenStateKind } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapArray, mapNullable, mapParkToCardModel } from '@shared/utils/mapping';
import { mapParkContentSummaryViewModel } from '../mappers/park-content-summary.mapper';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';

interface ParkDetailSourceData {
  park: Park;
  explorer: ParkExplorer | null;
  nearbyParks: Park[];
  nearbyState: ScreenStateKind;
}

@Injectable()
export class ParkDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkDetailSourceData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly park: Signal<ParkDetailViewModel | null> = computed(() => {
    return mapNullable(this.screenStateStore.data()?.park, (park: Park) => mapParkToDetailViewModel(park, this.currentLanguageSignal()));
  });
  public readonly summary: Signal<ParkContentSummaryViewModel | null> = computed(() => {
    return mapParkContentSummaryViewModel(this.park(), this.screenStateStore.data()?.explorer ?? null);
  });
  public readonly nearbyParks: Signal<ParkCardModel[]> = computed(() => {
    return mapArray(this.screenStateStore.data()?.nearbyParks, (park: Park) => mapParkToCardModel(park, this.currentLanguageSignal()));
  });
  public readonly nearbyState = computed(() => this.screenStateStore.data()?.nearbyState ?? 'empty');

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly parkItemsApiService: ParkItemsApiService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadPark(id: string): void {
    const previousData: ParkDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parksApiService.getParkById(id).subscribe({
      next: (park: Park) => {
        const sourceData: ParkDetailSourceData = {
          park,
          explorer: null,
          nearbyParks: [],
          nearbyState: 'empty'
        };

        this.screenStateStore.setReady(sourceData);
        this.loadNearbyParks(park);
        this.loadExplorerSummary(park.id ?? id);
      },
      error: (error: unknown) => {
        console.error('Error loading park details', error);
        this.screenStateStore.setError('parks.detail.errorMessage', previousData);
      }
    });
  }

  private loadExplorerSummary(parkId: string): void {
    this.parksApiService.getParkExplorer(parkId).subscribe({
      next: (explorer: ParkExplorer) => {
        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          explorer
        }));
      },
      error: (error: unknown) => {
        console.error('Error loading park explorer summary', error);
        this.loadFallbackExplorerSummary(parkId);
      }
    });
  }

  private loadFallbackExplorerSummary(parkId: string): void {
    this.parkItemsApiService.getParkItemsByParkId(parkId).subscribe({
      next: (items: ParkItem[]) => {
        const countsByCategory: ParkExplorerCount[] = this.buildCounts(items.map((item: ParkItem) => item.category));
        const countsByType: ParkExplorerCount[] = this.buildCounts(items.map((item: ParkItem) => item.type));

        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          explorer: {
            parkId,
            hasZones: false,
            overview: {
              name: 'overview',
              isVirtual: true,
              totalItems: items.length,
              countsByCategory,
              countsByType
            },
            zones: [],
            unassigned: null
          }
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          explorer: null
        }));
      }
    });
  }

  private buildCounts(values: string[]): ParkExplorerCount[] {
    const counts: Map<string, number> = new Map<string, number>();

    values.filter((value: string) => !!value).forEach((value: string) => {
      counts.set(value, (counts.get(value) ?? 0) + 1);
    });

    return Array.from(counts.entries())
      .map(([key, count]: [string, number]) => ({ key, count }))
      .sort((left: ParkExplorerCount, right: ParkExplorerCount) => right.count - left.count || left.key.localeCompare(right.key));
  }

  private loadNearbyParks(park: Park): void {
    if (!this.hasLocationInfo(park)) {
      this.updateReadyData((current: ParkDetailSourceData) => ({
        ...current,
        nearbyParks: [],
        nearbyState: 'empty'
      }));
      return;
    }

    this.updateReadyData((current: ParkDetailSourceData) => ({
      ...current,
      nearbyState: 'loading'
    }));

    this.parksApiService.getParksByLocation(park.latitude, park.longitude, 150).subscribe({
      next: (parks: Park[]) => {
        const nearbyParks: Park[] = parks.filter((candidate: Park) => candidate.id !== park.id).slice(0, 4);
        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          nearbyParks,
          nearbyState: nearbyParks.length > 0 ? 'ready' : 'empty'
        }));
      },
      error: (error: unknown) => {
        const status: number = typeof error === 'object' && error !== null && 'status' in error
          ? Number((error as { status?: number }).status)
          : 0;

        if (status === 404) {
          this.updateReadyData((current: ParkDetailSourceData) => ({
            ...current,
            nearbyParks: [],
            nearbyState: 'empty'
          }));
          return;
        }

        console.error('Error loading nearby parks', error);
        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          nearbyState: 'error'
        }));
      }
    });
  }

  private hasLocationInfo(park: Park | null): boolean {
    return !!park && Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  }

  private updateReadyData(updater: (current: ParkDetailSourceData) => ParkDetailSourceData): void {
    const currentData: ParkDetailSourceData | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady(updater(currentData));
  }
}
