import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

import { Park } from '../../models/parks/park';
import { Pagination } from '../../models/shared/pagination';
import { ViewState } from '../../models/shared/view-state';
import { ApiService } from '../../services/api.service';
import { TranslationService } from '../../services/translation.service';

@Component({
  selector: 'app-park-list',
  templateUrl: './park-list.component.html',
  styleUrls: ['./park-list.component.scss'],
  standalone: false
})
export class ParkListComponent implements OnInit, OnDestroy {
  parks: Park[] = [];
  pagination: Pagination | null = null;
  pageState: ViewState = ViewState.Loading;

  currentLang: string = 'en';
  searchTerm: string = '';
  pageSize: number = 9;

  private readonly searchSubject: Subject<string> = new Subject<string>();
  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';

    this.subscriptions.add(
      this.searchSubject.pipe(debounceTime(300)).subscribe((term: string) => {
        this.loadParks(1, this.pageSize, term);
      })
    );

    this.loadParks(1, this.pageSize, this.searchTerm);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.searchSubject.next(this.searchTerm);
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.pageSize;
    this.pageSize = rows;
    this.loadParks(page, rows, this.searchTerm);
  }

  private loadParks(page: number, size: number, term: string): void {
    this.pageState = ViewState.Loading;

    const request$ = term
      ? this.apiService.searchParks(term, page, size)
      : this.apiService.getParksPaginated(page, size);

    this.subscriptions.add(
      request$.subscribe({
        next: (response) => {
          this.parks = response.data ?? [];
          this.pagination = response.pagination ?? null;
          this.pageState = this.parks.length > 0 ? ViewState.Ready : ViewState.Empty;
        },
        error: (error: unknown) => {
          console.error('Error fetching parks:', error);
          this.pageState = ViewState.Error;
        }
      })
    );
  }
}
