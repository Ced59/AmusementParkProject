import { Injectable, Signal, computed, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Park } from '@app/models/parks/park';
import { Pagination } from '@app/models/shared/pagination';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ParksApiService } from '@data-access/parks/parks-api.service';
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

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly parks: Signal<Park[]> = computed(() => this.screenStateStore.data()?.parks ?? []);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly searchQuery = this.searchQuerySignal.asReadonly();

  constructor(private readonly parksApiService: ParksApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadParks(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const previousData: AdminParksViewModel | undefined = this.screenStateStore.data();
    const trimmedQuery: string = this.searchQuerySignal().trim();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const handleResponse = (response: ParksApiResponse, currentPage: number, currentSize: number) => {
      const parks: Park[] = (response.data ?? []).map((park: Park) => ({
        ...park,
        isVisible: park.isVisible ?? false
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
      this.parksApiService.searchParks(trimmedQuery, page, size).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

    this.parksApiService.getParksPaginated(page, size).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
}
