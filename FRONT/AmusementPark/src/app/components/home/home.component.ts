// src/app/components/home/home.component.ts

import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject, Subscription, EMPTY } from 'rxjs';
import { debounceTime, switchMap, catchError } from 'rxjs/operators';
import { ApiService } from '../../services/api.service';
import { SearchResultItem } from '../../models/search/search-result-item';
import { SearchApiResponse } from '../../models/search/search-api-response';
import { Pagination } from '../../models/shared/pagination';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit, OnDestroy {
  searchTerm: string = '';
  selectedCategory: string = '';

  // On ne met plus de texte fixe ici, juste la clé de traduction et la value réelle
  categoryOptions: { labelKey: string; value: string }[] = [
    { labelKey: 'home.categories.park',    value: 'park' },
    { labelKey: 'home.categories.attractions', value: 'attractions' }
  ];

  private searchSubject = new Subject<string>();
  private subscription$!: Subscription;

  results: SearchResultItem[] = [];
  pagination: Pagination | null = null;

  currentPage = 1;
  pageSize = 10;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.subscription$ = this.searchSubject.pipe(
      debounceTime(300),
      switchMap((term: string) => {
        if (!term && !this.selectedCategory) {
          this.results = [];
          this.pagination = null;
          return EMPTY;
        }
        this.currentPage = 1;
        const categoriesToSend: string[] = this.selectedCategory
          ? [this.selectedCategory]
          : [];
        return this.apiService.getSearch(
          term,
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

  onPageChange(event: any): void {
    this.currentPage = event.page + 1;
    this.pageSize = event.rows;
    const categoriesToSend: string[] = this.selectedCategory
      ? [this.selectedCategory]
      : [];
    this.apiService.getSearch(
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
    ).subscribe((response: SearchApiResponse) => {
      this.results = response.data;
      this.pagination = response.pagination;
    });
  }
}
