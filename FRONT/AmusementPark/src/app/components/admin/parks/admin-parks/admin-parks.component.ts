import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Park } from '../../../../models/parks/park';
import { ParksApiResponse } from '../../../../models/parks/parks_api_response';
import { Pagination } from '../../../../models/shared/pagination';
import { ViewState } from '../../../../models/shared/view-state';
import { ApiService } from '../../../../services/api.service';

@Component({
  selector: 'app-admin-parks',
  templateUrl: './admin-parks.component.html',
  styleUrls: ['./admin-parks.component.scss']
})
export class AdminParksComponent implements OnInit {
  readonly parks = signal<Park[]>([]);
  readonly pagination = signal<Pagination | null>(null);
  readonly viewState = signal<ViewState>(ViewState.Loading);

  totalRecords = 0;
  pageSize = 10;
  currentPage = 1;
  searchQuery = '';

  constructor(
    private readonly apiService: ApiService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {
  }

  ngOnInit(): void {
    this.loadParks(this.currentPage, this.pageSize);
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadParks(this.currentPage, this.pageSize);
  }

  clearSearch(): void {
    if (!this.searchQuery.trim()) {
      return;
    }

    this.searchQuery = '';
    this.currentPage = 1;
    this.loadParks(this.currentPage, this.pageSize);
  }

  onPageChanged(event: any): void {
    const rows = event.rows ?? this.pageSize;
    const first = event.first ?? 0;
    const page = Math.floor(first / rows) + 1;
    this.loadParks(page, rows);
  }

  onVisibilityChange(park: Park): void {
    if (!park.id) {
      return;
    }

    const newValue = !!park.isVisible;

    this.apiService.updateParkVisibility(park.id, newValue).subscribe({
      error: (err: unknown) => {
        console.error('Error updating park visibility', err);
        park.isVisible = !newValue;
      }
    });
  }

  private loadParks(page: number, size: number): void {
    this.viewState.set(ViewState.Loading);

    const trimmedQuery = this.searchQuery.trim();
    const request$ = trimmedQuery.length > 0
      ? this.apiService.searchParks(trimmedQuery, page, size)
      : this.apiService.getParksPaginated(page, size);

    request$.subscribe({
      next: (response: ParksApiResponse) => {
        const parks = (response.data ?? []).map((park) => ({
          ...park,
          isVisible: park.isVisible ?? false
        }));

        const pagination = response.pagination ?? null;

        this.parks.set(parks);
        this.pagination.set(pagination);
        this.totalRecords = pagination?.totalItems ?? parks.length;
        this.pageSize = pagination?.itemsPerPage ?? size;
        this.currentPage = pagination?.currentPage ?? page;
        this.viewState.set(ViewState.Ready);
      },
      error: (err: unknown) => {
        console.error('Error loading parks', err);
        this.viewState.set(ViewState.Error);
      }
    });
  }
}
