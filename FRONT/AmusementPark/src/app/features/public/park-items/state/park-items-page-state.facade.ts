import { Injectable, Signal, computed, signal } from '@angular/core';
import { catchError, forkJoin, of } from 'rxjs';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { resolveParkItemDescription } from '@app/commons/park-item-presentation.utils';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { Park } from '@app/models/parks/park';
import { ParkExplorer, ParkExplorerBucket } from '@app/models/parks/park-explorer';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { getParkItemCategoryTranslationKey, getParkItemTypeTranslationKey } from '@shared/utils/display/display-label.helpers';
import { buildTranslationOptions } from '@shared/utils/display/display-options';

export interface SelectOption {
  labelKey?: string;
  label?: string;
  value: string | null;
}

interface ParkItemsPageViewModel {
  park: Park;
  explorer: ParkExplorer;
  allItems: ParkItem[];
  manufacturers: AttractionManufacturer[];
  zones: ParkZone[];
}

interface ParkItemsPageFilters {
  searchTerm: string;
  selectedCategory: string | null;
  selectedType: string | null;
  selectedZoneId: string | null;
  currentPage: number;
  pageSize: number;
}

@Injectable()
export class ParkItemsPageStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemsPageViewModel>();
  private readonly currentLangSignal = signal('en');
  private readonly searchTermSignal = signal('');
  private readonly selectedCategorySignal = signal<string | null>(null);
  private readonly selectedTypeSignal = signal<string | null>(null);
  private readonly selectedZoneIdSignal = signal<string | null>(null);
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(12);

  public readonly state = this.screenStateStore.state;
  public readonly park: Signal<Park | null> = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly explorer: Signal<ParkExplorer | null> = computed(() => this.screenStateStore.data()?.explorer ?? null);
  public readonly allItems: Signal<ParkItem[]> = computed(() => this.screenStateStore.data()?.allItems ?? []);
  public readonly manufacturersById = computed(() => {
    const manufacturers: AttractionManufacturer[] = this.screenStateStore.data()?.manufacturers ?? [];
    return manufacturers.reduce((accumulator: Record<string, string>, current: AttractionManufacturer) => {
      if (current.id && current.name) {
        accumulator[current.id] = current.name;
      }
      return accumulator;
    }, {} as Record<string, string>);
  });
  public readonly zonesById = computed(() => {
    const zones: ParkZone[] = this.screenStateStore.data()?.zones ?? [];
    const currentLang: string = this.currentLangSignal();
    return zones.reduce((accumulator: Record<string, string>, current: ParkZone) => {
      const localizedName: string | undefined = resolveLocalizedValue(current.names, currentLang);
      if (current.id) {
        accumulator[current.id] = localizedName ?? current.name ?? current.id;
      }
      return accumulator;
    }, {} as Record<string, string>);
  });
  public readonly bucketCards: Signal<ParkExplorerBucket[]> = computed(() => {
    const explorer: ParkExplorer | null = this.explorer();

    if (!explorer) {
      return [];
    }

    return [...explorer.zones].filter((bucket: ParkExplorerBucket) => bucket.totalItems > 0);
  });
  public readonly hasZones = computed(() => this.bucketCards().length > 0);
  public readonly activeZoneLabel = computed(() => {
    const selectedZoneId: string | null = this.selectedZoneIdSignal();

    if (!selectedZoneId) {
      return null;
    }

    return this.zonesById()[selectedZoneId] ?? null;
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
  public readonly topTypeHighlights: Signal<Array<{ key: string; count: number }>> = computed(() => {
    const explorer: ParkExplorer | null = this.explorer();

    if (!explorer) {
      return [];
    }

    return [...explorer.overview.countsByType]
      .sort((left, right) => right.count - left.count)
      .slice(0, 4);
  });
  public readonly filteredItems: Signal<ParkItem[]> = computed(() => {
    const allItems: ParkItem[] = this.allItems();

    if (allItems.length === 0) {
      return [];
    }

    const normalizedSearchTerm: string = this.searchTermSignal().trim().toLowerCase();
    const selectedCategory: string | null = this.selectedCategorySignal();
    const selectedType: string | null = this.selectedTypeSignal();
    const selectedZoneId: string | null = this.selectedZoneIdSignal();
    const currentLang: string = this.currentLangSignal();
    const manufacturersById: Record<string, string> = this.manufacturersById();
    const zonesById: Record<string, string> = this.zonesById();

    return allItems.filter((item: ParkItem) => {
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
      const description: string = resolveParkItemDescription(item, currentLang) ?? '';
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
    }).sort((left: ParkItem, right: ParkItem) => left.name.localeCompare(right.name));
  });
  public readonly pagedItems: Signal<ParkItem[]> = computed(() => {
    const filteredItems: ParkItem[] = this.filteredItems();
    const firstIndex: number = (this.currentPageSignal() - 1) * this.pageSizeSignal();
    return filteredItems.slice(firstIndex, firstIndex + this.pageSizeSignal());
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

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly parkZonesApiService: ParkZonesApiService
  ) {
  }

  setCurrentLanguage(lang: string): void {
    this.currentLangSignal.set(lang);
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

  loadData(parkId: string, currentLang: string): void {
    this.currentLangSignal.set(currentLang);

    const previousData: ParkItemsPageViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      park: this.parksApiService.getParkById(parkId),
      explorer: this.parksApiService.getParkExplorer(parkId),
      items: this.parkItemsApiService.getParkItemsByParkId(parkId),
      manufacturers: this.manufacturersApiService.getAttractionManufacturers().pipe(catchError(() => of([] as AttractionManufacturer[]))),
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId).pipe(catchError(() => of([] as ParkZone[])))
    }).subscribe({
      next: ({
        park,
        explorer,
        items,
        manufacturers,
        zones
      }: {
        park: Park;
        explorer: ParkExplorer;
        items: ParkItem[];
        manufacturers: AttractionManufacturer[];
        zones: ParkZone[];
      }) => {
        this.screenStateStore.setReady({
          park,
          explorer,
          allItems: items,
          manufacturers,
          zones
        });
        this.normalizeFilters();
      },
      error: (error: unknown) => {
        console.error('Error loading park items page', error);
        this.screenStateStore.setError('parkItems.list.errorMessage', previousData);
      }
    });
  }

  getParkZoneDisplayName(bucket: ParkExplorerBucket): string {
    const localizedName: string | undefined = resolveLocalizedValue(bucket.names, this.currentLangSignal());
    return localizedName ?? bucket.name;
  }

  getZoneCardTypeHighlights(bucket: ParkExplorerBucket): Array<{ key: string; count: number }> {
    return [...bucket.countsByType]
      .sort((left, right) => right.count - left.count)
      .slice(0, 3);
  }

  getTypeKey(type: string): string {
    return getParkItemTypeTranslationKey(type);
  }

  getManufacturerName(item: ParkItem): string | null {
    const manufacturerId: string | null | undefined = item.attractionDetails?.manufacturerId;

    if (!manufacturerId) {
      return null;
    }

    return this.manufacturersById()[manufacturerId] ?? null;
  }

  getZoneName(item: ParkItem): string | null {
    if (!item.zoneId) {
      return null;
    }

    return this.zonesById()[item.zoneId] ?? null;
  }

  isZoneSelected(bucket: ParkExplorerBucket): boolean {
    return this.selectedZoneIdSignal() === (bucket.id ?? null);
  }

  private normalizeFilters(): void {
    const selectedZoneId: string | null = this.selectedZoneIdSignal();

    if (selectedZoneId && !this.zonesById()[selectedZoneId]) {
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
