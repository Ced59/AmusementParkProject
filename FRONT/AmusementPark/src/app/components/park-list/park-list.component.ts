import { Component, OnDestroy, OnInit, signal } from '@angular/core';
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
  styleUrls: ['./park-list.component.scss']
})
export class ParkListComponent implements OnInit, OnDestroy {
  readonly parks = signal<Park[]>([]);
  readonly pagination = signal<Pagination | null>(null);
  readonly pageState = signal<ViewState>(ViewState.Loading);

  currentLang = 'en';
  searchTerm = '';
  pageSize = 9;

  private readonly searchSubject = new Subject<string>();
  private searchSubscription?: Subscription;

  constructor(
    private readonly apiService: ApiService,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.translationService.getCurrentLang() || 'en';

    this.searchSubscription = this.searchSubject.pipe(
      debounceTime(300)
    ).subscribe((term: string) => {
      this.loadParks(1, this.pageSize, term);
    });

    this.loadParks(1, this.pageSize, this.searchTerm);
  }

  ngOnDestroy(): void {
    this.searchSubscription?.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.searchTerm = value.trim();
    this.searchSubject.next(this.searchTerm);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.searchSubject.next(this.searchTerm);
  }

  onPageChange(event: { page: number; rows: number }): void {
    this.pageSize = event.rows;
    this.loadParks(event.page + 1, event.rows, this.searchTerm);
  }

  private loadParks(page: number, size: number, term: string): void {
    this.pageState.set(ViewState.Loading);

    const request$ = term
      ? this.apiService.searchParks(term, page, size)
      : this.apiService.getParksPaginated(page, size);

    request$.subscribe({
      next: (response) => {
        const parks = response.data ?? [];
        this.parks.set(parks);
        this.pagination.set(response.pagination ?? null);
        this.pageState.set(parks.length > 0 ? ViewState.Ready : ViewState.Empty);
      },
      error: (error: unknown) => {
        console.error('Error fetching parks:', error);
        this.pageState.set(ViewState.Error);
      }
    });
  }
}
