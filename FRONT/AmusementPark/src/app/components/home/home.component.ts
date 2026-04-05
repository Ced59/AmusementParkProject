import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { EMPTY, Subject, Subscription } from 'rxjs';
import { catchError, debounceTime, switchMap } from 'rxjs/operators';

import { SearchApiResponse } from '../../models/search/search-api-response';
import { SearchResultItem } from '../../models/search/search-result-item';
import { Pagination } from '../../models/shared/pagination';
import { ViewState } from '../../models/shared/view-state';
import { Park } from '../../models/parks/park';
import { ApiService } from '../../services/api.service';
import { TranslationService } from '../../services/translation.service';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { RouterLink } from '@angular/router';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { NgFor, NgIf } from '@angular/common';
import { ParkCardComponent } from '../public/park-card/park-card.component';
import { SearchResultCardComponent } from '../public/search-result-card/search-result-card.component';
import { Paginator } from 'primeng/paginator';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, ButtonDirective, RouterLink, InputText, Select, FormsModule, PageStateComponent, NgFor, ParkCardComponent, NgIf, SearchResultCardComponent, Paginator, TranslateModule]
})
export class HomeComponent implements OnInit, OnDestroy {
  searchTerm: string = '';
  selectedCategory: string = '';
  currentLang: string = 'en';

  categoryOptions: { labelKey: string; value: string }[] = [
    { labelKey: 'home.categories.everywhere', value: '' },
    { labelKey: 'home.categories.park', value: 'park' },
    { labelKey: 'home.categories.parkItems', value: 'parkItems' },
    { labelKey: 'home.categories.operators', value: 'operators' },
    { labelKey: 'home.categories.manufacturers', value: 'manufacturers' }
  ];

  featuredParks: Park[] = [];
  featuredState: ViewState = ViewState.Loading;

  results: SearchResultItem[] = [];
  pagination: Pagination | null = null;
  searchState: ViewState = ViewState.Ready;
  hasPerformedSearch: boolean = false;

  currentPage: number = 1;
  pageSize: number = 10;

  private readonly searchSubject: Subject<string> = new Subject<string>();
  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';
    this.loadFeaturedParks();

    this.subscriptions.add(
      this.searchSubject.pipe(
        debounceTime(300),
        switchMap(() => {
          if (!this.searchTerm && !this.selectedCategory) {
            this.results = [];
            this.pagination = null;
            this.hasPerformedSearch = false;
            this.searchState = ViewState.Ready;
            this.cdr.markForCheck();
            return EMPTY;
          }

          this.currentPage = 1;
          this.hasPerformedSearch = true;
          this.searchState = ViewState.Loading;
          this.cdr.markForCheck();

          return this.executeSearch();
        })
      ).subscribe((response: SearchApiResponse) => {
        this.results = response.data ?? [];
        this.pagination = response.pagination ?? null;
        this.searchState = this.results.length > 0 ? ViewState.Ready : ViewState.Empty;
        this.cdr.markForCheck();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  onCategoryChange(): void {
    this.searchSubject.next(this.searchTerm);
  }


  get searchResultsTotal(): number {
    return this.pagination?.totalItems ?? this.results.length;
  }

  get searchResultsHintKey(): string {
    return this.hasPerformedSearch ? 'home.search.resultsSubtitle' : 'home.search.hintMessage';
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.currentPage = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;
    this.hasPerformedSearch = true;
    this.searchState = ViewState.Loading;
    this.cdr.markForCheck();

    this.subscriptions.add(
      this.executeSearch().subscribe((response: SearchApiResponse) => {
        this.results = response.data ?? [];
        this.pagination = response.pagination ?? null;
        this.searchState = this.results.length > 0 ? ViewState.Ready : ViewState.Empty;
        this.cdr.markForCheck();
      })
    );
  }

  private executeSearch() {
    const categoriesToSend: string[] = this.selectedCategory ? [this.selectedCategory] : [];

    return this.apiService.getSearch(
      this.searchTerm,
      categoriesToSend,
      this.currentPage,
      this.pageSize
    ).pipe(
      catchError((error: unknown) => {
        console.error('Error searching content', error);
        this.results = [];
        this.pagination = null;
        this.searchState = ViewState.Error;
        this.cdr.markForCheck();
        return EMPTY;
      })
    );
  }

  private loadFeaturedParks(): void {
    this.featuredState = ViewState.Loading;

    this.subscriptions.add(
      this.apiService.getParksPaginated(1, 6).subscribe({
        next: (response) => {
          this.featuredParks = response.data ?? [];
          this.featuredState = this.featuredParks.length > 0 ? ViewState.Ready : ViewState.Empty;
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          console.error('Error loading featured parks', error);
          this.featuredState = ViewState.Error;
          this.cdr.markForCheck();
        }
      })
    );
  }
}
