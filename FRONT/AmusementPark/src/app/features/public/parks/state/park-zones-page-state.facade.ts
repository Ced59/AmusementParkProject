import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { MapMarker } from '@app/models/map/map-marker';
import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkExplorer, ParkExplorerBucket, ParkExplorerCount } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkMapItem, ParkMapItems, ParkMapUnlocatedItem } from '@app/models/parks/park-map-items';
import { ParkZone } from '@app/models/parks/park-zone';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PagedResult, DEFAULT_PAGINATION } from '@shared/models/contracts';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveParkSummarySocialImageId } from '@shared/utils/images/park-social-image.helpers';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { resolveParkItemMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { resolvePublicParkItemsClosedFilter } from '@shared/utils/parks/public-park-items-closed-filter.helper';
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
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

interface ParkZonesPageSourceData {
  park: Park;
  parkImageId: string | null;
  explorer: ParkExplorer;
  zones: ParkZone[];
  mapItems: ParkItem[];
  itemsPage: PagedResult<ParkItem>;
}

const ZONE_ITEMS_PREVIEW_SIZE = 24;
const EMPTY_ZONE_ITEMS_PAGE: PagedResult<ParkItem> = {
  items: [],
  pagination: DEFAULT_PAGINATION
};

@Injectable()
export class ParkZonesPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkZonesPageSourceData>();
  private readonly currentLanguageSignal = signal('en');
  private readonly selectedZoneIdSignal = signal<string | null>(null);
  private readonly selectedClosedFilterSignal = signal<ClosedEntityFilter>(DEFAULT_CLOSED_ENTITY_FILTER);

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
    const explorer: ParkExplorer | null = this.explorer();
    const zone: ParkZone | null = this.selectedZone();

    if (!park || !explorer || !zone) {
      return null;
    }

    const zoneMapItems: ParkItem[] = this.zoneMapItems();
    const zoneCardItems: ParkItem[] = this.zoneCardItems();
    const zoneName: string = this.resolveZoneName(zone);
    const zoneBucket: ParkExplorerBucket | null = this.resolveZoneBucket(explorer, zone.id ?? null);

    return {
      parkName: park.name ?? '',
      parkLink: buildPublicParkRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      zonesLink: buildPublicParkZonesRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      allItemsLink: buildPublicParkItemsRouteCommands({ language: this.currentLanguageSignal(), parkId: park.id, parkName: park.name }),
      allItemsQueryParams: zone.id ? { zone: zone.id } : null,
      zoneName,
      zoneDescription: this.resolveZoneDescription(zone),
      totalItems: zoneBucket?.totalItems ?? zoneMapItems.length,
      typeHighlights: this.buildTypeHighlightsFromCounts(zoneBucket?.countsByType ?? [], 5),
      map: this.buildMap(park, zoneMapItems),
      items: zoneCardItems.map((item: ParkItem) => mapParkItemToCardViewModel(
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
  private readonly mapItems: Signal<ParkItem[]> = computed(() => this.screenStateStore.data()?.mapItems ?? []);
  private readonly itemsPage: Signal<PagedResult<ParkItem>> = computed(() => this.screenStateStore.data()?.itemsPage ?? EMPTY_ZONE_ITEMS_PAGE);
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
  private readonly zoneMapItems: Signal<ParkItem[]> = computed(() => {
    const zoneId: string | null = this.selectedZoneIdSignal();

    if (!zoneId) {
      return [];
    }

    return this.mapItems()
      .filter((item: ParkItem) => item.isVisible !== false)
      .filter((item: ParkItem) => item.zoneId === zoneId)
      .sort((left: ParkItem, right: ParkItem) => left.name.localeCompare(right.name));
  });
  private readonly zoneCardItems: Signal<ParkItem[]> = computed(() => this.itemsPage().items);

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

  setSelectedZone(zoneId: string | null, routeParkId: string | null = null): void {
    const previousZoneId: string | null = this.selectedZoneIdSignal();
    this.selectedZoneIdSignal.set(zoneId);
    const currentParkId: string | null | undefined = this.park()?.id;

    if (previousZoneId !== zoneId && currentParkId && (!routeParkId || routeParkId === currentParkId)) {
      this.reloadZoneItemsPage(currentParkId);
    }
  }

  loadData(parkId: string, currentLanguage: string): void {
    this.currentLanguageSignal.set(currentLanguage);

    const previousData: ParkZonesPageSourceData | undefined = this.screenStateStore.data();
    const requestedClosedFilter: ClosedEntityFilter = this.selectedClosedFilterSignal();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      summary: this.parksApiService.getParkDetailSummary(parkId, { ...anonymousHttpOptions(), closedFilter: requestedClosedFilter }),
      explorer: this.parksApiService.getParkExplorer(parkId, { ...anonymousHttpOptions(), closedFilter: requestedClosedFilter }),
      mapItems: this.parksApiService.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter: requestedClosedFilter }),
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId, anonymousHttpOptions()).pipe(catchError(() => of([] as ParkZone[]))),
      itemsPage: this.parkItemsApiService.getParkItemsByParkIdPage(
        parkId,
        1,
        ZONE_ITEMS_PREVIEW_SIZE,
        { zoneId: this.selectedZoneIdSignal(), closedFilter: requestedClosedFilter },
        anonymousHttpOptions())
    }).pipe(
      switchMap((data: { summary: ParkDetailSummary; explorer: ParkExplorer; mapItems: ParkMapItems; zones: ParkZone[]; itemsPage: PagedResult<ParkItem> }) => {
        const effectiveClosedFilter: ClosedEntityFilter = this.normalizeClosedFilterForPark(data.summary.park);

        if (effectiveClosedFilter === requestedClosedFilter) {
          return of(data);
        }

        return forkJoin({
          summary: this.parksApiService.getParkDetailSummary(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter }),
          explorer: this.parksApiService.getParkExplorer(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter }),
          mapItems: this.parksApiService.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter }),
          zones: of(data.zones),
          itemsPage: this.parkItemsApiService.getParkItemsByParkIdPage(
            parkId,
            1,
            ZONE_ITEMS_PREVIEW_SIZE,
            { zoneId: this.selectedZoneIdSignal(), closedFilter: effectiveClosedFilter },
            anonymousHttpOptions())
        });
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (data: { summary: ParkDetailSummary; explorer: ParkExplorer; mapItems: ParkMapItems; zones: ParkZone[]; itemsPage: PagedResult<ParkItem> }) => {
        this.screenStateStore.setReady({
          park: data.summary.park,
          parkImageId: resolveParkSummarySocialImageId(data.summary),
          explorer: data.explorer,
          zones: data.zones,
          mapItems: mapParkMapItemsToParkItems(data.mapItems),
          itemsPage: data.itemsPage
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park zones page', error);
        this.screenStateStore.setError('parks.zones.errorMessage', previousData);
      }
    });
  }

  private reloadZoneItemsPage(parkId: string): void {
    if (!this.screenStateStore.data()) {
      return;
    }

    this.parkItemsApiService.getParkItemsByParkIdPage(
      parkId,
      1,
      ZONE_ITEMS_PREVIEW_SIZE,
      { zoneId: this.selectedZoneIdSignal(), closedFilter: this.selectedClosedFilterSignal() },
      anonymousHttpOptions()
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (itemsPage: PagedResult<ParkItem>) => {
        const currentData: ParkZonesPageSourceData | undefined = this.screenStateStore.data();
        if (!currentData || currentData.park.id !== parkId) {
          return;
        }

        this.screenStateStore.setReady({
          ...currentData,
          itemsPage
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park zone items page', error);
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

  private normalizeClosedFilterForPark(park: Park | null | undefined): ClosedEntityFilter {
    const effectiveClosedFilter: ClosedEntityFilter = resolvePublicParkItemsClosedFilter(park, this.selectedClosedFilterSignal());

    if (effectiveClosedFilter !== this.selectedClosedFilterSignal()) {
      this.selectedClosedFilterSignal.set(effectiveClosedFilter);
    }

    return effectiveClosedFilter;
  }
}

function mapParkMapItemsToParkItems(mapItems: ParkMapItems): ParkItem[] {
  const parkId: string = mapItems.park.id ?? '';
  const locatedItems: ParkItem[] = mapItems.items.map((item: ParkMapItem) => mapParkMapItemToParkItem(parkId, item, item.latitude, item.longitude));
  const unlocatedItems: ParkItem[] = (mapItems.unlocatedItems ?? []).map((item: ParkMapUnlocatedItem) => mapParkMapItemToParkItem(parkId, item, null, null));

  return locatedItems.concat(unlocatedItems);
}

function mapParkMapItemToParkItem(
  parkId: string,
  item: ParkMapItem | ParkMapUnlocatedItem,
  latitude: number | null,
  longitude: number | null
): ParkItem {
  return {
    id: item.id,
    parkId,
    zoneId: item.zoneId ?? null,
    name: item.name,
    category: item.category as ParkItem['category'],
    type: item.type as ParkItem['type'],
    subtype: item.subtype ?? null,
    latitude,
    longitude,
    descriptions: item.descriptions ?? [],
    attractionDetails: item.attractionDetails ?? null,
    attractionLocations: null,
    isVisible: true,
    adminReviewStatus: 'Validated'
  };
}
