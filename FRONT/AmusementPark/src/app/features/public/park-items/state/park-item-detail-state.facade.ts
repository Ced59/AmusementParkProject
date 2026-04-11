import { Injectable, Signal, computed } from '@angular/core';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';

interface ParkItemDetailViewModel {
  item: ParkItem;
  park: Park | null;
  manufacturerName: string | null;
  zoneName: string | null;
}

@Injectable()
export class ParkItemDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemDetailViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly item: Signal<ParkItem | null> = computed(() => this.screenStateStore.data()?.item ?? null);
  public readonly park: Signal<Park | null> = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly manufacturerName: Signal<string | null> = computed(() => this.screenStateStore.data()?.manufacturerName ?? null);
  public readonly zoneName: Signal<string | null> = computed(() => this.screenStateStore.data()?.zoneName ?? null);

  constructor(
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly parkZonesApiService: ParkZonesApiService
  ) {
  }

  loadItem(itemId: string): void {
    const previousData: ParkItemDetailViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkItemsApiService.getParkItemById(itemId).subscribe({
      next: (item: ParkItem) => {
        this.screenStateStore.setReady({
          item,
          park: null,
          manufacturerName: null,
          zoneName: null
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
    this.parksApiService.getParkById(item.parkId).subscribe({
      next: (park: Park) => {
        this.updateReadyData((current: ParkItemDetailViewModel) => ({
          ...current,
          park
        }));
      },
      error: (error: unknown) => {
        console.error('Error loading park for item', error);
        this.screenStateStore.setError('parkItems.detail.errorMessage', this.screenStateStore.data());
      }
    });

    if (item.attractionDetails?.manufacturerId) {
      this.manufacturersApiService.getAttractionManufacturerById(item.attractionDetails.manufacturerId).subscribe({
        next: (manufacturer: { name?: string | null }) => {
          this.updateReadyData((current: ParkItemDetailViewModel) => ({
            ...current,
            manufacturerName: manufacturer.name ?? null
          }));
        },
        error: () => {
          this.updateReadyData((current: ParkItemDetailViewModel) => ({
            ...current,
            manufacturerName: null
          }));
        }
      });
    }

    if (item.zoneId) {
      this.parkZonesApiService.getParkZoneById(item.zoneId).subscribe({
        next: (zone: { name?: string | null }) => {
          this.updateReadyData((current: ParkItemDetailViewModel) => ({
            ...current,
            zoneName: zone.name ?? null
          }));
        },
        error: () => {
          this.updateReadyData((current: ParkItemDetailViewModel) => ({
            ...current,
            zoneName: null
          }));
        }
      });
    }
  }

  private updateReadyData(updater: (current: ParkItemDetailViewModel) => ParkItemDetailViewModel): void {
    const currentData: ParkItemDetailViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady(updater(currentData));
  }
}
