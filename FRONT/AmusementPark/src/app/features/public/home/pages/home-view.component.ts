import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { PaginationContract } from '@shared/models/contracts';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { HomeFeaturedParkCardModel } from '@app/models/home/home-featured-park-card.model';
import { getSearchCategoryTranslationKey } from '@shared/utils/display/display-label.helpers';
import { resolveLocalizedCountryName } from '@shared/utils/display/country-display.helpers';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiSearchPanelSelectFilterModel } from '@ui/forms/models/ui-search-panel.model';
import { UiSearchPanelComponent } from '@ui/forms';
import { UiButtonDirective, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiPrimitiveTone } from '@ui/primitives/models/ui-primitive-variant.model';
import { UiFeaturedParkCardComponent, UiSearchResultCardComponent, UiSearchResultCardModel } from '@ui/cards';

@Component({
  selector: 'app-home-view',
  templateUrl: './home-view.component.html',
  styleUrls: ['./home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    PageStateComponent,
    PaginationComponent,
    EmptyStateComponent,
    UiSearchResultCardComponent,
    TranslateModule,
    UiFeaturedParkCardComponent,
    UiButtonDirective,
    UiSearchPanelComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class HomeViewComponent {
  constructor(private readonly translateService: TranslateService) {
  }

  protected readonly heroCardStyles: { tone: string; iconClass: string; tagKey: string }[] = [
    { tone: 'primary', iconClass: 'pi pi-map-marker', tagKey: 'home.heroCards.featured' },
    { tone: 'lime', iconClass: 'pi pi-compass', tagKey: 'home.heroCards.sensations' },
    { tone: 'sky', iconClass: 'pi pi-bolt', tagKey: 'home.heroCards.featured' },
    { tone: 'rose', iconClass: 'pi pi-sparkles', tagKey: 'home.heroCards.family' }
  ];

  @Input() currentLang!: Signal<string>;
  @Input() searchTerm!: Signal<string>;
  @Input() searchFilters!: Signal<UiSearchPanelSelectFilterModel[]>;
  @Input() statsState!: Signal<ScreenState<unknown, string>>;
  @Input() homeStats!: Signal<HomeStatsModel | null>;
  @Input() featuredState!: Signal<ScreenState<unknown, string>>;
  @Input() featuredParks!: Signal<HomeFeaturedParkCardModel[]>;
  @Input() heroFeaturedParks!: Signal<ParkCardModel[]>;
  @Input() searchState!: Signal<ScreenState<unknown, string>>;
  @Input() results!: Signal<SearchResultItem[]>;
  @Input() pagination!: Signal<PaginationContract | null>;
  @Input() hasPerformedSearch!: Signal<boolean>;
  @Input() searchResultsTotal: number = 0;
  @Input() searchResultsHintKey: string = '';

  @Output() searchInputChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() categoryChanged: EventEmitter<string> = new EventEmitter<string>();
  @Output() searchCleared: EventEmitter<void> = new EventEmitter<void>();
  @Output() pageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  protected getHeroCardStyle(index: number): { tone: string; iconClass: string; tagKey: string } {
    return this.heroCardStyles[index % this.heroCardStyles.length];
  }

  protected getHeroCardClass(index: number): string {
    return `home-hero-card home-hero-card--${index + 1} home-hero-card--${this.getHeroCardStyle(index).tone}`;
  }

  protected buildParkLink(park: ParkCardModel): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang(),
      parkId: park.id,
      parkName: park.name
    });
  }

  protected buildSearchResultCard(item: SearchResultItem): UiSearchResultCardModel {
    return {
      title: item.title,
      description: item.description ?? null,
      logoImageId: item.logoImageId?.trim() ?? null,
      iconClass: this.resolveSearchResultIconClass(item),
      tone: this.resolveSearchResultTone(item),
      categoryLabelKey: getSearchCategoryTranslationKey(item.category),
      metaParts: this.buildSearchResultMetaParts(item),
      detailLink: this.buildSearchResultLink(item),
      actionLabelKey: 'home.search.openResult'
    };
  }

  private buildSearchResultMetaParts(item: SearchResultItem): string[] {
    const metaParts: string[] = [];

    if (item.city?.trim()) {
      metaParts.push(item.city.trim());
    }

    const countryName: string | null = resolveLocalizedCountryName(item.countryCode, this.currentLang());
    if (countryName) {
      metaParts.push(countryName);
    }

    if (this.isParkSearchResult(item) && item.attractionCount !== null && item.attractionCount !== undefined) {
      metaParts.push(`${new Intl.NumberFormat(this.currentLang()).format(item.attractionCount)} ${this.searchResultsAttractionsLabel}`);
      return metaParts;
    }

    const subtitle: string | null = this.resolveSearchResultSubtitle(item);
    if (subtitle) {
      metaParts.push(subtitle);
    }

    return metaParts;
  }

  private buildSearchResultLink(item: SearchResultItem): string[] | null {
    const originalId: string = item.originalId?.trim() ?? '';
    const title: string = item.title?.trim() ?? '';

    if (originalId.startsWith('park_')) {
      return buildPublicParkRouteCommands({
        language: this.currentLang(),
        parkId: originalId.substring(5),
        parkName: title
      });
    }

    if (originalId.startsWith('parkItem_')) {
      return buildPublicParkItemRouteCommands({
        language: this.currentLang(),
        parkId: item.parentParkId,
        parkName: item.parentParkName,
        itemId: originalId.substring(9),
        itemName: title
      });
    }

    return null;
  }

  private resolveSearchResultSubtitle(item: SearchResultItem): string | null {
    const subtitle: string = item.subtitle?.trim() ?? '';
    if (subtitle.length > 0) {
      return subtitle;
    }

    const parentParkName: string = item.parentParkName?.trim() ?? '';
    return parentParkName.length > 0 ? parentParkName : null;
  }

  private resolveSearchResultIconClass(item: SearchResultItem): string {
    const category: string = this.normalizeSearchResultCategory(item.category);

    if (category === 'attraction' || category.includes('item')) {
      return 'pi pi-bolt';
    }

    if (category === 'hotel') {
      return 'pi pi-home';
    }

    if (category === 'restaurant') {
      return 'pi pi-shop';
    }

    if (category === 'shop') {
      return 'pi pi-shopping-bag';
    }

    if (category === 'show') {
      return 'pi pi-star';
    }

    if (category === 'operator') {
      return 'pi pi-building';
    }

    if (category === 'manufacturer') {
      return 'pi pi-wrench';
    }

    if (category === 'founder') {
      return 'pi pi-user';
    }

    return 'pi pi-map';
  }

  private resolveSearchResultTone(item: SearchResultItem): UiPrimitiveTone {
    const category: string = this.normalizeSearchResultCategory(item.category);

    if (category === 'attraction' || category.includes('item')) {
      return 'sky';
    }

    if (category === 'hotel' || category === 'restaurant') {
      return 'gold';
    }

    if (category === 'shop' || category === 'show') {
      return 'rose';
    }

    if (category === 'operator') {
      return 'lime';
    }

    if (category === 'manufacturer') {
      return 'purple';
    }

    return 'primary';
  }

  private isParkSearchResult(item: SearchResultItem): boolean {
    return this.normalizeSearchResultCategory(item.category) === 'park'
      || this.normalizeSearchResultCategory(item.resourceType) === 'parks';
  }

  private normalizeSearchResultCategory(value: string | null | undefined): string {
    return (value ?? '')
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '')
      .replace(/s$/, '');
  }

  private get searchResultsAttractionsLabel(): string {
    return this.translateService.instant('home.search.attractionsLabel') as string;
  }

  protected formatStatValue(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '—';
    }

    return new Intl.NumberFormat(this.currentLang()).format(value);
  }

  onSearchInput(value: string): void {
    this.searchInputChanged.emit(value);
  }

  onFilterChanged(event: { id: string; value: string | null }): void {
    if (event.id !== 'category') {
      return;
    }

    this.categoryChanged.emit(event.value ?? '');
  }

  onClearSearch(): void {
    this.searchCleared.emit();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.pageChanged.emit(event);
  }
}
