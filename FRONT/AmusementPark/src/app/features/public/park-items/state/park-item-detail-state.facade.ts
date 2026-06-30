import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemSiblingNavigation } from '@app/models/parks/park-item-sibling-navigation';
import { HistoryTimeline } from '@app/models/history/history.models';
import { TechnicalPage } from '@app/models/technical-pages/technical-page';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { PagedResult } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { mapParkItemToDetailViewModel } from '../mappers/park-item-detail-view.mapper';
import { ParkItemDetailViewModel } from '../models/park-item-detail-view.model';
import {
  PARK_ITEM_DETAIL_HISTORY_PORT,
  PARK_ITEM_DETAIL_IMAGES_PORT,
  PARK_ITEM_DETAIL_ITEMS_PORT,
  PARK_ITEM_DETAIL_MANUFACTURERS_PORT,
  PARK_ITEM_DETAIL_PARKS_PORT,
  PARK_ITEM_DETAIL_TECHNICAL_PAGES_PORT,
  PARK_ITEM_DETAIL_VIDEOS_PORT,
  PARK_ITEM_DETAIL_ZONES_PORT,
  ParkItemDetailHistoryPort,
  ParkItemDetailImagesPort,
  ParkItemDetailItemsPort,
  ParkItemDetailManufacturersPort,
  ParkItemDetailParksPort,
  ParkItemDetailTechnicalPagesPort,
  ParkItemDetailVideosPort,
  ParkItemDetailZonesPort
} from './park-item-detail-data.ports';

interface ParkItemDetailSourceData {
  item: ParkItem;
  park: Park | null;
  manufacturerName: string | null;
  zoneName: string | null;
  relatedItems: ParkItem[];
  siblingNavigation: ParkItemSiblingNavigation | null;
  photos: ImageDto[];
  hasVideos: boolean;
  hasHistory: boolean;
  technicalPages: TechnicalPage[];
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
      sourceData?.relatedItems ?? [],
      sourceData?.siblingNavigation ?? null,
      sourceData?.photos ?? [],
      this.textTruncator,
      this.measurementPreferenceService.preferredSystem(),
      this.measurementConversionService,
      sourceData?.hasVideos ?? false,
      sourceData?.technicalPages ?? [],
      sourceData?.hasHistory ?? false
    );
  });

  constructor(
    @Inject(PARK_ITEM_DETAIL_ITEMS_PORT) private readonly parkItemsApiService: ParkItemDetailItemsPort,
    @Inject(PARK_ITEM_DETAIL_PARKS_PORT) private readonly parksApiService: ParkItemDetailParksPort,
    @Inject(PARK_ITEM_DETAIL_MANUFACTURERS_PORT) private readonly manufacturersApiService: ParkItemDetailManufacturersPort,
    @Inject(PARK_ITEM_DETAIL_ZONES_PORT) private readonly parkZonesApiService: ParkItemDetailZonesPort,
    @Inject(PARK_ITEM_DETAIL_IMAGES_PORT) private readonly imagesApiService: ParkItemDetailImagesPort,
    @Inject(PARK_ITEM_DETAIL_VIDEOS_PORT) private readonly videosApiService: ParkItemDetailVideosPort,
    @Inject(PARK_ITEM_DETAIL_TECHNICAL_PAGES_PORT) private readonly technicalPagesApiService: ParkItemDetailTechnicalPagesPort,
    @Inject(PARK_ITEM_DETAIL_HISTORY_PORT) private readonly historyApiService: ParkItemDetailHistoryPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly ssrRuntimeService: SsrRuntimeService,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly measurementConversionService: MeasurementConversionService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadItem(itemId: string): void {
    const previousData: ParkItemDetailSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkItemsApiService.getParkItemById(itemId, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (item: ParkItem) => {
        this.screenStateStore.setReady({
          item,
          park: null,
          manufacturerName: null,
          zoneName: null,
          relatedItems: [],
          siblingNavigation: null,
          photos: [],
          hasVideos: false,
          hasHistory: false,
          technicalPages: []
        });

        this.loadRelatedData(item);
      },
      error: (error: unknown) => {
        console.error('Error loading park item', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parkItems.detail.errorMessage', previousData);
      }
    });
  }

  private loadRelatedData(item: ParkItem): void {
    this.parksApiService.getParkById(item.parkId, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (park: Park) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          park
        }));
      },
      error: (error: unknown) => {
        console.error('Error loading park for item', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parkItems.detail.errorMessage', this.screenStateStore.data());
      }
    });

    if (!item.id) {
      return;
    }

    const useMinimalSsrData: boolean = this.ssrRuntimeService.shouldUseMinimalPublicData();
    this.imagesApiService.getImages(ImageOwnerType.PARK_ITEM, item.id, ImageCategory.PARK_ITEM, 1, 1, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (photos: ImageDto[]) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          photos
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          photos: []
        }));
      }
    });

    this.videosApiService.getVideosPage({
      page: 1,
      size: 1,
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: item.id
    }, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: PagedResult<VideoDto>) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          hasVideos: response.pagination.totalItems > 0 || response.items.length > 0
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          hasVideos: false
        }));
      }
    });

    this.historyApiService.getParkItemTimeline(item.id, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (timeline: HistoryTimeline) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          hasHistory: (timeline.events?.length ?? 0) > 0
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          hasHistory: false
        }));
      }
    });

    if (useMinimalSsrData) {
      return;
    }

    this.technicalPagesApiService.getPublicLinkIndex().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (technicalPages: TechnicalPage[]) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          technicalPages
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          technicalPages: []
        }));
      }
    });

    this.parkItemsApiService.getParkItemSiblingNavigation(item.id, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (siblingNavigation: ParkItemSiblingNavigation) => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          siblingNavigation
        }));
      },
      error: () => {
        this.updateReadyData((current: ParkItemDetailSourceData) => ({
          ...current,
          siblingNavigation: null
        }));
      }
    });

    this.parkItemsApiService.getRelatedParkItems(item.id, 3, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
      this.parkZonesApiService.getParkZoneById(item.zoneId, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
