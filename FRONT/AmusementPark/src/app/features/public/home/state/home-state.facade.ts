import { Injectable, Signal, computed, signal } from '@angular/core';
import { SearchApiService } from '@data-access/search/search-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { Park } from '@app/models/parks/park';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { Pagination } from '@app/models/shared/pagination';

interface HomeFeaturedViewModel {
  parks: Park[];
}

interface HomeSearchViewModel {
  results: SearchResultItem[];
  pagination: Pagination | null;
  hasPerformedSearch: boolean;
}

@Injectable()
export class HomeStateFacade {
  private readonly featuredStateStore = new SignalScreenStateStore<HomeFeaturedViewModel>();
  private readonly searchStateStore = new SignalScreenStateStore<HomeSearchViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(10);

  public readonly featuredState = this.featuredStateStore.state;
  public readonly featuredParks: Signal<Park[]> = computed(() => this.featuredStateStore.data()?.parks ?? []);

  public readonly searchState = this.searchStateStore.state;
  public readonly searchResults: Signal<SearchResultItem[]> = computed(() => this.searchStateStore.data()?.results ?? []);
  public readonly searchPagination: Signal<Pagination | null> = computed(() => this.searchStateStore.data()?.pagination ?? null);
  public readonly hasPerformedSearch = computed(() => this.searchStateStore.data()?.hasPerformedSearch ?? false);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(
    private readonly parksApiService: ParksApiService,
    private readonly searchApiService: SearchApiService
  ) {
    this.searchStateStore.setReady({
      results: [],
      pagination: null,
      hasPerformedSearch: false
    });
  }

  loadFeaturedParks(): void {
    const previousData: HomeFeaturedViewModel | undefined = this.featuredStateStore.data();
    this.featuredStateStore.setLoading(previousData);

    this.parksApiService.getParksPaginated(1, 6).subscribe({
      next: (response: { data?: Park[] | null }) => {
        const parks: Park[] = response.data ?? [];
        const viewModel: HomeFeaturedViewModel = {
          parks
        };

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

    this.searchApiService.getSearch(normalizedTerm, categoriesToSend, page, size).subscribe({
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
}
