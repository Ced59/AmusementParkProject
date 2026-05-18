import { Injectable, Signal, computed, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapParkItemToDetailViewModel } from '../mappers/park-item-detail-view.mapper';
import { ParkItemDetailViewModel } from '../models/park-item-detail-view.model';

interface ParkItemDetailSourceData {
  item: ParkItem;
  park: Park | null;
  manufacturerName: string | null;
  zoneName: string | null;
  relatedItems: ParkItem[];
}

@Injectable()
export class ParkItemDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemDetailSourceData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly detail: Signal<ParkItemDetailViewModel | null> = computed(() => {
    const sourceData: ParkItemDetailSourceData | undefined = this.screenStateStore.data();

    return mapParkItemToDetailViewModel(
      sourceData?.item ?? null,
      sourceData?.park ?? null,
      sourceData?.manufacturerName ?? null,
      sourceData?.zoneName ?? null,
      this.currentLanguageSignal(),
      sourceData?.relatedItems ?? []
    );
  });

  constructor(
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadItem(itemId: string): void {
    const previousData: ParkItemDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkItemsApiService.getParkItemById(itemId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (item: ParkItem) => {
        this.screenStateStore.setReady({
          item,
          park: null,
          manufacturerName: null,
          zoneName: null,
          relatedItems: []
        });

        this.loadRelatedData(item);
      },
      error: (error: unknown) => {
        console.error('Error loading park item', error);
        this.screenStateStore.setError('parkItems.detail.errorMessage', previousData);
      }
    });
  }

  private loadRelatedData(item: ParkItem): void {
    this.parksApiService.getParkById(item.parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (park: Park) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          park
        }));
      },
      error: (error: unknown) => {
        console.error('Error loading park for item', error);
        this.screenStateStore.setError('parkItems.detail.errorMessage', this.screenStateStore.data());
      }
    });

    this.parkItemsApiService.getParkItemsByParkId(item.parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items: ParkItem[]) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          relatedItems: items
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          relatedItems: []
        }));
      }
    });

    if (item.attractionDetails?.manufacturerId) {
      this.manufacturersApiService.getAttractionManufacturerById(item.attractionDetails.manufacturerId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (manufacturer: { name?: string | null }) => {
          this.updateReadyData((current: ParkItemDetailSourceData) => ({
            ...current,
            manufacturerName: manufacturer.name ?? null
          }));
        },
        error: () => {
          this.updateReadyData((current: ParkItemDetailSourceData) => ({
            ...current,
            manufacturerName: null
          }));
        }
      });
    }

    if (item.zoneId) {
      this.parkZonesApiService.getParkZoneById(item.zoneId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (zone: { name?: string | null }) => {
          this.updateReadyData((current: ParkItemDetailSourceData) => ({
            ...current,
            zoneName: zone.name ?? null
          }));
        },
        error: () => {
          this.updateReadyData((current: ParkItemDetailSourceData) => ({
            ...current,
            zoneName: null
          }));
        }
      });
    }
  }

  private updateReadyData(updater: (current: ParkItemDetailSourceData) => ParkItemDetailSourceData): void {
    const currentData: ParkItemDetailSourceData | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady(updater(currentData));
  }
}
