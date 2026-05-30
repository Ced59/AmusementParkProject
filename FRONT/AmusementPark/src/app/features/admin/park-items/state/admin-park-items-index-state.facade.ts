import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, expand, forkJoin, map, reduce, shareReplay } from 'rxjs';

import { AdminReviewStatus, BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { Pagination } from '@app/models/shared/pagination';
import { ParkItemAdminListFilters, ParkItemAdminListSort, ParkItemAdminSortField } from '@data-access/park-items/park-items-api-endpoints';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminParkItemsIndexViewModel {
  rows: ParkItemAdminRow[];
  parkOptions: { label: string; value: string | null }[];
  totalRecords: number;
}

@Injectable()
export class AdminParkItemsIndexStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminParkItemsIndexViewModel>();
  private readonly allParksLabelSignal = signal('');
  private readonly selectedParkIdSignal = signal<string | null>(null);
  private readonly searchTermSignal = signal('');
  private readonly visibilityFilterSignal = signal<boolean | null>(null);
  private readonly adminReviewStatusFilterSignal = signal<AdminReviewStatus | null>(null);
  private readonly categoryFilterSignal = signal<ParkItemCategory | null>(null);
  private readonly typeFilterSignal = signal<ParkItemType | null>(null);
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);
  private readonly sortFieldSignal = signal<ParkItemAdminSortField>('default');
  private readonly sortOrderSignal = signal<1 | -1>(1);
  private parksRequest$: Observable<Park[]> | null = null;

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly rows: Signal<ParkItemAdminRow[]> = computed(() => this.screenStateStore.data()?.rows ?? []);
  public readonly parkOptions: Signal<Array<{ label: string; value: string | null }>> = computed(() => this.screenStateStore.data()?.parkOptions ?? []);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly selectedParkId: Signal<string | null> = this.selectedParkIdSignal.asReadonly();
  public readonly searchTerm: Signal<string> = this.searchTermSignal.asReadonly();
  public readonly visibilityFilter = this.visibilityFilterSignal.asReadonly();
  public readonly adminReviewStatusFilter = this.adminReviewStatusFilterSignal.asReadonly();
  public readonly categoryFilter = this.categoryFilterSignal.asReadonly();
  public readonly typeFilter = this.typeFilterSignal.asReadonly();
  public readonly pageSize: Signal<number> = this.pageSizeSignal.asReadonly();
  public readonly sortField: Signal<ParkItemAdminSortField> = this.sortFieldSignal.asReadonly();
  public readonly sortOrder: Signal<1 | -1> = this.sortOrderSignal.asReadonly();
  public readonly filters = computed<ParkItemAdminListFilters>(() => ({
    isVisible: this.visibilityFilterSignal(),
    adminReviewStatus: this.adminReviewStatusFilterSignal(),
    category: this.categoryFilterSignal(),
    type: this.typeFilterSignal()
  }));
  public readonly sort = computed<ParkItemAdminListSort>(() => ({
    sortBy: this.sortFieldSignal(),
    sortDirection: this.sortOrderSignal() === -1 ? 'desc' : 'asc'
  }));

  constructor(
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  initialize(allParksLabel: string): void {
    this.allParksLabelSignal.set(allParksLabel);
    this.loadData();
  }

  updateFilters(filters: {
    selectedParkId: string | null;
    searchTerm: string;
    isVisible?: boolean | null;
    adminReviewStatus?: AdminReviewStatus | null;
    category?: ParkItemCategory | null;
    type?: ParkItemType | null;
  }): void {
    this.selectedParkIdSignal.set(filters.selectedParkId);
    this.searchTermSignal.set(filters.searchTerm);
    this.visibilityFilterSignal.set(filters.isVisible ?? null);
    this.adminReviewStatusFilterSignal.set(filters.adminReviewStatus ?? null);
    this.categoryFilterSignal.set(filters.category ?? null);
    this.typeFilterSignal.set(filters.type ?? null);
    this.currentPageSignal.set(1);
    this.loadData();
  }

  updateSort(event: { sortBy: ParkItemAdminSortField; sortOrder: 1 | -1 }): void {
    this.sortFieldSignal.set(event.sortBy);
    this.sortOrderSignal.set(event.sortOrder);
    this.currentPageSignal.set(1);
    this.loadData();
  }

  updatePage(event: { page?: number; rows?: number }): void {
    this.currentPageSignal.set((event.page ?? 0) + 1);
    this.pageSizeSignal.set(event.rows ?? this.pageSizeSignal());
    this.loadData();
  }

  updateBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    return this.parkItemsApiService.updateParkItemsBulkAdministration(request);
  }

  loadData(): void {
    const previousData: AdminParkItemsIndexViewModel | undefined = this.screenStateStore.data();

    if (!previousData) {
      this.screenStateStore.setLoading();
    }

    forkJoin({
      rowsResponse: this.parkItemsApiService.getParkItemsPaginated(
        this.currentPageSignal(),
        this.pageSizeSignal(),
        this.selectedParkIdSignal(),
        this.searchTermSignal().trim(),
        this.filters(),
        this.sort()
      ),
      parks: this.getAllParks()
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ rowsResponse, parks }: { rowsResponse: ApiResponse<ParkItemAdminRow>; parks: Park[] }) => {
        const rows: ParkItemAdminRow[] = (rowsResponse.data ?? []).map((row: ParkItemAdminRow) => ({
          ...row,
          adminReviewStatus: row.adminReviewStatus ?? 'ToReview'
        }));
        const validParks: Park[] = parks.filter((park: Park) => !!park.id);
        const viewModel: AdminParkItemsIndexViewModel = {
          rows,
          totalRecords: rowsResponse.pagination?.totalItems ?? rows.length,
          parkOptions: [
            { label: this.allParksLabelSignal(), value: null },
            ...validParks.map((park: Park) => ({
              label: park.name ?? '',
              value: park.id ?? null
            }))
          ]
        };

        if (viewModel.totalRecords === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading park items index', error);
        this.screenStateStore.setError('admin.parkItems.empty', previousData);
      }
    });
  }

  private getAllParks(): Observable<Park[]> {
    if (this.parksRequest$) {
      return this.parksRequest$;
    }

    this.parksRequest$ = this.parksApiService.getParksPaginated(1, 100).pipe(
      expand((response: ParksApiResponse) => {
        const pagination: Pagination | undefined = response.pagination;
        const currentPage: number = pagination?.currentPage ?? 1;
        const totalPages: number = pagination?.totalPages ?? 1;

        if (currentPage >= totalPages) {
          return EMPTY;
        }

        return this.parksApiService.getParksPaginated(currentPage + 1, 100);
      }),
      map((response: ParksApiResponse) => response.data ?? []),
      reduce((allParks: Park[], parks: Park[]) => [...allParks, ...parks], [] as Park[]),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    return this.parksRequest$;
  }
}
