import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed } from '@angular/core';
import { NgFor } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { PaginationContract } from '@shared/models/contracts';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiSearchPanelComponent, UiSearchPanelSelectFilterModel, UiSelectOptionModel } from '@ui/forms';
import { UiParkCardComponent, UiSearchResultCardComponent, UiSearchResultCardModel } from '@ui/cards';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { ParkListMapComponent } from './park-list-map.component';
import { LocalizedPluralPipe } from '@shared/pipes';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { PublicPlaceDiscoveryScope } from '@shared/models/search/public-search-category-option.model';
import { getSearchCategoryTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedCountryName } from '@shared/utils/display/country-display.helpers';
import { buildPublicStandaloneAttractionRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { getParkStatusPresentation, ParkStatusPresentation } from '@shared/utils/parks/park-status.presentation';

@Component({
  selector: 'app-park-list-view',
  templateUrl: './park-list-view.component.html',
  styleUrls: ['./park-list-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, PaginationComponent, NgFor, TranslateModule, UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent, UiSurfaceDirective, UiSearchPanelComponent, UiParkCardComponent, UiSearchResultCardComponent, ParkListMapComponent, PublicSharePanelComponent, LocalizedPluralPipe]
})
export class ParkListViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() mapState!: Signal<ScreenState<ParkMapPointViewModel[], string>>;
  @Input() parks!: Signal<ParkCardModel[]>;
  @Input() searchResults!: Signal<SearchResultItem[]>;
  @Input() pagination!: Signal<PaginationContract | null>;
  @Input() visibleMapPoints!: Signal<ParkMapPointViewModel[]>;
  @Input() visibleCountryCount!: Signal<number>;
  @Input() selectedMapParkId!: Signal<string | null>;
  @Input() selectedParkCard!: Signal<ParkCardModel | null>;
  @Input() selectedRegion!: Signal<ParkRegionFilter | null>;
  @Input() selectedStatus!: Signal<string | null>;
  @Input() selectedAudienceClassificationFilter!: Signal<ParkAudienceClassificationFilter | null>;
  @Input() discoveryScope!: Signal<PublicPlaceDiscoveryScope>;
  @Input() discoveryScopeFilterOptions!: Signal<UiSelectOptionModel[]>;
  @Input() statusFilterOptions!: Signal<UiSelectOptionModel[]>;
  @Input() audienceClassificationFilterOptions!: Signal<UiSelectOptionModel[]>;
  @Input() currentLang!: Signal<string>;
  @Input() searchTerm!: Signal<string>;

  @Output() searchInputChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() searchSubmitted: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearSearchClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() mapParkSelected: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() regionFilterChanged: EventEmitter<ParkRegionFilter | null> = new EventEmitter<ParkRegionFilter | null>();
  @Output() statusFilterChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() audienceClassificationFilterChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() discoveryScopeChanged: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() resultParkFocused: EventEmitter<ParkCardModel> = new EventEmitter<ParkCardModel>();
  @Output() selectedParkCleared: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  protected readonly filters = computed<UiSearchPanelSelectFilterModel[]>(() => [
    {
      id: 'discoveryScope',
      labelKey: 'parks.discoveryScopes.label',
      selectedValue: this.discoveryScope(),
      options: this.discoveryScopeFilterOptions()
    },
    {
      id: 'status',
      labelKey: 'parks.statusFilters.label',
      selectedValue: this.selectedStatus(),
      options: this.statusFilterOptions(),
      hidden: this.discoveryScope() !== 'parks'
    },
    {
      id: 'audienceClassification',
      labelKey: 'parks.audienceFilters.label',
      selectedValue: this.selectedAudienceClassificationFilter(),
      options: this.audienceClassificationFilterOptions(),
      hidden: this.discoveryScope() !== 'parks'
    }
  ]);

  protected buildParkLink(park: ParkCardModel): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang(),
      parkId: park.id,
      parkName: park.name
    });
  }

  protected get mapTitleKey(): string {
    return this.discoveryScope() === 'parks' ? 'parks.map.title' : 'parks.discoveryScopes.mapTitle';
  }

  protected get mapSubtitleKey(): string {
    return this.discoveryScope() === 'parks' ? 'parks.map.subtitle' : 'parks.discoveryScopes.mapSubtitle';
  }

  protected get mapCountPluralKey(): string {
    return this.discoveryScope() === 'standaloneAttractions'
      ? 'publicCounts.standaloneAttractionOnMap'
      : this.discoveryScope() === 'parksAndStandaloneAttractions'
        ? 'publicCounts.placeOnMap'
        : 'publicCounts.parkOnMap';
  }

  protected get resultsTitleKey(): string {
    return this.discoveryScope() === 'standaloneAttractions'
      ? 'parks.discoveryScopes.standaloneResultsTitle'
      : this.discoveryScope() === 'parksAndStandaloneAttractions'
        ? 'parks.discoveryScopes.resultsTitle'
        : 'parks.results.title';
  }

  protected get resultCountLabelKey(): string {
    return this.discoveryScope() === 'standaloneAttractions'
      ? 'parks.discoveryScopes.visibleStandaloneAttractions'
      : this.discoveryScope() === 'parksAndStandaloneAttractions'
        ? 'parks.discoveryScopes.visiblePlaces'
        : 'parks.stats.visibleParks';
  }

  protected buildSearchResultCard(item: SearchResultItem): UiSearchResultCardModel {
    const isPark: boolean = item.originalId?.startsWith('park_');
    const statusPresentation: ParkStatusPresentation | null = isPark ? getParkStatusPresentation(item.parkStatus) : null;
    const countryName: string | null = resolveLocalizedCountryName(item.countryCode, this.currentLang());
    const metaParts: string[] = [item.city?.trim() || null, countryName]
      .filter((value: string | null): value is string => !!value);

    return {
      title: item.title,
      description: item.description ?? null,
      logoImageId: item.logoImageId?.trim() ?? null,
      iconClass: isPark ? 'pi pi-map' : 'pi pi-bolt',
      tone: isPark ? 'primary' : 'sky',
      categoryLabelKey: getSearchCategoryTranslationKey(item.category),
      statusLabelKey: statusPresentation?.labelKey ?? null,
      statusIconClass: statusPresentation?.iconClass ?? null,
      statusTone: statusPresentation?.tone ?? null,
      metaParts,
      detailLink: isPark
        ? buildPublicParkRouteCommands({ language: this.currentLang(), parkId: item.originalId.substring(5), parkName: item.title })
        : item.originalId?.startsWith('standaloneAttraction_')
          ? buildPublicStandaloneAttractionRouteCommands({
            language: this.currentLang(),
            attractionId: item.originalId.substring('standaloneAttraction_'.length),
            attractionName: item.title
          })
          : null,
      actionLabelKey: 'home.search.openResult'
    };
  }

  onSearchInput(value: string): void {
    this.searchInputChanged.emit(value);
  }

  onSearchSubmit(): void {
    this.searchSubmitted.emit();
  }

  clearSearch(): void {
    this.clearSearchClicked.emit();
  }

  onMapParkSelected(parkId: string | null): void {
    this.mapParkSelected.emit(parkId);
  }

  onRegionFilterChanged(region: ParkRegionFilter | null): void {
    this.regionFilterChanged.emit(region);
  }

  onFilterChanged(event: { id: string; value: string | null }): void {
    if (event.id === 'discoveryScope') {
      this.discoveryScopeChanged.emit(event.value);
    }

    if (event.id === 'status') {
      this.statusFilterChanged.emit(event.value);
    }

    if (event.id === 'audienceClassification') {
      this.audienceClassificationFilterChanged.emit(event.value);
    }
  }

  onResultCardClick(event: MouseEvent, park: ParkCardModel): void {
    if (this.isInteractiveChildClick(event)) {
      return;
    }

    this.resultParkFocused.emit(park);
  }

  onResultCardKeydown(event: KeyboardEvent, park: ParkCardModel): void {
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }

    event.preventDefault();
    this.resultParkFocused.emit(park);
  }

  private isInteractiveChildClick(event: MouseEvent): boolean {
    const target: EventTarget | null = event.target;
    const currentTarget: EventTarget | null = event.currentTarget;

    if (!(target instanceof HTMLElement) || !(currentTarget instanceof HTMLElement)) {
      return false;
    }

    const interactiveElement: Element | null = target.closest('a, button, input, textarea, select, [role=\"button\"]');
    return !!interactiveElement && interactiveElement !== currentTarget;
  }

  clearSelectedPark(): void {
    this.selectedParkCleared.emit();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
