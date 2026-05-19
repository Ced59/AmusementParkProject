import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult, AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParkType } from '@app/models/parks/park-type';
import { Pagination } from '@app/models/shared/pagination';
import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminParksViewModel {
  parks: Park[];
  pagination: Pagination | null;
  totalRecords: number;
  currentPage: number;
  pageSize: number;
  searchQuery: string;
}

@Injectable()
export class AdminParksStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParksViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(10);
  private readonly searchQuerySignal = signal('');
  private readonly visibilityFilterSignal = signal<boolean | null>(null);
  private readonly adminReviewStatusFilterSignal = signal<AdminReviewStatus | null>(null);
  private readonly typeFilterSignal = signal<ParkType | null>(null);
  private readonly countryCodeFilterSignal = signal('');

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly parks: Signal<Park[]> = computed(() => this.screenStateStore.data()?.parks ?? []);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly visibilityFilter = this.visibilityFilterSignal.asReadonly();
  public readonly adminReviewStatusFilter = this.adminReviewStatusFilterSignal.asReadonly();
  public readonly typeFilter = this.typeFilterSignal.asReadonly();
  public readonly countryCodeFilter = this.countryCodeFilterSignal.asReadonly();
  public readonly filters = computed<ParkAdminListFilters>(() => ({
    isVisible: this.visibilityFilterSignal(),
    adminReviewStatus: this.adminReviewStatusFilterSignal(),
    type: this.typeFilterSignal(),
    countryCode: this.countryCodeFilterSignal().trim() || null
  }));

  constructor(private readonly parksApiService: ParksApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadParks(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const previousData: AdminParksViewModel | undefined = this.screenStateStore.data();
    const trimmedQuery: string = this.searchQuerySignal().trim();
    const filters: ParkAdminListFilters = this.filters();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const handleResponse = (response: ParksApiResponse, currentPage: number, currentSize: number) => {
      const parks: Park[] = (response.data ?? []).map((park: Park) => ({
        ...park,
        isVisible: park.isVisible ?? false,
        adminReviewStatus: park.adminReviewStatus ?? 'Ready'
      }));
      const pagination: Pagination | null = response.pagination ?? null;
      const viewModel: AdminParksViewModel = {
        parks,
        pagination,
        totalRecords: pagination?.totalItems ?? parks.length,
        pageSize: pagination?.itemsPerPage ?? currentSize,
        currentPage: pagination?.currentPage ?? currentPage,
        searchQuery: this.searchQuerySignal()
      };

      this.currentPageSignal.set(viewModel.currentPage);
      this.pageSizeSignal.set(viewModel.pageSize);

      if (viewModel.totalRecords === 0) {
        this.screenStateStore.setEmpty(viewModel);
        return;
      }

      this.screenStateStore.setReady(viewModel);
    };

    if (trimmedQuery.length > 0) {
      this.parksApiService.searchParks(trimmedQuery, page, size, false, null, filters).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (response: ParksApiResponse) => {
          handleResponse(response, page, size);
        },
        error: (error: unknown) => {
          console.error('Error searching parks', error);
          this.screenStateStore.setError('admin.parks.loadError', previousData);
        }
      });
      return;
    }

    this.parksApiService.getParksPaginated(page, size, false, null, filters).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: ParksApiResponse) => {
        handleResponse(response, page, size);
      },
      error: (error: unknown) => {
        console.error('Error loading parks', error);
        this.screenStateStore.setError('admin.parks.loadError', previousData);
      }
    });
  }

  setSearchQuery(searchQuery: string): void {
    this.searchQuerySignal.set(searchQuery);
  }

  clearSearchQuery(): void {
    this.searchQuerySignal.set('');
  }

  updateFilters(filters: ParkAdminListFilters): void {
    this.visibilityFilterSignal.set(filters.isVisible ?? null);
    this.adminReviewStatusFilterSignal.set(filters.adminReviewStatus ?? null);
    this.typeFilterSignal.set(filters.type ?? null);
    this.countryCodeFilterSignal.set(filters.countryCode ?? '');
    this.loadParks(1, this.pageSizeSignal());
  }

  updateBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    return this.parksApiService.updateParksBulkAdministration(request);
  }
}
