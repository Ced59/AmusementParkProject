import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkFounder } from '@app/models/parks/park-founder';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerBucket, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkZone } from '@app/models/parks/park-zone';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { ScreenStateKind } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapArray, mapNullable, mapParkToCardModel } from '@shared/utils/mapping';
import { mapParkContentSummaryViewModel } from '../mappers/park-content-summary.mapper';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { mapParkZoneToDetailViewModel } from '../mappers/park-zone-detail-view.mapper';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { ParkZoneDetailViewModel } from '../models/park-zone-detail-view.model';

interface ParkDetailSourceData {
  park: Park;
  explorer: ParkExplorer | null;
  zones: ParkZone[];
  founderName: string | null;
  operatorName: string | null;
  nearbyParks: Park[];
  nearbyState: ScreenStateKind;
}

@Injectable()
export class ParkDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkDetailSourceData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly park: Signal<ParkDetailViewModel | null> = computed(() => {
    const sourceData: ParkDetailSourceData | undefined = this.screenStateStore.data();

    return mapNullable(sourceData?.park, (park: Park) => mapParkToDetailViewModel(
      park,
      this.currentLanguageSignal(),
      {
        founderName: sourceData?.founderName ?? null,
        operatorName: sourceData?.operatorName ?? null
      },
      {
        totalItems: sourceData?.explorer?.overview.totalItems ?? null,
        zoneCount: this.resolveZoneCount(sourceData)
      }
    ));
  });
  public readonly summary: Signal<ParkContentSummaryViewModel | null> = computed(() => {
    return mapParkContentSummaryViewModel(this.park(), this.screenStateStore.data()?.explorer ?? null);
  });
  public readonly zones: Signal<ParkZoneDetailViewModel[]> = computed(() => {
    const sourceData: ParkDetailSourceData | undefined = this.screenStateStore.data();
    const currentPark: ParkDetailViewModel | null = this.park();

    if (!sourceData || !currentPark?.exploreLink) {
      return [];
    }

    const bucketsById: Record<string, ParkExplorerBucket> = (sourceData.explorer?.zones ?? [])
      .filter((bucket: ParkExplorerBucket) => !!bucket.id)
      .reduce((accumulator: Record<string, ParkExplorerBucket>, bucket: ParkExplorerBucket) => {
        accumulator[bucket.id!] = bucket;
        return accumulator;
      }, {} as Record<string, ParkExplorerBucket>);

    return sourceData.zones
      .filter((zone: ParkZone) => zone.isVisible !== false)
      .sort((left: ParkZone, right: ParkZone) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0) || (left.name ?? '').localeCompare(right.name ?? ''))
      .map((zone: ParkZone) => mapParkZoneToDetailViewModel(
        zone,
        zone.id ? bucketsById[zone.id] ?? null : null,
        currentPark.exploreLink,
        this.currentLanguageSignal()
      ));
  });
  public readonly nearbyParks: Signal<ParkCardModel[]> = computed(() => {
    return mapArray(this.screenStateStore.data()?.nearbyParks, (park: Park) => mapParkToCardModel(park, this.currentLanguageSignal()));
  });
  public readonly nearbyState = computed(() => this.screenStateStore.data()?.nearbyState ?? 'empty');

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly parkOperatorsApiService: ParkOperatorsApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadPark(id: string): void {
    const previousData: ParkDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parksApiService.getParkById(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (park: Park) => {
        const sourceData: ParkDetailSourceData = {
          park,
          explorer: null,
          zones: [],
          founderName: null,
          operatorName: null,
          nearbyParks: [],
          nearbyState: 'empty'
        };

        this.screenStateStore.setReady(sourceData);
        this.loadNearbyParks(park);
        this.loadExplorerSummary(park.id ?? id);
        this.loadZones(park.id ?? id);
        this.loadReferences(park);
      },
      error: (error: unknown) => {
        console.error('Error loading park details', error);
        this.screenStateStore.setError('parks.detail.errorMessage', previousData);
      }
    });
  }

  private loadExplorerSummary(parkId: string): void {
    this.parksApiService.getParkExplorer(parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  private loadZones(parkId: string): void {
    this.parkZonesApiService.getParkZonesByParkId(parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (zones: ParkZone[]) => {
        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          zones
        }));
      },
      error: (error: unknown) => {
        console.error('Error loading park zones', error);
        this.updateReadyData((current: ParkDetailSourceData) => ({
          ...current,
          zones: []
        }));
      }
    });
  }

  private loadReferences(park: Park): void {
    const founderId: string | null = park.founderId?.trim() ?? null;
    const operatorId: string | null = park.operatorId?.trim() ?? null;

    if (founderId) {
      this.parkFoundersApiService.getParkFounderById(founderId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (founder: ParkFounder) => {
          this.updateReadyData((current: ParkDetailSourceData) => ({
            ...current,
            founderName: founder.name?.trim() || founderId
          }));
        },
        error: () => {
          this.updateReadyData((current: ParkDetailSourceData) => ({
            ...current,
            founderName: founderId
          }));
        }
      });
    }

    if (operatorId) {
      this.parkOperatorsApiService.getParkOperatorById(operatorId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (operator: ParkOperator) => {
          this.updateReadyData((current: ParkDetailSourceData) => ({
            ...current,
            operatorName: operator.name?.trim() || operatorId
          }));
        },
        error: () => {
          this.updateReadyData((current: ParkDetailSourceData) => ({
            ...current,
            operatorName: operatorId
          }));
        }
      });
    }
  }

  private loadFallbackExplorerSummary(parkId: string): void {
    this.parkItemsApiService.getParkItemsByParkId(parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

    this.parksApiService.getParksByLocation(park.latitude, park.longitude, 150).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  private resolveZoneCount(sourceData: ParkDetailSourceData | undefined): number | null {
    if (!sourceData) {
      return null;
    }

    const visibleZonesCount: number = sourceData.zones.filter((zone: ParkZone) => zone.isVisible !== false).length;

    if (visibleZonesCount > 0) {
      return visibleZonesCount;
    }

    const explorerZoneCount: number = sourceData.explorer?.zones.length ?? 0;
    return explorerZoneCount > 0 ? explorerZoneCount : null;
  }

  private updateReadyData(updater: (current: ParkDetailSourceData) => ParkDetailSourceData): void {
    const currentData: ParkDetailSourceData | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady(updater(currentData));
  }
}
