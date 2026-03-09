import { Component, OnDestroy, OnInit } from '@angular/core';
import { EMPTY, Observable, Subject, Subscription } from 'rxjs';
import { catchError, debounceTime, switchMap } from 'rxjs/operators';

import { SearchApiResponse } from '../../models/search/search-api-response';
import { SearchResultItem } from '../../models/search/search-result-item';
import { Pagination } from '../../models/shared/pagination';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  searchTerm: string = '';
  selectedCategory: string = '';
  categoryOptions: { labelKey: string; value: string }[] = [
    { labelKey: 'home.categories.everywhere', value: '' },
    { labelKey: 'home.categories.park', value: 'park' },
    { labelKey: 'home.categories.parkItems', value: 'parkItems' },
    { labelKey: 'home.categories.operators', value: 'operators' },
    { labelKey: 'home.categories.manufacturers', value: 'manufacturers' }
  ];

  private readonly searchSubject: Subject<string> = new Subject<string>();
  private subscription$!: Subscription;

  results: SearchResultItem[] = [];
  pagination: Pagination | null = null;

  currentPage: number = 1;
  pageSize: number = 10;

  constructor(private readonly apiService: ApiService) {
  }

  ngOnInit(): void {
    this.subscription$ = this.searchSubject.pipe(
      debounceTime(300),
      switchMap(() => {
        if (!this.searchTerm && !this.selectedCategory) {
          this.results = [];
          this.pagination = null;
          return EMPTY;
        }

        this.currentPage = 1;
        return this.executeSearch();
      })
    ).subscribe((response: SearchApiResponse) => {
      this.results = response.data;
      this.pagination = response.pagination;
    });
  }

  ngOnDestroy(): void {
    if (this.subscription$) {
      this.subscription$.unsubscribe();
    }
  }

  onSearchInput(value: string): void {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  onCategoryChange(): void {
    this.searchSubject.next(this.searchTerm);
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.currentPage = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? this.pageSize;

    this.executeSearch().subscribe((response: SearchApiResponse) => {
      this.results = response.data;
      this.pagination = response.pagination;
    });
  }

  getCategoryLabelKey(category: string): string {
    return `home.categories.${category}`;
  }

  private executeSearch(): Observable<SearchApiResponse> {
    const categoriesToSend: string[] = this.selectedCategory
      ? [this.selectedCategory]
      : [];

    return this.apiService.getSearch(
      this.searchTerm,
      categoriesToSend,
      this.currentPage,
      this.pageSize
    ).pipe(
      catchError(() => {
        this.results = [];
        this.pagination = null;
        return EMPTY;
      })
    );
  }
}
