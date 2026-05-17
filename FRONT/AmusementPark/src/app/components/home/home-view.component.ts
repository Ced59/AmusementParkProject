import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { PaginationComponent } from '../shared/pagination/pagination.component';
import { EmptyStateComponent } from '../shared/empty-state/empty-state.component';
import { SearchResultCardComponent } from '../public/search-result-card/search-result-card.component';
import { PaginationContract } from '@shared/models/contracts';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { HomeFeaturedParkCardModel } from '@app/models/home/home-featured-park-card.model';
import { buildParkSlug } from '@shared/utils/display/park-presentation.helpers';
import { UiSearchPanelSelectFilterModel } from '@ui/forms/models/ui-search-panel.model';
import { UiSearchPanelComponent } from '@ui/forms';
import { UiButtonDirective, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiFeaturedParkCardComponent } from '@ui/cards';

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
    SearchResultCardComponent,
    TranslateModule,
    UiFeaturedParkCardComponent,
    UiButtonDirective,
    UiSearchPanelComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class HomeViewComponent {
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
    if (!park.id || !park.name) {
      return null;
    }

    return ['/', this.currentLang(), 'park', park.id, buildParkSlug(park.name)];
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
