import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of } from 'rxjs';

import { MapMarker } from '@app/models/map/map-marker';
import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkExplorer, ParkExplorerBucket, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { resolveParkItemMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import {
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicParkZoneRouteCommands,
  buildPublicParkZonesRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import { buildParkItemMapDetailRouteCommands } from '@shared/services/maps/map-marker-detail-route.helpers';
import { mapParkItemToCardViewModel } from '../../park-items/mappers/park-item-card.mapper';
import { ParkItemCardViewModel } from '../../park-items/models/park-item-card.model';
import { ParkItemsCountTagViewModel } from '../../park-items/models/park-items-page-view.model';
import {
  ParkZoneMapViewModel,
  ParkZoneOverviewCardViewModel,
  ParkZonePageViewModel,
  ParkZonesPageViewModel
} from '../models/park-zone-page.model';
import {
  PARK_ZONES_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT,
  PARK_ZONES_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT,
  PARK_ZONES_PAGE_STATE_PARKS_API_SERVICE_PORT,
  ParkZonesPageStateParkItemsApiServicePort,
  ParkZonesPageStateParkZonesApiServicePort,
  ParkZonesPageStateParksApiServicePort
} from './park-zones-page-state-data.ports';

interface ParkZonesPageSourceData {
  park: Park;
  parkImageId: string | null;
  explorer: ParkExplorer;
  zones: ParkZone[];
  items: ParkItem[];
}

@Injectable()
export class ParkZonesPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkZonesPageSourceData>();
  private readonly currentLanguageSignal = signal('en');
  private readonly selectedZoneIdSignal = signal<string | null>(null);

  public readonly state = this.screenStateStore.state;
  public readonly parkImageId: Signal<string | null> = computed(() => this.screenStateStore.data()?.parkImageId ?? null);
  public readonly zonesPage: Signal<ParkZonesPageViewModel | null> = computed(() => {
    const park: Park | null = this.park();
    const explorer: ParkExplorer | null = this.explorer();

    if (!park || !explorer) {
      return null;
    }

    const zones: ParkZoneOverviewCardViewModel[] = this.publicZones()
      .map((zone: ParkZone) => this.mapZoneCard(park, explorer, zone))
      .filter((zone: ParkZoneOverviewCardViewModel) => zone.totalItems > 0);

    return {
      parkName: park.name ?? '',
      parkLink: buildPublicParkRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      itemsLink: buildPublicParkItemsRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      zoneCount: zones.length,
      totalItems: zones.reduce((total: number, zone: ParkZoneOverviewCardViewModel) => total + zone.totalItems, 0),
      zones
    };
  });
  public readonly zonePage: Signal<ParkZonePageViewModel | null> = computed(() => {
    const park: Park | null = this.park();
    const zone: ParkZone | null = this.selectedZone();

    if (!park || !zone) {
      return null;
    }

    const zoneItems: ParkItem[] = this.zoneItems();
    const zoneName: string = this.resolveZoneName(zone);

    return {
      parkName: park.name ?? '',
      parkLink: buildPublicParkRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      zonesLink: buildPublicParkZonesRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      allItemsLink: buildPublicParkItemsRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      zoneName,
      zoneDescription: this.resolveZoneDescription(zone),
      totalItems: zoneItems.length,
      typeHighlights: this.buildTypeHighlights(zoneItems, 5),
      map: this.buildMap(park, zoneItems),
      items: zoneItems.map((item: ParkItem) => mapParkItemToCardViewModel(
        item,
        park,
        this.currentLanguageSignal(),
        null,
        zoneName,
        this.textTruncator,
        this.measurementPreferenceService.preferredSystem(),
        this.measurementConversionService
      ))
    };
  });

  private readonly park: Signal<Park | null> = computed(() => this.screenStateStore.data()?.park ?? null);
  private readonly explorer: Signal<ParkExplorer | null> = computed(() => this.screenStateStore.data()?.explorer ?? null);
  private readonly zones: Signal<ParkZone[]> = computed(() => this.screenStateStore.data()?.zones ?? []);
  private readonly items: Signal<ParkItem[]> = computed(() => this.screenStateStore.data()?.items ?? []);
  private readonly publicZones: Signal<ParkZone[]> = computed(() => {
    return this.zones()
      .filter((zone: ParkZone) => zone.isVisible !== false)
      .sort((left: ParkZone, right: ParkZone) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0) || this.resolveZoneName(left).localeCompare(this.resolveZoneName(right)));
  });
  private readonly selectedZone: Signal<ParkZone | null> = computed(() => {
    const zoneId: string | null = this.selectedZoneIdSignal();

    if (!zoneId) {
      return null;
    }

    return this.publicZones().find((zone: ParkZone) => zone.id === zoneId) ?? null;
  });
  private readonly zoneItems: Signal<ParkItem[]> = computed(() => {
    const zoneId: string | null = this.selectedZoneIdSignal();

    if (!zoneId) {
      return [];
    }

    return this.items()
      .filter((item: ParkItem) => item.isVisible !== false)
      .filter((item: ParkItem) => item.zoneId === zoneId)
      .sort((left: ParkItem, right: ParkItem) => left.name.localeCompare(right.name));
  });

  constructor(
    @Inject(PARK_ZONES_PAGE_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: ParkZonesPageStateParksApiServicePort,
    @Inject(PARK_ZONES_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: ParkZonesPageStateParkZonesApiServicePort,
    @Inject(PARK_ZONES_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: ParkZonesPageStateParkItemsApiServicePort,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly measurementConversionService: MeasurementConversionService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  setSelectedZone(zoneId: string | null): void {
    this.selectedZoneIdSignal.set(zoneId);
  }

  loadData(parkId: string, currentLanguage: string): void {
    this.currentLanguageSignal.set(currentLanguage);

    const previousData: ParkZonesPageSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      summary: this.parksApiService.getParkDetailSummary(parkId, anonymousHttpOptions()),
      explorer: this.parksApiService.getParkExplorer(parkId, anonymousHttpOptions()),
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId, anonymousHttpOptions()).pipe(catchError(() => of([] as ParkZone[]))),
      items: this.parkItemsApiService.getParkItemsByParkId(parkId, anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data: { summary: ParkDetailSummary; explorer: ParkExplorer; zones: ParkZone[]; items: ParkItem[] }) => {
        this.screenStateStore.setReady({
          park: data.summary.park,
          parkImageId: data.summary.mainImage?.id ?? null,
          explorer: data.explorer,
          zones: data.zones,
          items: data.items
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park zones page', error);
        this.screenStateStore.setError('parks.zones.errorMessage', previousData);
      }
    });
  }

  private mapZoneCard(park: Park, explorer: ParkExplorer, zone: ParkZone): ParkZoneOverviewCardViewModel {
    const zoneName: string = this.resolveZoneName(zone);
    const bucket: ParkExplorerBucket | null = this.resolveZoneBucket(explorer, zone.id ?? null);

    return {
      id: zone.id ?? '',
      name: zoneName,
      slug: zone.slug ?? zoneName,
      description: this.resolveZoneDescription(zone),
      totalItems: bucket?.totalItems ?? 0,
      typeHighlights: this.buildTypeHighlightsFromCounts(bucket?.countsByType ?? [], 3),
      zoneLink: buildPublicParkZoneRouteCommands({
        language: this.currentLanguageSignal(),
        parkId: park.id,
        parkName: park.name,
        zoneId: zone.id,
        zoneName
      }),
      itemsLink: buildPublicParkItemsRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      itemsQueryParams: zone.id ? { zone: zone.id } : null
    };
  }

  private resolveZoneBucket(explorer: ParkExplorer, zoneId: string | null): ParkExplorerBucket | null {
    if (!zoneId) {
      return null;
    }

    return explorer.zones.find((bucket: ParkExplorerBucket) => bucket.id === zoneId) ?? null;
  }

  private resolveZoneName(zone: ParkZone): string {
    return this.normalizeOptionalString(resolveLocalizedValue(zone.names, this.currentLanguageSignal()))
      ?? this.normalizeOptionalString(zone.name)
      ?? this.normalizeOptionalString(zone.id)
      ?? '';
  }

  private resolveZoneDescription(zone: ParkZone): string | null {
    return this.normalizeOptionalString(resolveLocalizedValue(zone.descriptions, this.currentLanguageSignal()));
  }

  private buildTypeHighlights(items: ParkItem[], maxCount: number): ParkItemsCountTagViewModel[] {
    const countsByType: Map<string, number> = new Map<string, number>();

    for (const item of items) {
      if (!item.type) {
        continue;
      }

      countsByType.set(item.type, (countsByType.get(item.type) ?? 0) + 1);
    }

    return this.buildTypeHighlightsFromCounts(
      Array.from(countsByType.entries()).map(([key, count]: [string, number]) => ({ key, count })),
      maxCount
    );
  }

  private buildTypeHighlightsFromCounts(counts: ParkExplorerCount[], maxCount: number): ParkItemsCountTagViewModel[] {
    return [...counts]
      .filter((count: ParkExplorerCount) => count.count > 0)
      .sort((left: ParkExplorerCount, right: ParkExplorerCount) => right.count - left.count || left.key.localeCompare(right.key))
      .slice(0, maxCount)
      .map((count: ParkExplorerCount) => ({
        value: count.key,
        labelKey: getParkItemTypeTranslationKey(count.key),
        count: count.count
      }));
  }

  private buildMap(park: Park, zoneItems: ParkItem[]): ParkZoneMapViewModel {
    const markers: MapMarker[] = zoneItems
      .filter((item: ParkItem) => this.hasValidPosition(item))
      .map((item: ParkItem) => ({
        id: item.id ?? `${item.name}-${item.latitude}-${item.longitude}`,
        lat: item.latitude!,
        lng: item.longitude!,
        title: item.name,
        subtitle: item.category,
        detailTranslationKeys: item.type ? [getParkItemTypeTranslationKey(item.type)] : [],
        directionsActionEnabled: true,
        iconKind: resolveParkItemMarkerIconKind({
          category: item.category,
          type: item.type,
          subtype: item.subtype ?? null
        }),
        detailActionRouteCommands: buildParkItemMapDetailRouteCommands({
          language: this.currentLanguageSignal(),
          parkId: park.id,
          parkName: park.name,
          itemId: item.id,
          itemName: item.name
        })
      }))
      .sort((left: MapMarker, right: MapMarker) => (left.title ?? '').localeCompare(right.title ?? ''));

    return {
      center: this.resolveMapCenter(park, markers),
      markers,
      hasMarkers: markers.length > 0
    };
  }

  private resolveMapCenter(park: Park, markers: MapMarker[]): [number, number] {
    if (markers.length > 0) {
      return [markers[0].lat, markers[0].lng];
    }

    if (Number.isFinite(park.latitude) && Number.isFinite(park.longitude)) {
      return [park.latitude!, park.longitude!];
    }

    return [0, 0];
  }

  private hasValidPosition(item: ParkItem): boolean {
    return item.latitude != null
      && item.longitude != null
      && Number.isFinite(item.latitude)
      && Number.isFinite(item.longitude)
      && Math.abs(item.latitude) <= 90
      && Math.abs(item.longitude) <= 180
      && !(item.latitude === 0 && item.longitude === 0);
  }

  private normalizeOptionalString(value: string | null | undefined): string | null {
    const trimmedValue: string = value?.trim() ?? '';
    return trimmedValue.length > 0 ? trimmedValue : null;
  }
}
