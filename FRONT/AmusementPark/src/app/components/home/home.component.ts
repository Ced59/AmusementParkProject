import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { EMPTY, Subject } from 'rxjs';
import { Router } from '@angular/router';
import { debounceTime, switchMap } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { HomeStateFacade } from '@features/public/home/state/home-state.facade';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiSearchPanelSelectFilterModel } from '@ui/forms/models/ui-search-panel.model';
import { HomeViewComponent } from './home-view.component';
import { SeoService } from '@core/seo/seo.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HomeStateFacade],
  imports: [HomeViewComponent]
})
export class HomeComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');
  protected readonly selectedCategory = signal<string>('');

  protected readonly categoryOptions: { labelKey: string; value: string }[] = [
    { labelKey: 'home.categories.everywhere', value: '' },
    { labelKey: 'home.categories.park', value: 'park' },
    { labelKey: 'home.categories.parkItems', value: 'parkItems' },
    { labelKey: 'home.categories.operators', value: 'operators' },
    { labelKey: 'home.categories.manufacturers', value: 'manufacturers' }
  ];

  protected readonly searchFilters = computed<UiSearchPanelSelectFilterModel[]>(() => [
    {
      id: 'category',
      labelKey: 'home.placeholder_category',
      selectedValue: this.selectedCategory() || null,
      options: this.categoryOptions.map((option: { labelKey: string; value: string }) => ({
        labelKey: option.labelKey,
        value: option.value || null
      }))
    }
  ]);

  protected readonly statsState = this.stateFacade.statsState;
  protected readonly homeStats = this.stateFacade.homeStats;
  protected readonly featuredState = this.stateFacade.featuredState;
  protected readonly featuredParks = this.stateFacade.featuredParks;
  protected readonly heroFeaturedParks = computed<ParkCardModel[]>(() => this.stateFacade.heroParks().slice(0, 4));
  protected readonly searchState = this.stateFacade.searchState;
  protected readonly results = this.stateFacade.searchResults;
  protected readonly pagination = this.stateFacade.searchPagination;
  protected readonly hasPerformedSearch = this.stateFacade.hasPerformedSearch;

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly searchSubject: Subject<void> = new Subject<void>();

  constructor(
    private readonly stateFacade: HomeStateFacade,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly seoService: SeoService
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.translationService.getCurrentLang() || 'en');
    this.seoService.applyHomeSeo(this.currentLang(), this.router.url);
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLang.set(language);
      this.seoService.applyHomeSeo(language, this.router.url);
      this.stateFacade.loadFeaturedParks(language);
    });
    this.stateFacade.loadHomeStats();
    this.stateFacade.loadFeaturedParks(this.currentLang());

    this.searchSubject.pipe(
      debounceTime(300),
      switchMap(() => {
        this.stateFacade.search(this.searchTerm(), this.selectedCategory(), 1, this.stateFacade.pageSize());
        return EMPTY;
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  get searchResultsTotal(): number {
    return this.pagination()?.totalItems ?? this.results().length;
  }

  get searchResultsHintKey(): string {
    return this.hasPerformedSearch() ? 'home.search.resultsSubtitle' : 'home.search.hintMessage';
  }

  onSearchInput(value: string): void {
    this.searchTerm.set(value.trim());
    this.searchSubject.next();
  }

  onCategoryChange(value: string): void {
    this.selectedCategory.set(value ?? '');
    this.searchSubject.next();
  }

  onClearSearch(): void {
    this.searchTerm.set('');
    this.selectedCategory.set('');
    this.stateFacade.clearSearch();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.stateFacade.pageSize();
    this.stateFacade.search(this.searchTerm(), this.selectedCategory(), page, rows);
  }
}
