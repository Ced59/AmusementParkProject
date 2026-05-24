import { Injectable, Signal, computed, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Park } from '@app/models/parks/park';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { SearchApiService } from '@data-access/search/search-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { HomeApiService } from '@data-access/home/home-api.service';
import { Pagination } from '@app/models/shared/pagination';
import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { HomeFeaturedParkCardModel } from '@app/models/home/home-featured-park-card.model';
import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapArray, mapParkToCardModel } from '@shared/utils/mapping';
import { mapHomeFeaturedParkToCardModel } from '../mappers/home-featured-park.mapper';

interface HomeHeroParksViewModel {
  parks: ParkCardModel[];
}

interface HomeFeaturedViewModel {
  parks: HomeFeaturedParkCardModel[];
}

interface HomeSearchViewModel {
  results: SearchResultItem[];
  pagination: Pagination | null;
  hasPerformedSearch: boolean;
}

interface HomeStatsViewModel {
  stats: HomeStatsModel;
}

@Injectable()
export class HomeStateFacade {
  private readonly heroParksStateStore = new SignalScreenStateStore<HomeHeroParksViewModel>();
  private readonly featuredStateStore = new SignalScreenStateStore<HomeFeaturedViewModel>();
  private readonly searchStateStore = new SignalScreenStateStore<HomeSearchViewModel>();
  private readonly statsStateStore = new SignalScreenStateStore<HomeStatsViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(10);

  public readonly heroParksState = this.heroParksStateStore.state;
  public readonly heroParks: Signal<ParkCardModel[]> = computed(() => this.heroParksStateStore.data()?.parks ?? []);

  public readonly featuredState = this.featuredStateStore.state;
  public readonly featuredParks: Signal<HomeFeaturedParkCardModel[]> = computed(() => this.featuredStateStore.data()?.parks ?? []);

  public readonly statsState = this.statsStateStore.state;
  public readonly homeStats: Signal<HomeStatsModel | null> = computed(() => this.statsStateStore.data()?.stats ?? null);

  public readonly searchState = this.searchStateStore.state;
  public readonly searchResults: Signal<SearchResultItem[]> = computed(() => this.searchStateStore.data()?.results ?? []);
  public readonly searchPagination: Signal<Pagination | null> = computed(() => this.searchStateStore.data()?.pagination ?? null);
  public readonly hasPerformedSearch = computed(() => this.searchStateStore.data()?.hasPerformedSearch ?? false);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly searchApiService: SearchApiService,
    private readonly homeApiService: HomeApiService,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly countryDisplayService: CountryDisplayService,
    private readonly destroyRef: DestroyRef
  ) {
    this.searchStateStore.setReady({
      results: [],
      pagination: null,
      hasPerformedSearch: false
    });
  }

  loadHomeStats(): void {
    const previousData: HomeStatsViewModel | undefined = this.statsStateStore.data();
    this.statsStateStore.setLoading(previousData);

    this.homeApiService.getHomeStats().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (stats: HomeStatsModel) => {
        this.statsStateStore.setReady({ stats });
      },
      error: (error: unknown) => {
        console.error('Error loading home stats', error);
        this.statsStateStore.setError('home.stats.errorMessage', previousData);
      }
    });
  }

  loadFeaturedParks(currentLanguage: string): void {
    const previousHeroData: HomeHeroParksViewModel | undefined = this.heroParksStateStore.data();
    const previousFeaturedData: HomeFeaturedViewModel | undefined = this.featuredStateStore.data();

    this.heroParksStateStore.setLoading(previousHeroData);
    this.featuredStateStore.setLoading(previousFeaturedData);

    this.parksApiService.getRandomVisibleParks(4).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: Park[]) => {
        const heroParks: ParkCardModel[] = mapArray(response, (park: Park) => mapParkToCardModel(park, currentLanguage, this.countryDisplayService));
        this.setHeroParks(heroParks);
        this.loadHomeFeaturedParks(currentLanguage, heroParks.map((park: ParkCardModel) => park.id).filter((parkId: string | null): parkId is string => !!parkId));
      },
      error: (error: unknown) => {
        console.error('Error loading hero parks', error);
        this.heroParksStateStore.setError('home.heroCards.errorMessage', previousHeroData);
        this.loadHomeFeaturedParks(currentLanguage, []);
      }
    });
  }

  search(term: string, selectedCategory: string, page: number = 1, size: number = this.pageSizeSignal()): void {
    const normalizedTerm: string = term.trim();
    const categoriesToSend: string[] = selectedCategory ? [selectedCategory] : [];

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);

    if (!normalizedTerm && categoriesToSend.length === 0) {
      this.clearSearch();
      return;
    }

    const previousData: HomeSearchViewModel | undefined = this.searchStateStore.data();
    this.searchStateStore.setLoading(previousData);

    this.searchApiService.getSearch(normalizedTerm, categoriesToSend, page, size).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: SearchApiResponse) => {
        const results: SearchResultItem[] = response.data ?? [];
        const pagination: Pagination | null = response.pagination ?? null;
        const viewModel: HomeSearchViewModel = {
          results,
          pagination,
          hasPerformedSearch: true
        };

        if (results.length === 0) {
          this.searchStateStore.setEmpty(viewModel);
          return;
        }

        this.searchStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error searching content', error);
        this.searchStateStore.setError('home.search.errorMessage', {
          results: [],
          pagination: null,
          hasPerformedSearch: true
        });
      }
    });
  }

  clearSearch(): void {
    this.currentPageSignal.set(1);
    this.searchStateStore.setReady({
      results: [],
      pagination: null,
      hasPerformedSearch: false
    });
  }

  private setHeroParks(parks: ParkCardModel[]): void {
    const viewModel: HomeHeroParksViewModel = { parks };

    if (parks.length === 0) {
      this.heroParksStateStore.setEmpty(viewModel);
      return;
    }

    this.heroParksStateStore.setReady(viewModel);
  }

  private loadHomeFeaturedParks(currentLanguage: string, excludedParkIds: readonly string[]): void {
    const previousData: HomeFeaturedViewModel | undefined = this.featuredStateStore.data();

    this.homeApiService.getFeaturedParks(excludedParkIds, 3).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: HomeFeaturedParkModel[]) => {
        const parks: HomeFeaturedParkCardModel[] = response.map((park: HomeFeaturedParkModel, index: number) =>
          mapHomeFeaturedParkToCardModel(park, currentLanguage, this.textTruncator, index, this.countryDisplayService));
        const viewModel: HomeFeaturedViewModel = { parks };

        if (parks.length === 0) {
          this.featuredStateStore.setEmpty(viewModel);
          return;
        }

        this.featuredStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading featured parks', error);
        this.featuredStateStore.setError('home.featured.errorMessage', previousData);
      }
    });
  }
}
