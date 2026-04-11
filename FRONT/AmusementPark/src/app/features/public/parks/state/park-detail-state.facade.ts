import { Injectable, Signal, computed } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ScreenStateKind } from '@shared/models/contracts/screen-state.model';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';

interface ParkDetailViewModel {
  park: Park;
  explorer: ParkExplorer | null;
  nearbyParks: Park[];
  nearbyState: ScreenStateKind;
}

@Injectable()
export class ParkDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkDetailViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly park: Signal<Park | null> = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly explorer: Signal<ParkExplorer | null> = computed(() => this.screenStateStore.data()?.explorer ?? null);
  public readonly nearbyParks: Signal<Park[]> = computed(() => this.screenStateStore.data()?.nearbyParks ?? []);
  public readonly nearbyState = computed(() => this.screenStateStore.data()?.nearbyState ?? 'empty');

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly parkItemsApiService: ParkItemsApiService
  ) {
  }

  loadPark(id: string): void {
    const previousData: ParkDetailViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parksApiService.getParkById(id).subscribe({
      next: (park: Park) => {
        const viewModel: ParkDetailViewModel = {
          park,
          explorer: null,
          nearbyParks: [],
          nearbyState: 'empty'
        };

        this.screenStateStore.setReady(viewModel);
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
        this.updateReadyData((current: ParkDetailViewModel) => ({
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

        this.updateReadyData((current: ParkDetailViewModel) => ({
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
        this.updateReadyData((current: ParkDetailViewModel) => ({
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
      this.updateReadyData((current: ParkDetailViewModel) => ({
        ...current,
        nearbyParks: [],
        nearbyState: 'empty'
      }));
      return;
    }

    this.updateReadyData((current: ParkDetailViewModel) => ({
      ...current,
      nearbyState: 'loading'
    }));

    this.parksApiService.getParksByLocation(park.latitude, park.longitude, 150).subscribe({
      next: (parks: Park[]) => {
        const nearbyParks: Park[] = parks.filter((candidate: Park) => candidate.id !== park.id).slice(0, 4);
        this.updateReadyData((current: ParkDetailViewModel) => ({
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
          this.updateReadyData((current: ParkDetailViewModel) => ({
            ...current,
            nearbyParks: [],
            nearbyState: 'empty'
          }));
          return;
        }

        console.error('Error loading nearby parks', error);
        this.updateReadyData((current: ParkDetailViewModel) => ({
          ...current,
          nearbyState: 'error'
        }));
      }
    });
  }

  private hasLocationInfo(park: Park | null): boolean {
    return !!park && Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  }

  private updateReadyData(updater: (current: ParkDetailViewModel) => ParkDetailViewModel): void {
    const currentData: ParkDetailViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady(updater(currentData));
  }
}
