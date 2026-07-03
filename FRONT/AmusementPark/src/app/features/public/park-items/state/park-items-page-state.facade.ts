import {
  Injectable,
  Signal,
  computed,
  signal,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';
import { PagedResult, DEFAULT_PAGINATION } from '@shared/models/contracts';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerBucket } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkMapItem, ParkMapItems, ParkMapUnlocatedItem } from '@app/models/parks/park-map-items';
import { ParkZone } from '@app/models/parks/park-zone';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { ParkItemsByParkIdFilters } from '@data-access/park-items/park-items-api-endpoints';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { buildTranslationOptions } from '@shared/utils/display/display-options';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveParkSocialImageId } from '@shared/utils/images/park-social-image.helpers';
import { resolvePublicParkItemsClosedFilter } from '@shared/utils/parks/public-park-items-closed-filter.helper';
import { mapParkItemToCardViewModel } from '../mappers/park-item-card.mapper';
import {
  mapParkExplorerBucketToZoneCardViewModel,
  mapParkItemsPageViewModel
} from '../mappers/park-items-page-view.mapper';
import { mapParkItemsZoneFocusViewModel } from '../mappers/park-items-zone-focus.mapper';
import { ParkItemCardViewModel } from '../models/park-item-card.model';
import { SelectOption } from '../models/select-option.model';
import { ParkItemsPageViewModel, ParkItemZoneCardViewModel } from '../models/park-items-page-view.model';
import { ParkItemsZoneFocusViewModel } from '../models/park-items-zone-focus.model';

import {
  PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT,
  ParkItemsPageStateImagesApiServicePort,
  PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT,
  ParkItemsPageStateManufacturersApiServicePort,
  PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT,
  ParkItemsPageStateParkItemsApiServicePort,
  PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT,
  ParkItemsPageStateParksApiServicePort,
  PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT,
  ParkItemsPageStateParkZonesApiServicePort
} from './park-items-page-state-data.ports';

const EMPTY_PARK_ITEMS_PAGE: PagedResult<ParkItem> = {
  items: [],
  pagination: DEFAULT_PAGINATION
};

interface ParkItemsPageSourceData {
  park: Park;
  explorer: ParkExplorer;
  itemsPage: PagedResult<ParkItem>;
  mapItems: ParkItem[];
  manufacturers: AttractionManufacturer[];
  zones: ParkZone[];
  parkPhotos: ImageDto[];
}

export interface ParkItemsPageFilters {
  searchTerm: string;
  selectedCategory: string | null;
  selectedType: string | null;
  selectedZoneId: string | null;
  selectedClosedFilter: ClosedEntityFilter;
  currentPage: number;
  pageSize: number;
}

@Injectable()
export class ParkItemsPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemsPageSourceData>();
  private readonly currentLanguageSignal = signal('en');
  private readonly searchTermSignal = signal('');
  private readonly selectedCategorySignal = signal<string | null>(null);
  private readonly selectedTypeSignal = signal<string | null>(null);
  private readonly selectedZoneIdSignal = signal<string | null>(null);
  private readonly selectedClosedFilterSignal = signal<ClosedEntityFilter>(DEFAULT_CLOSED_ENTITY_FILTER);
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(12);
  private readonly currentParkIdSignal = signal<string | null>(null);

  public readonly state = this.screenStateStore.state;
  public readonly pageView: Signal<ParkItemsPageViewModel | null> = computed(() => {
    return mapParkItemsPageViewModel(
      this.park(),
      this.explorer(),
      this.currentLanguageSignal(),
      this.totalResults(),
      this.activeZoneLabel(),
      this.hasZones()
    );
  });
  public readonly categoryOptions: Signal<SelectOption[]> = computed(() => {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allCategories', value: null }];
    const categoryValues: string[] = (this.explorer()?.overview.countsByCategory ?? [])
      .filter((count) => count.count > 0)
      .map((count) => count.key)
      .sort((left: string, right: string) => left.localeCompare(right));

    return base.concat(buildTranslationOptions(categoryValues, getParkItemCategoryTranslationKey));
  });
  public readonly typeOptions: Signal<SelectOption[]> = computed(() => {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allTypes', value: null }];
    const typeValues: string[] = (this.explorer()?.overview.countsByType ?? [])
      .filter((count) => count.count > 0)
      .map((count) => count.key)
      .sort((left: string, right: string) => left.localeCompare(right));

    return base.concat(buildTranslationOptions(typeValues, getParkItemTypeTranslationKey));
  });
  public readonly zoneOptions: Signal<SelectOption[]> = computed(() => {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allZones', value: null }];
    const values: SelectOption[] = Object.entries(this.zonesById())
      .map(([value, label]: [string, string]) => ({ value, label }))
      .sort((left: SelectOption, right: SelectOption) => (left.label ?? '').localeCompare(right.label ?? ''));

    return base.concat(values);
  });
  public readonly closedFilterOptions: Signal<SelectOption[]> = computed(() => [
    { labelKey: 'parkItems.filters.openOnly', value: 'openOnly' },
    { labelKey: 'parkItems.filters.withClosed', value: 'all' },
    { labelKey: 'parkItems.filters.closedOnly', value: 'closedOnly' }
  ]);
  public readonly zoneCards: Signal<ParkItemZoneCardViewModel[]> = computed(() => {
    const explorer: ParkExplorer | null = this.explorer();

    if (!explorer) {
      return [];
    }

    return [...explorer.zones]
      .filter((bucket: ParkExplorerBucket) => bucket.totalItems > 0)
      .map((bucket: ParkExplorerBucket) => mapParkExplorerBucketToZoneCardViewModel(
        bucket,
        this.currentLanguageSignal(),
        this.selectedZoneIdSignal()
      ));
  });
  public readonly zoneFocus: Signal<ParkItemsZoneFocusViewModel | null> = computed(() => {
    return mapParkItemsZoneFocusViewModel(
      this.park(),
      this.explorer(),
      this.zones(),
      this.filteredMapItems(),
      this.typeFilterScopeMapItems(),
      this.selectedZoneIdSignal(),
      this.screenStateStore.data()?.parkPhotos ?? [],
      this.currentLanguageSignal()
    );
  });
  public readonly pagedItems: Signal<ParkItemCardViewModel[]> = computed(() => {
    const park: Park | null = this.park();

    return this.itemsPage().items
      .map((item: ParkItem) => mapParkItemToCardViewModel(
        item,
        park,
        this.currentLanguageSignal(),
        this.resolveManufacturerName(item),
        this.resolveZoneName(item),
        this.textTruncator,
        this.measurementPreferenceService.preferredSystem(),
        this.measurementConversionService,
        this.resolveItemImageUrl(item),
        this.resolveItemImageSrcSet(item)
      ));
  });
  public readonly totalResults = computed(() => this.itemsPage().pagination.totalItems);
  public readonly rangeStart = computed(() => {
    if (this.totalResults() === 0) {
      return 0;
    }

    return ((this.currentPageSignal() - 1) * this.pageSizeSignal()) + 1;
  });
  public readonly rangeEnd = computed(() => {
    if (this.totalResults() === 0 || this.itemsPage().items.length === 0) {
      return 0;
    }

    return Math.min(this.rangeStart() + this.itemsPage().items.length - 1, this.totalResults());
  });
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly selectedZoneId = this.selectedZoneIdSignal.asReadonly();
  public readonly selectedClosedFilter = this.selectedClosedFilterSignal.asReadonly();
  public readonly searchTerm = this.searchTermSignal.asReadonly();
  public readonly selectedCategory = this.selectedCategorySignal.asReadonly();
  public readonly selectedType = this.selectedTypeSignal.asReadonly();
  public readonly hasZones = computed(() => this.zoneCards().length > 0);
  public readonly parkImageId: Signal<string | null> = computed(() => resolveParkSocialImageId(this.screenStateStore.data()?.parkPhotos ?? []));

  private readonly park: Signal<Park | null> = computed(() => this.screenStateStore.data()?.park ?? null);
  private readonly explorer: Signal<ParkExplorer | null> = computed(() => this.screenStateStore.data()?.explorer ?? null);
  private readonly itemsPage: Signal<PagedResult<ParkItem>> = computed(() => this.screenStateStore.data()?.itemsPage ?? EMPTY_PARK_ITEMS_PAGE);
  private readonly mapItems: Signal<ParkItem[]> = computed(() => this.screenStateStore.data()?.mapItems ?? []);
  private readonly zones: Signal<ParkZone[]> = computed(() => this.screenStateStore.data()?.zones ?? []);
  private readonly manufacturersById = computed(() => {
    const manufacturers: AttractionManufacturer[] = this.screenStateStore.data()?.manufacturers ?? [];

    return manufacturers.reduce((accumulator: Record<string, string>, current: AttractionManufacturer) => {
      if (current.id && current.name) {
        accumulator[current.id] = current.name;
      }

      return accumulator;
    }, {} as Record<string, string>);
  });
  private readonly zonesById = computed(() => {
    const zones: ParkZone[] = this.screenStateStore.data()?.zones ?? [];
    const currentLanguage: string = this.currentLanguageSignal();

    return zones.reduce((accumulator: Record<string, string>, current: ParkZone) => {
      const localizedName: string | undefined = resolveLocalizedValue(current.names, currentLanguage);

      if (current.id) {
        accumulator[current.id] = localizedName ?? current.name ?? current.id;
      }

      return accumulator;
    }, {} as Record<string, string>);
  });
  private readonly activeZoneLabel = computed(() => {
    const selectedZoneId: string | null = this.selectedZoneIdSignal();

    if (!selectedZoneId) {
      return null;
    }

    return this.zonesById()[selectedZoneId] ?? null;
  });
  private readonly filteredMapItems: Signal<ParkItem[]> = computed(() => this.filterMapItems(true));
  private readonly typeFilterScopeMapItems: Signal<ParkItem[]> = computed(() => this.filterMapItems(false));

  constructor(
    @Inject(PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: ParkItemsPageStateParksApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: ParkItemsPageStateParkItemsApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: ParkItemsPageStateImagesApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT) private readonly manufacturersApiService: ParkItemsPageStateManufacturersApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: ParkItemsPageStateParkZonesApiServicePort,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly measurementPreferenceService: MeasurementPreferenceService,
    private readonly measurementConversionService: MeasurementConversionService,
    private readonly destroyRef: DestroyRef,
    private readonly ssrRuntimeService: SsrRuntimeService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
    this.normalizeFilters();
  }

  setFilters(filters: ParkItemsPageFilters): void {
    const previousItemsRequestKey: string = this.buildItemsRequestKey();
    const previousClosedFilter: ClosedEntityFilter = this.selectedClosedFilterSignal();

    this.searchTermSignal.set(filters.searchTerm);
    this.selectedCategorySignal.set(filters.selectedCategory);
    this.selectedTypeSignal.set(filters.selectedType);
    this.selectedZoneIdSignal.set(filters.selectedZoneId);
    this.selectedClosedFilterSignal.set(filters.selectedClosedFilter);
    this.currentPageSignal.set(filters.currentPage);
    this.pageSizeSignal.set(filters.pageSize);
    this.normalizeFilters();

    const nextItemsRequestKey: string = this.buildItemsRequestKey();
    if (!this.currentParkIdSignal() || previousItemsRequestKey === nextItemsRequestKey) {
      return;
    }

    if (previousClosedFilter !== filters.selectedClosedFilter) {
      this.reloadCurrentData();
      return;
    }

    this.reloadItemsPage();
  }

  loadData(parkId: string, currentLanguage: string): void {
    this.currentParkIdSignal.set(parkId);
    this.currentLanguageSignal.set(currentLanguage);
    this.reloadCurrentData();
  }

  private reloadCurrentData(): void {
    const parkId: string | null = this.currentParkIdSignal();

    if (!parkId) {
      return;
    }

    const closedFilter: ClosedEntityFilter = this.selectedClosedFilterSignal();
    const itemsFilters: ParkItemsByParkIdFilters = this.buildItemsRequestFilters();
    const previousData: ParkItemsPageSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    const useMinimalSsrData: boolean = this.ssrRuntimeService.shouldUseMinimalPublicData();

    forkJoin({
      park: this.parksApiService.getParkById(parkId, anonymousHttpOptions()),
      explorer: this.parksApiService.getParkExplorer(parkId, { ...anonymousHttpOptions(), closedFilter }),
      mapItems: this.parksApiService.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter }),
      itemsPage: this.parkItemsApiService.getParkItemsByParkIdPage(
        parkId,
        this.currentPageSignal(),
        this.pageSizeSignal(),
        itemsFilters,
        anonymousHttpOptions()),
      manufacturers: useMinimalSsrData
        ? of([] as AttractionManufacturer[])
        : this.manufacturersApiService.getAttractionManufacturers().pipe(catchError(() => of([] as AttractionManufacturer[]))),
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId, anonymousHttpOptions()).pipe(catchError(() => of([] as ParkZone[]))),
      parkPhotos: useMinimalSsrData
        ? of([] as ImageDto[])
        : this.imagesApiService.getImages(ImageOwnerType.PARK, parkId, ImageCategory.PARK, 1, 24, anonymousHttpOptions()).pipe(catchError(() => of([] as ImageDto[])))
    }).pipe(
      switchMap((data: {
        park: Park;
        explorer: ParkExplorer;
        mapItems: ParkMapItems;
        itemsPage: PagedResult<ParkItem>;
        manufacturers: AttractionManufacturer[];
        zones: ParkZone[];
        parkPhotos: ImageDto[];
      }) => {
        const effectiveClosedFilter: ClosedEntityFilter = this.normalizeClosedFilterForPark(data.park);

        if (effectiveClosedFilter === closedFilter) {
          return of(data);
        }

        const effectiveItemsFilters: ParkItemsByParkIdFilters = this.buildItemsRequestFilters();

        return forkJoin({
          park: of(data.park),
          explorer: this.parksApiService.getParkExplorer(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter }),
          mapItems: this.parksApiService.getParkMapItems(parkId, { ...anonymousHttpOptions(), closedFilter: effectiveClosedFilter }),
          itemsPage: this.parkItemsApiService.getParkItemsByParkIdPage(
            parkId,
            this.currentPageSignal(),
            this.pageSizeSignal(),
            effectiveItemsFilters,
            anonymousHttpOptions()),
          manufacturers: of(data.manufacturers),
          zones: of(data.zones),
          parkPhotos: of(data.parkPhotos)
        });
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: ({
        park,
        explorer,
        mapItems,
        itemsPage,
        manufacturers,
        zones,
        parkPhotos
      }: {
        park: Park;
        explorer: ParkExplorer;
        mapItems: ParkMapItems;
        itemsPage: PagedResult<ParkItem>;
        manufacturers: AttractionManufacturer[];
        zones: ParkZone[];
        parkPhotos: ImageDto[];
      }) => {
        this.screenStateStore.setReady({
          park,
          explorer,
          itemsPage,
          mapItems: mapParkMapItemsToParkItems(mapItems),
          manufacturers,
          zones,
          parkPhotos
        });
        if (this.normalizeFilters()) {
          this.reloadItemsPage();
        }
      },
      error: (error: unknown) => {
        console.error('Error loading park items page', error);
        this.screenStateStore.setError('parkItems.list.errorMessage', previousData);
      }
    });
  }

  private reloadItemsPage(): void {
    const parkId: string | null = this.currentParkIdSignal();
    const previousData: ParkItemsPageSourceData | undefined = this.screenStateStore.data();

    if (!parkId || !previousData) {
      return;
    }

    this.parkItemsApiService.getParkItemsByParkIdPage(
      parkId,
      this.currentPageSignal(),
      this.pageSizeSignal(),
      this.buildItemsRequestFilters(),
      anonymousHttpOptions()
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (itemsPage: PagedResult<ParkItem>) => {
        this.screenStateStore.setReady({
          ...previousData,
          itemsPage
        });
        if (this.normalizeFilters()) {
          this.reloadItemsPage();
        }
      },
      error: (error: unknown) => {
        console.error('Error loading park items page', error);
        this.screenStateStore.setError('parkItems.list.errorMessage', previousData);
      }
    });
  }

  private filterMapItems(includeTypeFilter: boolean): ParkItem[] {
    const mapItems: ParkItem[] = this.mapItems();

    if (mapItems.length === 0) {
      return [];
    }

    const normalizedSearchTerm: string = this.searchTermSignal().trim().toLowerCase();
    const selectedCategory: string | null = this.selectedCategorySignal();
    const selectedType: string | null = includeTypeFilter ? this.selectedTypeSignal() : null;
    const selectedZoneId: string | null = this.selectedZoneIdSignal();
    const manufacturersById: Record<string, string> = this.manufacturersById();
    const zonesById: Record<string, string> = this.zonesById();

    return mapItems
      .filter((item: ParkItem) => {
        if (selectedCategory && item.category !== selectedCategory) {
          return false;
        }

        if (selectedType && item.type !== selectedType) {
          return false;
        }

        if (selectedZoneId && item.zoneId !== selectedZoneId) {
          return false;
        }

        if (!normalizedSearchTerm) {
          return true;
        }

        const manufacturerName: string | null = item.attractionDetails?.manufacturerId
          ? manufacturersById[item.attractionDetails.manufacturerId] ?? null
          : null;
        const zoneName: string | null = item.zoneId ? zonesById[item.zoneId] ?? null : null;
        const descriptions: string[] = (item.descriptions ?? [])
          .map(description => description.value)
          .filter((value: string | null | undefined): value is string => !!value);
        const haystack: string = [
          item.name,
          item.subtype,
          manufacturerName,
          item.attractionDetails?.model,
          item.attractionDetails?.status,
          zoneName,
          ...descriptions
        ]
          .filter((value: string | null | undefined): value is string => !!value)
          .join(' ')
          .toLowerCase();

        return haystack.includes(normalizedSearchTerm);
      })
      .sort((left: ParkItem, right: ParkItem) => left.name.localeCompare(right.name));
  }

  private buildItemsRequestFilters(): ParkItemsByParkIdFilters {
    return {
      closedFilter: this.selectedClosedFilterSignal(),
      search: this.searchTermSignal(),
      category: this.selectedCategorySignal() as ParkItemsByParkIdFilters['category'],
      type: this.selectedTypeSignal() as ParkItemsByParkIdFilters['type'],
      zoneId: this.selectedZoneIdSignal()
    };
  }

  private buildItemsRequestKey(): string {
    const filters: ParkItemsByParkIdFilters = this.buildItemsRequestFilters();

    return [
      this.currentPageSignal(),
      this.pageSizeSignal(),
      filters.closedFilter ?? DEFAULT_CLOSED_ENTITY_FILTER,
      filters.search?.trim() ?? '',
      filters.category ?? '',
      filters.type ?? '',
      filters.zoneId?.trim() ?? ''
    ].join('|');
  }

  private resolveManufacturerName(item: ParkItem): string | null {
    const manufacturerId: string | null | undefined = item.attractionDetails?.manufacturerId;

    if (!manufacturerId) {
      return null;
    }

    return this.manufacturersById()[manufacturerId] ?? null;
  }

  private resolveZoneName(item: ParkItem): string | null {
    if (!item.zoneId) {
      return null;
    }

    return this.zonesById()[item.zoneId] ?? null;
  }

  private resolveItemImageUrl(item: ParkItem): string | null {
    const imageId: string | null = normalizeOptionalImageId(item.mainImageId);

    return imageId ? this.imagesApiService.buildImageUrl(imageId, { width: 640 }) : null;
  }

  private resolveItemImageSrcSet(item: ParkItem): string | null {
    const imageId: string | null = normalizeOptionalImageId(item.mainImageId);

    return imageId ? this.imagesApiService.buildImageSrcSet(imageId, [320, 480, 640, 800]) : null;
  }

  private normalizeFilters(): boolean {
    const previousItemsRequestKey: string = this.buildItemsRequestKey();
    const selectedZoneId: string | null = this.selectedZoneIdSignal();
    const sourceData: ParkItemsPageSourceData | undefined = this.screenStateStore.data();
    const availableZoneIds: string[] = Object.keys(this.zonesById());

    this.normalizeClosedFilterForPark(sourceData?.park ?? null);

    if (selectedZoneId && sourceData && (availableZoneIds.length === 0 || !this.zonesById()[selectedZoneId])) {
      this.selectedZoneIdSignal.set(null);
    }

    const totalPages: number = this.itemsPage().pagination.totalPages;
    const currentPage: number = this.currentPageSignal();

    if (totalPages > 0 && currentPage > totalPages) {
      this.currentPageSignal.set(1);
    }

    return previousItemsRequestKey !== this.buildItemsRequestKey();
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

function normalizeOptionalImageId(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
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
