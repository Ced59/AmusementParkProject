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

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerBucket } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { resolveParkItemDescription } from '@shared/utils/display/park-item-presentation.helpers';
import { buildTranslationOptions } from '@shared/utils/display/display-options';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
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
interface ParkItemsPageSourceData {
  park: Park;
  explorer: ParkExplorer;
  allItems: ParkItem[];
  manufacturers: AttractionManufacturer[];
  zones: ParkZone[];
  parkPhotos: ImageDto[];
}

export interface ParkItemsPageFilters {
  searchTerm: string;
  selectedCategory: string | null;
  selectedType: string | null;
  selectedZoneId: string | null;
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
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(12);

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
    const categoryValues: string[] = Array.from(new Set(this.allItems().map((item: ParkItem) => item.category)))
      .sort((left: string, right: string) => left.localeCompare(right));

    return base.concat(buildTranslationOptions(categoryValues, getParkItemCategoryTranslationKey));
  });
  public readonly typeOptions: Signal<SelectOption[]> = computed(() => {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allTypes', value: null }];
    const typeValues: string[] = Array.from(new Set(this.allItems().map((item: ParkItem) => item.type)))
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
      this.filteredItems(),
      this.typeFilterScopeItems(),
      this.selectedZoneIdSignal(),
      this.screenStateStore.data()?.parkPhotos ?? [],
      this.currentLanguageSignal()
    );
  });
  public readonly pagedItems: Signal<ParkItemCardViewModel[]> = computed(() => {
    const park: Park | null = this.park();
    const firstIndex: number = (this.currentPageSignal() - 1) * this.pageSizeSignal();

    return this.filteredItems()
      .slice(firstIndex, firstIndex + this.pageSizeSignal())
      .map((item: ParkItem) => mapParkItemToCardViewModel(
        item,
        park,
        this.currentLanguageSignal(),
        this.resolveManufacturerName(item),
        this.resolveZoneName(item)
      ));
  });
  public readonly totalResults = computed(() => this.filteredItems().length);
  public readonly rangeStart = computed(() => {
    if (this.totalResults() === 0) {
      return 0;
    }

    return ((this.currentPageSignal() - 1) * this.pageSizeSignal()) + 1;
  });
  public readonly rangeEnd = computed(() => Math.min(this.currentPageSignal() * this.pageSizeSignal(), this.totalResults()));
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly selectedZoneId = this.selectedZoneIdSignal.asReadonly();
  public readonly searchTerm = this.searchTermSignal.asReadonly();
  public readonly selectedCategory = this.selectedCategorySignal.asReadonly();
  public readonly selectedType = this.selectedTypeSignal.asReadonly();

  private readonly park: Signal<Park | null> = computed(() => this.screenStateStore.data()?.park ?? null);
  private readonly explorer: Signal<ParkExplorer | null> = computed(() => this.screenStateStore.data()?.explorer ?? null);
  private readonly allItems: Signal<ParkItem[]> = computed(() => this.screenStateStore.data()?.allItems ?? []);
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
  private readonly hasZones = computed(() => this.zoneCards().length > 0);
  private readonly filteredItems: Signal<ParkItem[]> = computed(() => this.filterItems(true));
  private readonly typeFilterScopeItems: Signal<ParkItem[]> = computed(() => this.filterItems(false));

  constructor(
    @Inject(PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: ParkItemsPageStateParksApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: ParkItemsPageStateParkItemsApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: ParkItemsPageStateImagesApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT) private readonly manufacturersApiService: ParkItemsPageStateManufacturersApiServicePort,
    @Inject(PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT) private readonly parkZonesApiService: ParkItemsPageStateParkZonesApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
    this.normalizeFilters();
  }

  setFilters(filters: ParkItemsPageFilters): void {
    this.searchTermSignal.set(filters.searchTerm);
    this.selectedCategorySignal.set(filters.selectedCategory);
    this.selectedTypeSignal.set(filters.selectedType);
    this.selectedZoneIdSignal.set(filters.selectedZoneId);
    this.currentPageSignal.set(filters.currentPage);
    this.pageSizeSignal.set(filters.pageSize);
    this.normalizeFilters();
  }

  loadData(parkId: string, currentLanguage: string): void {
    this.currentLanguageSignal.set(currentLanguage);

    const previousData: ParkItemsPageSourceData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      park: this.parksApiService.getParkById(parkId),
      explorer: this.parksApiService.getParkExplorer(parkId),
      items: this.parkItemsApiService.getParkItemsByParkId(parkId),
      manufacturers: this.manufacturersApiService.getAttractionManufacturers().pipe(catchError(() => of([] as AttractionManufacturer[]))),
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId).pipe(catchError(() => of([] as ParkZone[]))),
      parkPhotos: this.imagesApiService.getImages(ImageOwnerType.PARK, parkId, ImageCategory.PARK, 1, 24).pipe(catchError(() => of([] as ImageDto[])))
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({
        park,
        explorer,
        items,
        manufacturers,
        zones,
        parkPhotos
      }: {
        park: Park;
        explorer: ParkExplorer;
        items: ParkItem[];
        manufacturers: AttractionManufacturer[];
        zones: ParkZone[];
        parkPhotos: ImageDto[];
      }) => {
        this.screenStateStore.setReady({
          park,
          explorer,
          allItems: items,
          manufacturers,
          zones,
          parkPhotos
        });
        this.normalizeFilters();
      },
      error: (error: unknown) => {
        console.error('Error loading park items page', error);
        this.screenStateStore.setError('parkItems.list.errorMessage', previousData);
      }
    });
  }

  private filterItems(includeTypeFilter: boolean): ParkItem[] {
    const allItems: ParkItem[] = this.allItems();

    if (allItems.length === 0) {
      return [];
    }

    const normalizedSearchTerm: string = this.searchTermSignal().trim().toLowerCase();
    const selectedCategory: string | null = this.selectedCategorySignal();
    const selectedType: string | null = includeTypeFilter ? this.selectedTypeSignal() : null;
    const selectedZoneId: string | null = this.selectedZoneIdSignal();
    const currentLanguage: string = this.currentLanguageSignal();
    const manufacturersById: Record<string, string> = this.manufacturersById();
    const zonesById: Record<string, string> = this.zonesById();

    return allItems
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
        const description: string = resolveParkItemDescription(item, currentLanguage) ?? '';
        const haystack: string = [
          item.name,
          item.subtype,
          description,
          manufacturerName,
          item.attractionDetails?.model,
          item.attractionDetails?.status,
          zoneName
        ]
          .filter((value: string | null | undefined): value is string => !!value)
          .join(' ')
          .toLowerCase();

        return haystack.includes(normalizedSearchTerm);
      })
      .sort((left: ParkItem, right: ParkItem) => left.name.localeCompare(right.name));
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

  private normalizeFilters(): void {
    const selectedZoneId: string | null = this.selectedZoneIdSignal();
    const sourceData: ParkItemsPageSourceData | undefined = this.screenStateStore.data();
    const availableZoneIds: string[] = Object.keys(this.zonesById());

    if (selectedZoneId && sourceData && (availableZoneIds.length === 0 || !this.zonesById()[selectedZoneId])) {
      this.selectedZoneIdSignal.set(null);
    }

    const filteredItems: ParkItem[] = this.filteredItems();
    const currentPage: number = this.currentPageSignal();
    const pageSize: number = this.pageSizeSignal();
    const firstIndex: number = (currentPage - 1) * pageSize;

    if (filteredItems.length > 0 && firstIndex >= filteredItems.length) {
      this.currentPageSignal.set(1);
    }
  }
}
