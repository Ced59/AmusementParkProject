import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { ParkDistanceResponse, ParkDistanceTarget } from '@app/models/parks/park-distance';
import { ParkDetailSummary, ParkDetailSummaryStats } from '@app/models/parks/park-detail-summary';
import { ParkOpeningHoursCalendar, ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { ParkExplorer, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { Park } from '@app/models/parks/park';
import { HistoryTimeline } from '@app/models/history/history.models';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';
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
  PARK_DETAIL_HISTORY_PORT,
  PARK_DETAIL_PARKS_PORT,
  PARK_DETAIL_VIDEOS_PORT,
  PARK_DETAIL_IMAGES_PORT,
  ParkDetailHistoryPort,
  ParkDetailImagesPort,
  ParkDetailParksPort,
  ParkDetailVideosPort
} from './park-detail-data.ports';

const NEARBY_PARKS_LIMIT = 4;
const OPENING_HOURS_PREVIEW_PAST_DAYS = 2;
const OPENING_HOURS_PREVIEW_FUTURE_DAYS = 14;
const OPENING_HOURS_NEXT_OPENING_LOOKAHEAD_DAYS = 370;

@Injectable()
export class ParkDetailStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkDetailSummary>();
  private readonly nearbyStateStore = new SignalScreenStateStore<ParkDistanceTarget[]>();
  private readonly weatherStateStore = new SignalScreenStateStore<ParkWeatherForecast>();
  private readonly openingHoursStateStore = new SignalScreenStateStore<ParkOpeningHoursCalendar>();
  private readonly currentLanguageSignal = signal('en');
  private readonly hasVideosSignal = signal(false);
  private readonly hasImagesSignal = signal(false);
  private readonly hasHistorySignal = signal(false);

  public readonly state = this.screenStateStore.state;
  public readonly nearbyState = this.nearbyStateStore.state;
  public readonly weatherState = this.weatherStateStore.state;
  public readonly openingHoursState = this.openingHoursStateStore.state;
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
        this.hasImagesSignal(),
        this.hasHistorySignal()
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
  public readonly openingHours: Signal<ParkOpeningHoursCalendar | null> = computed(() => this.openingHoursStateStore.data() ?? null);

  constructor(
    @Inject(PARK_DETAIL_PARKS_PORT) private readonly parksApiService: ParkDetailParksPort,
    @Inject(PARK_DETAIL_VIDEOS_PORT) private readonly videosApiService: ParkDetailVideosPort,
    @Inject(PARK_DETAIL_IMAGES_PORT) private readonly imagesApiService: ParkDetailImagesPort,
    @Inject(PARK_DETAIL_HISTORY_PORT) private readonly historyApiService: ParkDetailHistoryPort,
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
    const previousOpeningHoursData: ParkOpeningHoursCalendar | undefined = this.openingHoursStateStore.data();
    this.screenStateStore.setLoading(previousData);
    this.nearbyStateStore.setLoading(previousNearbyData);
    this.weatherStateStore.setLoading(previousWeatherData);
    this.openingHoursStateStore.setLoading(previousOpeningHoursData);
    this.hasVideosSignal.set(false);
    this.hasImagesSignal.set(false);
    this.hasHistorySignal.set(false);

    this.parksApiService.getParkDetailSummary(id, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (summary: ParkDetailSummary) => {
        this.screenStateStore.setReady(summary);
        this.hasImagesSignal.set(this.hasKnownImage(summary));
        this.loadNearbyParks(summary.park);
        this.loadWeather(summary.park);
        this.loadOpeningHours(summary.park);
        this.loadVideoAvailability(summary.park);
        this.loadImageAvailability(summary);
        this.loadHistoryAvailability(summary.park);
      },
      error: (error: unknown) => {
        console.error('Error loading park detail summary', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.detail.errorMessage', previousData);
        this.nearbyStateStore.setError('parks.detail.nearby.errorMessage', previousNearbyData);
        this.weatherStateStore.setError('parkWeather.errorMessage', previousWeatherData);
        this.openingHoursStateStore.setError('parkOpeningHours.errorMessage', previousOpeningHoursData);
      }
    });
  }

  private loadOpeningHours(park: Park): void {
    const parkId: string | null = park.id?.trim() ?? null;

    if (!parkId) {
      this.openingHoursStateStore.setEmpty(undefined);
      return;
    }

    const range: { from: string; to: string } = this.resolveOpeningHoursPreviewRange();
    this.parksApiService.getParkOpeningHours(parkId, range.from, range.to, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (calendar: ParkOpeningHoursCalendar) => {
        if (!this.hasOpeningHoursCoverage(calendar)) {
          this.openingHoursStateStore.setEmpty(calendar);
          return;
        }

        if (!this.hasCurrentOrFutureOpening(calendar)) {
          this.loadOpeningHoursNextOpeningWindow(parkId, calendar);
          return;
        }

        this.openingHoursStateStore.setReady(calendar);
      },
      error: (error: unknown) => {
        if (hasHttpStatus(error, 404)) {
          this.openingHoursStateStore.setEmpty(undefined);
          return;
        }

        console.error('Error loading park opening hours', error);
        this.openingHoursStateStore.setError('parkOpeningHours.errorMessage');
      }
    });
  }

  private loadOpeningHoursNextOpeningWindow(parkId: string, previewCalendar: ParkOpeningHoursCalendar): void {
    const range: { from: string; to: string } = this.resolveOpeningHoursNextOpeningRange();
    this.parksApiService.getParkOpeningHours(parkId, range.from, range.to, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (futureCalendar: ParkOpeningHoursCalendar) => {
        this.openingHoursStateStore.setReady(this.mergeOpeningHoursCalendars(previewCalendar, futureCalendar));
      },
      error: (error: unknown) => {
        console.error('Error loading next park opening hours window', error);
        this.openingHoursStateStore.setReady(previewCalendar);
      }
    });
  }

  private resolveOpeningHoursPreviewRange(): { from: string; to: string } {
    const today: Date = new Date();
    const fromDate: Date = new Date(today);
    fromDate.setDate(today.getDate() - OPENING_HOURS_PREVIEW_PAST_DAYS);
    const toDate: Date = new Date(today);
    toDate.setDate(today.getDate() + OPENING_HOURS_PREVIEW_FUTURE_DAYS);

    return {
      from: this.formatLocalDate(fromDate),
      to: this.formatLocalDate(toDate)
    };
  }

  private resolveOpeningHoursNextOpeningRange(): { from: string; to: string } {
    const today: Date = new Date();
    const fromDate: Date = new Date(today);
    fromDate.setDate(today.getDate() + OPENING_HOURS_PREVIEW_FUTURE_DAYS + 1);
    const toDate: Date = new Date(today);
    toDate.setDate(today.getDate() + OPENING_HOURS_NEXT_OPENING_LOOKAHEAD_DAYS);

    return {
      from: this.formatLocalDate(fromDate),
      to: this.formatLocalDate(toDate)
    };
  }

  private hasOpeningHoursCoverage(calendar: ParkOpeningHoursCalendar): boolean {
    return !!calendar.firstDate || !!calendar.lastDate || (calendar.days?.length ?? 0) > 0;
  }

  private hasCurrentOrFutureOpening(calendar: ParkOpeningHoursCalendar): boolean {
    const parkNow: { localDate: string; minutes: number } = this.resolveParkNow(calendar.timeZoneId, new Date());
    const previousDay: ParkOpeningHoursDay | null = calendar.days.find((day: ParkOpeningHoursDay): boolean => {
      return day.localDate === this.addDays(parkNow.localDate, -1);
    }) ?? null;

    if (this.findActiveRange(previousDay, parkNow.minutes + 1440, true) !== null) {
      return true;
    }

    for (const day of calendar.days ?? []) {
      if (day.isClosed || day.timeRanges.length === 0) {
        continue;
      }

      const dayOffset: number = this.diffLocalDatesInDays(parkNow.localDate, day.localDate);
      if (dayOffset < 0) {
        continue;
      }

      for (const range of day.timeRanges) {
        const opensAt: number = (dayOffset * 1440) + this.toMinutes(range.opensAt);
        const closesAt: number = (dayOffset * 1440) + this.toMinutes(range.closesAt) + (range.closesNextDay ? 1440 : 0);
        if (parkNow.minutes >= opensAt && parkNow.minutes < closesAt) {
          return true;
        }

        if (opensAt > parkNow.minutes) {
          return true;
        }
      }
    }

    return false;
  }

  private findActiveRange(day: ParkOpeningHoursDay | null, currentMinutes: number, requireNextDay: boolean): ParkOpeningHoursTimeRange | null {
    if (!day || day.isClosed) {
      return null;
    }

    return day.timeRanges.find((range: ParkOpeningHoursTimeRange): boolean => {
      if (requireNextDay && !range.closesNextDay) {
        return false;
      }

      const opensAt: number = this.toMinutes(range.opensAt);
      const closesAt: number = this.toMinutes(range.closesAt) + (range.closesNextDay ? 1440 : 0);
      return currentMinutes >= opensAt && currentMinutes < closesAt;
    }) ?? null;
  }

  private mergeOpeningHoursCalendars(first: ParkOpeningHoursCalendar, second: ParkOpeningHoursCalendar): ParkOpeningHoursCalendar {
    const daysByDate: Map<string, ParkOpeningHoursDay> = new Map<string, ParkOpeningHoursDay>();
    for (const day of [...(first.days ?? []), ...(second.days ?? [])]) {
      daysByDate.set(day.localDate, day);
    }

    return {
      ...first,
      sourceUrl: first.sourceUrl ?? second.sourceUrl,
      notes: first.notes ?? second.notes,
      lastVerifiedAtUtc: first.lastVerifiedAtUtc ?? second.lastVerifiedAtUtc,
      updatedAtUtc: first.updatedAtUtc || second.updatedAtUtc,
      firstDate: first.firstDate ?? second.firstDate,
      lastDate: first.lastDate ?? second.lastDate,
      toDate: second.toDate > first.toDate ? second.toDate : first.toDate,
      days: [...daysByDate.values()].sort((left: ParkOpeningHoursDay, right: ParkOpeningHoursDay): number => {
        return left.localDate.localeCompare(right.localDate);
      })
    };
  }

  private resolveParkNow(timeZoneId: string | null | undefined, now: Date): { localDate: string; minutes: number } {
    let parts: Intl.DateTimeFormatPart[];
    try {
      parts = new Intl.DateTimeFormat('en-CA', {
        timeZone: timeZoneId || undefined,
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23'
      }).formatToParts(now);
    } catch {
      parts = new Intl.DateTimeFormat('en-CA', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23'
      }).formatToParts(now);
    }

    const valueByType: Record<string, string> = Object.fromEntries(parts.map((part: Intl.DateTimeFormatPart) => [part.type, part.value]));
    const hour: number = Number(valueByType['hour'] ?? '0');
    const minute: number = Number(valueByType['minute'] ?? '0');

    return {
      localDate: `${valueByType['year']}-${valueByType['month']}-${valueByType['day']}`,
      minutes: (hour * 60) + minute
    };
  }

  private addDays(localDate: string, offset: number): string {
    const parts: number[] = localDate.split('-').map((part: string): number => Number(part));
    const date: Date = new Date(Date.UTC(parts[0], parts[1] - 1, parts[2]));
    date.setUTCDate(date.getUTCDate() + offset);
    return date.toISOString().slice(0, 10);
  }

  private diffLocalDatesInDays(fromLocalDate: string, toLocalDate: string): number {
    const fromParts: number[] = fromLocalDate.split('-').map((part: string): number => Number(part));
    const toParts: number[] = toLocalDate.split('-').map((part: string): number => Number(part));
    const fromTime: number = Date.UTC(fromParts[0], fromParts[1] - 1, fromParts[2]);
    const toTime: number = Date.UTC(toParts[0], toParts[1] - 1, toParts[2]);
    return Math.round((toTime - fromTime) / 86400000);
  }

  private toMinutes(value: string): number {
    const parts: number[] = value.split(':').map((part: string): number => Number(part));
    return ((parts[0] || 0) * 60) + (parts[1] || 0);
  }

  private formatLocalDate(date: Date): string {
    const year: number = date.getFullYear();
    const month: string = String(date.getMonth() + 1).padStart(2, '0');
    const day: string = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
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

    forkJoin({
      parkVideos: this.videosApiService.getVideosPage({
        page: 1,
        size: 1,
        ownerType: VideoOwnerType.PARK,
        ownerId: parkId
      }, anonymousHttpOptions()),
      itemVideos: this.videosApiService.getParkItemVideosByPark(parkId, {
        page: 1,
        size: 1
      }, anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { parkVideos: PagedResult<VideoDto>; itemVideos: PagedResult<ParkItemVideoDto> }) => {
        this.hasVideosSignal.set(
          response.parkVideos.pagination.totalItems > 0 ||
          response.parkVideos.items.length > 0 ||
          response.itemVideos.pagination.totalItems > 0 ||
          response.itemVideos.items.length > 0
        );
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

  private loadHistoryAvailability(park: Park): void {
    const parkId: string | null = park.id?.trim() ?? null;

    if (!parkId) {
      this.hasHistorySignal.set(false);
      return;
    }

    this.historyApiService.getParkTimeline(parkId, true, [], anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (timeline: HistoryTimeline) => {
        this.hasHistorySignal.set((timeline.events?.length ?? 0) > 0);
      },
      error: () => {
        this.hasHistorySignal.set(false);
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
