import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { Subscription, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { Paginator } from 'primeng/paginator';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '../../../models/parks/park';
import { ParkExplorer, ParkExplorerBucket } from '../../../models/parks/park-explorer';
import { ParkItem } from '../../../models/parks/park-item';
import { ParkZone } from '../../../models/parks/park-zone';
import { AttractionManufacturer } from '../../../models/parks/attraction-manufacturer';
import { ViewState } from '../../../models/shared/view-state';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { TranslationService } from '../../../services/translation.service';
import { commitViewUpdate } from '../../../utils/change-detection.utils';
import { buildParkSlug } from '../../../commons/park-presentation.utils';
import { resolveParkItemDescription } from '../../../commons/park-item-presentation.utils';
import { resolveLocalizedValue } from '../../../commons/localized-item.utils';
import { PageStateComponent } from '../../shared/page-state/page-state.component';
import { ParkItemCardComponent } from '../park-item-card/park-item-card.component';

interface SelectOption {
  labelKey?: string;
  label?: string;
  value: string | null;
}

@Component({
  selector: 'app-park-items-page',
  templateUrl: './park-items-page.component.html',
  styleUrls: ['./park-items-page.component.scss'],
  imports: [
    NgFor,
    NgIf,
    FormsModule,
    InputText,
    Paginator,
    ButtonDirective,
    TranslateModule,
    PageStateComponent,
    ParkItemCardComponent
  ]
})
export class ParkItemsPageComponent implements OnInit, OnDestroy {
  park: Park | null = null;
  explorer: ParkExplorer | null = null;
  pageState: ViewState = ViewState.Loading;
  currentLang: string = 'en';

  allItems: ParkItem[] = [];
  filteredItems: ParkItem[] = [];
  pagedItems: ParkItem[] = [];

  manufacturersById: Record<string, string> = {};
  zonesById: Record<string, string> = {};

  searchTerm: string = '';
  selectedCategory: string | null = null;
  selectedType: string | null = null;
  selectedZoneId: string | null = null;
  pageSize: number = 12;
  currentPage: number = 1;

  private currentParkId: string | null = null;
  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly parksApiService: ParksApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly translationService: TranslationService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    if (this.route.parent) {
      this.subscriptions.add(this.route.parent.paramMap.subscribe((params: ParamMap) => {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.currentLang = params.get('lang') ?? 'en';
        });
      }));
    }

    this.subscriptions.add(this.translationService.languageChanged.subscribe((lang: string) => {
      commitViewUpdate(this.changeDetectorRef, () => {
        this.currentLang = lang;
      });
    }));

    this.subscriptions.add(this.route.paramMap.subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');
      if (!parkId) {
        commitViewUpdate(this.changeDetectorRef, () => {
          this.pageState = ViewState.Error;
        });
        return;
      }

      if (parkId !== this.currentParkId) {
        this.currentParkId = parkId;
        this.loadData(parkId);
      }
    }));

    this.subscriptions.add(this.route.queryParamMap.subscribe((queryParams) => {
      this.searchTerm = queryParams.get('search') ?? '';
      this.selectedCategory = queryParams.get('category');
      this.selectedType = queryParams.get('type');
      this.selectedZoneId = queryParams.get('zone');
      this.currentPage = Math.max(Number(queryParams.get('page') ?? '1') || 1, 1);
      this.pageSize = Math.max(Number(queryParams.get('size') ?? '12') || 12, 1);
      this.applyFilters();
    }));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get bucketCards(): ParkExplorerBucket[] {
    if (!this.explorer) {
      return [];
    }

    return [...this.explorer.zones].filter((bucket: ParkExplorerBucket) => bucket.totalItems > 0);
  }

  get hasZones(): boolean {
    return this.bucketCards.length > 0;
  }

  get activeZoneLabel(): string | null {
    if (!this.selectedZoneId) {
      return null;
    }

    return this.zonesById[this.selectedZoneId] ?? null;
  }

  get categoryOptions(): SelectOption[] {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allCategories', value: null }];

    const categoryValues: string[] = Array.from(new Set(this.allItems.map((item: ParkItem) => item.category)))
      .sort((left: string, right: string) => left.localeCompare(right));

    return base.concat(categoryValues.map((value: string) => ({
      value,
      labelKey: `parkExplorer.categories.${this.toCamelCase(value)}`
    })));
  }

  get typeOptions(): SelectOption[] {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allTypes', value: null }];

    const typeValues: string[] = Array.from(new Set(this.allItems.map((item: ParkItem) => item.type)))
      .sort((left: string, right: string) => left.localeCompare(right));

    return base.concat(typeValues.map((value: string) => ({
      value,
      labelKey: `parkExplorer.types.${this.toCamelCase(value)}`
    })));
  }

  get zoneOptions(): SelectOption[] {
    const base: SelectOption[] = [{ labelKey: 'parkItems.filters.allZones', value: null }];
    const values: SelectOption[] = Object.entries(this.zonesById)
      .map(([value, label]: [string, string]) => ({ value, label }))
      .sort((left: SelectOption, right: SelectOption) => (left.label ?? '').localeCompare(right.label ?? ''));

    return base.concat(values);
  }

  get topTypeHighlights(): Array<{ key: string; count: number }> {
    if (!this.explorer) {
      return [];
    }

    return [...this.explorer.overview.countsByType]
      .sort((left, right) => right.count - left.count)
      .slice(0, 4);
  }

  get totalResults(): number {
    return this.filteredItems.length;
  }

  get rangeStart(): number {
    if (this.totalResults === 0) {
      return 0;
    }

    return ((this.currentPage - 1) * this.pageSize) + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalResults);
  }

  get parkLink(): string[] | null {
    if (!this.park?.id || !this.park?.name) {
      return null;
    }

    return ['/', this.currentLang, 'park', this.park.id, buildParkSlug(this.park.name)];
  }

  getParkZoneDisplayName(bucket: ParkExplorerBucket): string {
    const localizedName: string | undefined = resolveLocalizedValue(bucket.names, this.currentLang);
    return localizedName ?? bucket.name;
  }

  getZoneCardTypeHighlights(bucket: ParkExplorerBucket): Array<{ key: string; count: number }> {
    return [...bucket.countsByType]
      .sort((left, right) => right.count - left.count)
      .slice(0, 3);
  }

  getTypeKey(type: string): string {
    return `parkExplorer.types.${this.toCamelCase(type)}`;
  }

  getManufacturerName(item: ParkItem): string | null {
    const manufacturerId: string | null | undefined = item.attractionDetails?.manufacturerId;
    if (!manufacturerId) {
      return null;
    }

    return this.manufacturersById[manufacturerId] ?? null;
  }

  getZoneName(item: ParkItem): string | null {
    if (!item.zoneId) {
      return null;
    }

    return this.zonesById[item.zoneId] ?? null;
  }

  isZoneSelected(bucket: ParkExplorerBucket): boolean {
    return this.selectedZoneId === (bucket.id ?? null);
  }

  selectZone(bucket: ParkExplorerBucket): void {
    this.selectedZoneId = bucket.id ?? null;
    this.currentPage = 1;
    this.updateQueryParams();
  }

  goBack(): void {
    if (this.parkLink) {
      this.router.navigate(this.parkLink);
      return;
    }

    this.router.navigate(['/', this.currentLang, 'parks']);
  }

  onFiltersChanged(): void {
    this.currentPage = 1;
    this.updateQueryParams();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = null;
    this.selectedType = null;
    this.selectedZoneId = null;
    this.currentPage = 1;
    this.pageSize = 12;
    this.updateQueryParams();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.currentPage = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.updateQueryParams();
  }

  private loadData(parkId: string): void {
    this.pageState = ViewState.Loading;
    this.park = null;
    this.explorer = null;
    this.allItems = [];
    this.filteredItems = [];
    this.pagedItems = [];
    this.manufacturersById = {};
    this.zonesById = {};

    this.subscriptions.add(forkJoin({
      park: this.parksApiService.getParkById(parkId),
      explorer: this.parksApiService.getParkExplorer(parkId),
      items: this.parkItemsApiService.getParkItemsByParkId(parkId),
      manufacturers: this.manufacturersApiService.getAttractionManufacturers().pipe(catchError(() => of([] as AttractionManufacturer[]))),
      zones: this.parkZonesApiService.getParkZonesByParkId(parkId).pipe(catchError(() => of([] as ParkZone[])))
    }).subscribe({
      next: ({ park, explorer, items, manufacturers, zones }) => {
        const manufacturersById: Record<string, string> = manufacturers.reduce((accumulator, current) => {
          if (current.id && current.name) {
            accumulator[current.id] = current.name;
          }
          return accumulator;
        }, {} as Record<string, string>);

        const zonesById: Record<string, string> = zones.reduce((accumulator, current) => {
          const localizedName: string | undefined = resolveLocalizedValue(current.names, this.currentLang);
          if (current.id) {
            accumulator[current.id] = localizedName ?? current.name ?? current.id;
          }
          return accumulator;
        }, {} as Record<string, string>);

        commitViewUpdate(this.changeDetectorRef, () => {
          this.park = park;
          this.explorer = explorer;
          this.allItems = items;
          this.manufacturersById = manufacturersById;
          this.zonesById = zonesById;
          this.pageState = ViewState.Ready;

          if (this.selectedZoneId && !this.zonesById[this.selectedZoneId]) {
            this.selectedZoneId = null;
          }

          this.applyFilters();
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park items page', error);
        commitViewUpdate(this.changeDetectorRef, () => {
          this.pageState = ViewState.Error;
        });
      }
    }));
  }

  private applyFilters(): void {
    if (this.allItems.length === 0) {
      this.filteredItems = [];
      this.pagedItems = [];
      return;
    }

    const normalizedSearchTerm: string = this.searchTerm.trim().toLowerCase();

    this.filteredItems = this.allItems.filter((item: ParkItem) => {
      if (this.selectedCategory && item.category !== this.selectedCategory) {
        return false;
      }

      if (this.selectedType && item.type !== this.selectedType) {
        return false;
      }

      if (this.selectedZoneId && item.zoneId !== this.selectedZoneId) {
        return false;
      }

      if (!normalizedSearchTerm) {
        return true;
      }

      const description: string = resolveParkItemDescription(item, this.currentLang) ?? '';
      const haystack: string = [
        item.name,
        item.subtype,
        description,
        this.getManufacturerName(item),
        item.attractionDetails?.model,
        item.attractionDetails?.status,
        this.getZoneName(item)
      ]
        .filter((value: string | null | undefined): value is string => !!value)
        .join(' ')
        .toLowerCase();

      return haystack.includes(normalizedSearchTerm);
    }).sort((left: ParkItem, right: ParkItem) => left.name.localeCompare(right.name));

    const firstIndex: number = (this.currentPage - 1) * this.pageSize;
    this.pagedItems = this.filteredItems.slice(firstIndex, firstIndex + this.pageSize);

    if (this.filteredItems.length > 0 && this.pagedItems.length === 0) {
      this.currentPage = 1;
      this.pagedItems = this.filteredItems.slice(0, this.pageSize);
    }
  }

  private updateQueryParams(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.searchTerm || null,
        category: this.selectedCategory,
        type: this.selectedType,
        zone: this.selectedZoneId,
        page: this.currentPage > 1 ? this.currentPage : null,
        size: this.pageSize !== 12 ? this.pageSize : null
      },
      queryParamsHandling: 'merge'
    });
  }

  private toCamelCase(value: string): string {
    return value.charAt(0).toLowerCase() + value.slice(1);
  }
}
