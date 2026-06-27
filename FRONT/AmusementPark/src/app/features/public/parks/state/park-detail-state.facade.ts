import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { ParkDistanceResponse, ParkDistanceTarget } from '@app/models/parks/park-distance';
import { ParkDetailSummary, ParkDetailSummaryStats } from '@app/models/parks/park-detail-summary';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { Park } from '@app/models/parks/park';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { PagedResult } from '@shared/models/contracts';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { mapNullable } from '@shared/utils/mapping';
import { mapParkDistanceTargetToCardModel } from '../mappers/park-distance-card.mapper';
import { mapParkContentSummaryViewModel } from '../mappers/park-content-summary.mapper';
import { mapParkToDetailViewModel } from '../mappers/park-detail-view.mapper';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import {
  PARK_DETAIL_PARKS_PORT,
  PARK_DETAIL_VIDEOS_PORT,
  PARK_DETAIL_IMAGES_PORT,
  ParkDetailImagesPort,
  ParkDetailParksPort,
  ParkDetailVideosPort
} from './park-detail-data.ports';

const NEARBY_PARKS_LIMIT = 4;

@Injectable()
export class ParkDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkDetailSummary>();
  private readonly nearbyStateStore = new SignalScreenStateStore<ParkDistanceTarget[]>();
  private readonly weatherStateStore = new SignalScreenStateStore<ParkWeatherForecast>();
  private readonly currentLanguageSignal = signal('en');
  private readonly hasVideosSignal = signal(false);
  private readonly hasImagesSignal = signal(false);

  public readonly state = this.screenStateStore.state;
  public readonly nearbyState = this.nearbyStateStore.state;
  public readonly weatherState = this.weatherStateStore.state;
  public readonly park: Signal<ParkDetailViewModel | null> = computed(() => {
    const summary: ParkDetailSummary | undefined = this.screenStateStore.data();

    return mapNullable(summary, (currentSummary: ParkDetailSummary) => {
      return mapParkToDetailViewModel(
        currentSummary.park,
        this.currentLanguageSignal(),
        {
          founderName: currentSummary.references?.founderName ?? null,
          operatorName: currentSummary.references?.operatorName ?? null,
          countryName: this.countryDisplayService.resolveLocalizedCountryName(currentSummary.park.countryCode, this.currentLanguageSignal())
        },
        {
          totalItems: currentSummary.stats?.totalItems ?? null,
          zoneCount: currentSummary.stats?.zoneCount ?? null
        },
        currentSummary.mainImage ? [currentSummary.mainImage] : [],
        [],
        [],
        currentSummary.rating ?? null,
        this.hasVideosSignal(),
        this.hasImagesSignal()
      );
    });
  });
  public readonly summary: Signal<ParkContentSummaryViewModel | null> = computed(() => {
    return mapParkContentSummaryViewModel(this.park(), this.toExplorerSummary(this.screenStateStore.data()));
  });
  public readonly nearbyParks: Signal<ParkCardModel[]> = computed(() => {
    const targets: ParkDistanceTarget[] = this.nearbyStateStore.data() ?? [];
    const currentLanguage: string = this.currentLanguageSignal();

    return targets.map((target: ParkDistanceTarget) => mapParkDistanceTargetToCardModel(
      target,
      currentLanguage,
      this.countryDisplayService,
      this.textTruncator,
      this.measurementPreferenceService.preferredSystem(),
      this.measurementConversionService
    ));
  });
  public readonly weather: Signal<ParkWeatherForecast | null> = computed(() => this.weatherStateStore.data() ?? null);

  constructor(
    @Inject(PARK_DETAIL_PARKS_PORT) private readonly parksApiService: ParkDetailParksPort,
    @Inject(PARK_DETAIL_VIDEOS_PORT) private readonly videosApiService: ParkDetailVideosPort,
    @Inject(PARK_DETAIL_IMAGES_PORT) private readonly imagesApiService: ParkDetailImagesPort,
    private readonly countryDisplayService: CountryDisplayService,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly measurementConversionService: MeasurementConversionService,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadPark(id: string): void {
    const previousData: ParkDetailSummary | undefined = this.screenStateStore.data();
    const previousNearbyData: ParkDistanceTarget[] | undefined = this.nearbyStateStore.data();
    const previousWeatherData: ParkWeatherForecast | undefined = this.weatherStateStore.data();
    this.screenStateStore.setLoading(previousData);
    this.nearbyStateStore.setLoading(previousNearbyData);
    this.weatherStateStore.setLoading(previousWeatherData);
    this.hasVideosSignal.set(false);
    this.hasImagesSignal.set(false);

    this.parksApiService.getParkDetailSummary(id, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (summary: ParkDetailSummary) => {
        this.screenStateStore.setReady(summary);
        this.hasImagesSignal.set(this.hasKnownImage(summary));
        this.loadNearbyParks(summary.park);
        this.loadWeather(summary.park);
        this.loadVideoAvailability(summary.park);
        this.loadImageAvailability(summary);
      },
      error: (error: unknown) => {
        console.error('Error loading park detail summary', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.detail.errorMessage', previousData);
        this.nearbyStateStore.setError('parks.detail.nearby.errorMessage', previousNearbyData);
        this.weatherStateStore.setError('parkWeather.errorMessage', previousWeatherData);
      }
    });
  }

  private loadWeather(park: Park): void {
    const parkId: string | null = park.id?.trim() ?? null;

    if (!parkId || !Number.isFinite(park.latitude) || !Number.isFinite(park.longitude)) {
      this.weatherStateStore.setEmpty(undefined);
      return;
    }

    this.parksApiService.getParkWeather(parkId, 7, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (forecast: ParkWeatherForecast) => {
        if (!forecast.days || forecast.days.length === 0) {
          this.weatherStateStore.setEmpty(forecast);
          return;
        }

        this.weatherStateStore.setReady(forecast);
      },
      error: (error: unknown) => {
        console.error('Error loading park weather', error);
        this.weatherStateStore.setError('parkWeather.errorMessage');
      }
    });
  }

  private loadVideoAvailability(park: Park): void {
    const parkId: string | null = park.id?.trim() ?? null;

    if (!parkId) {
      this.hasVideosSignal.set(false);
      return;
    }

    this.videosApiService.getVideosPage({
      page: 1,
      size: 1,
      ownerType: VideoOwnerType.PARK,
      ownerId: parkId
    }, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: PagedResult<VideoDto>) => {
        this.hasVideosSignal.set(response.pagination.totalItems > 0 || response.items.length > 0);
      },
      error: () => {
        this.hasVideosSignal.set(false);
      }
    });
  }

  private loadImageAvailability(summary: ParkDetailSummary): void {
    if (this.hasKnownImage(summary)) {
      this.hasImagesSignal.set(true);
      return;
    }

    const parkId: string | null = summary.park.id?.trim() ?? null;
    if (!parkId) {
      this.hasImagesSignal.set(false);
      return;
    }

    forkJoin({
      parkImages: this.imagesApiService.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.PARK, 1, 1, anonymousHttpOptions()),
      logoImages: this.imagesApiService.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.LOGO, 1, 1, anonymousHttpOptions()),
      itemImages: this.imagesApiService.getParkItemImagesByPark(parkId, 1, 1, anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { parkImages: PagedResult<ImageDto>; logoImages: PagedResult<ImageDto>; itemImages: PagedResult<ParkItemImageDto> }) => {
        this.hasImagesSignal.set(
          response.parkImages.pagination.totalItems > 0 ||
          response.logoImages.pagination.totalItems > 0 ||
          response.itemImages.pagination.totalItems > 0
        );
      },
      error: () => {
        this.hasImagesSignal.set(false);
      }
    });
  }

  private loadNearbyParks(park: Park): void {
    const parkId: string | null = park.id?.trim() ?? null;

    if (!parkId || !Number.isFinite(park.latitude) || !Number.isFinite(park.longitude)) {
      this.nearbyStateStore.setEmpty([]);
      return;
    }

    this.parksApiService.getNearestParks(parkId, NEARBY_PARKS_LIMIT, null, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: ParkDistanceResponse) => {
        const targets: ParkDistanceTarget[] = response.targets
          .filter((target: ParkDistanceTarget) => !!target.park?.id && target.park.id !== parkId)
          .slice(0, NEARBY_PARKS_LIMIT);

        if (targets.length === 0) {
          this.nearbyStateStore.setEmpty([]);
          return;
        }

        this.nearbyStateStore.setReady(targets);
      },
      error: (error: unknown) => {
        console.error('Error loading nearby parks', error);
        this.nearbyStateStore.setError('parks.detail.nearby.errorMessage', []);
      }
    });
  }

  private toExplorerSummary(summary: ParkDetailSummary | undefined): ParkExplorer | null {
    if (!summary?.park.id || !summary.stats) {
      return null;
    }

    const categoryCounts: ParkExplorerCount[] = this.toCategoryCounts(summary.stats);

    return {
      parkId: summary.park.id,
      hasZones: summary.stats.zoneCount > 0,
      overview: {
        id: null,
        name: 'overview',
        names: [],
        slug: null,
        isVirtual: true,
        totalItems: summary.stats.totalItems,
        countsByCategory: categoryCounts,
        countsByType: []
      },
      zones: [],
      unassigned: null
    };
  }

  private hasKnownImage(summary: ParkDetailSummary): boolean {
    return !!summary.mainImage?.id || !!summary.park.currentLogoImageId?.trim();
  }

  private toCategoryCounts(stats: ParkDetailSummaryStats): ParkExplorerCount[] {
    return Object.entries(stats.countsByCategory ?? {})
      .map(([key, count]: [string, number]) => ({ key, count }))
      .filter((entry: ParkExplorerCount) => entry.count > 0);
  }
}
