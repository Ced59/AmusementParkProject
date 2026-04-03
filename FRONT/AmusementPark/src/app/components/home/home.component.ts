import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { EMPTY, Subject, Subscription } from 'rxjs';
import { catchError, debounceTime, switchMap } from 'rxjs/operators';
import { ApiService } from '../../services/api.service';
import { SearchResultItem } from '../../models/search/search-result-item';
import { SearchApiResponse } from '../../models/search/search-api-response';
import { Pagination } from '../../models/shared/pagination';
import { ViewState } from '../../models/shared/view-state';
import { Park } from '../../models/parks/park';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  searchTerm = '';
  selectedCategory = '';
  currentLang = 'en';

  readonly categoryOptions: { labelKey: string; value: string }[] = [
    { labelKey: 'home.categories.park', value: 'park' },
    { labelKey: 'home.categories.attractions', value: 'attractions' }
  ];

  readonly featuredParks = signal<Park[]>([]);
  readonly featuredState = signal<ViewState>(ViewState.Loading);

  readonly results = signal<SearchResultItem[]>([]);
  readonly pagination = signal<Pagination | null>(null);
  readonly searchState = signal<ViewState>(ViewState.Ready);
  readonly hasPerformedSearch = signal<boolean>(false);

  currentPage = 1;
  pageSize = 10;

  private readonly searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  constructor(
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
    this.loadFeaturedParks();

    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300),
      switchMap((term: string) => {
        if (!term && !this.selectedCategory) {
          this.results.set([]);
          this.pagination.set(null);
          this.hasPerformedSearch.set(false);
          this.searchState.set(ViewState.Ready);
          return EMPTY;
        }

        this.currentPage = 1;
        this.hasPerformedSearch.set(true);
        this.searchState.set(ViewState.Loading);

        const categoriesToSend: string[] = this.selectedCategory ? [this.selectedCategory] : [];

        return this.apiService.getSearch(term, categoriesToSend, this.currentPage, this.pageSize).pipe(
          catchError((error: unknown) => {
            console.error('Error searching content', error);
            this.results.set([]);
            this.pagination.set(null);
            this.searchState.set(ViewState.Error);
            return EMPTY;
          })
        );
      })
    ).subscribe((response: SearchApiResponse) => {
      const results = response.data ?? [];
      this.results.set(results);
      this.pagination.set(response.pagination ?? null);
      this.searchState.set(results.length > 0 ? ViewState.Ready : ViewState.Empty);
    });
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  onCategoryChange(): void {
    this.searchSubject.next(this.searchTerm);
  }

  onPageChange(event: { page: number; rows: number }): void {
    this.currentPage = event.page + 1;
    this.pageSize = event.rows;
    this.hasPerformedSearch.set(true);
    this.searchState.set(ViewState.Loading);

    const categoriesToSend: string[] = this.selectedCategory ? [this.selectedCategory] : [];

    this.apiService.getSearch(this.searchTerm, categoriesToSend, this.currentPage, this.pageSize).pipe(
      catchError((error: unknown) => {
        console.error('Error loading search page', error);
        this.results.set([]);
        this.pagination.set(null);
        this.searchState.set(ViewState.Error);
        return EMPTY;
      })
    ).subscribe((response: SearchApiResponse) => {
      const results = response.data ?? [];
      this.results.set(results);
      this.pagination.set(response.pagination ?? null);
      this.searchState.set(results.length > 0 ? ViewState.Ready : ViewState.Empty);
    });
  }

  private loadFeaturedParks(): void {
    this.featuredState.set(ViewState.Loading);

    this.apiService.getParksPaginated(1, 6).subscribe({
      next: (response) => {
        const parks = response.data ?? [];
        this.featuredParks.set(parks);
        this.featuredState.set(parks.length > 0 ? ViewState.Ready : ViewState.Empty);
      },
      error: (error: unknown) => {
        console.error('Error loading featured parks', error);
        this.featuredState.set(ViewState.Error);
      }
    });
  }
}
