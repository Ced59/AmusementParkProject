import {
  Injectable,
  Signal,
  computed,
  DestroyRef,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize, forkJoin, shareReplay } from 'rxjs';

import {
  AdminReviewStatus,
  BulkAdministrationUpdateRequest,
  BulkAdministrationUpdateResult,
} from '@app/models/admin/admin-review-status';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkZone } from '@app/models/parks/park-zone';
import { ApiResponse } from '@app/models/shared/api_reponse';
import {
  ParkItemAdminListFilters,
  ParkItemAdminListSort,
  ParkItemAdminSortField,
} from '@data-access/park-items/park-items-api-endpoints';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminParkItemsViewModel {
  items: ParkItemAdminRow[];
  zones: ParkZone[];
  totalRecords: number;
}

@Injectable()
export class AdminParkItemsStateFacade {
  private readonly screenStateStore =
    new SignalScreenStateStore<AdminParkItemsViewModel>();
  private readonly searchTermSignal = signal('');
  private readonly visibilityFilterSignal = signal<boolean | null>(null);
  private readonly adminReviewStatusFilterSignal =
    signal<AdminReviewStatus | null>(null);
  private readonly categoryFilterSignal = signal<ParkItemCategory | null>(null);
  private readonly typeFilterSignal = signal<ParkItemType | null>(null);
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);
  private readonly sortFieldSignal = signal<ParkItemAdminSortField>('default');
  private readonly sortOrderSignal = signal<1 | -1>(1);
  private readonly zoneRequestsByParkId = new Map<
    string,
    Observable<ParkZone[]>
  >();
  private readonly inFlightRequestKeys = new Set<string>();
  private lastLoadedRequestKey: string | null = null;
  private activeRequestKey: string | null = null;

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly items: Signal<ParkItemAdminRow[]> = computed(
    () => this.screenStateStore.data()?.items ?? [],
  );
  public readonly zones: Signal<ParkZone[]> = computed(
    () => this.screenStateStore.data()?.zones ?? [],
  );
  public readonly totalRecords: Signal<number> = computed(
    () => this.screenStateStore.data()?.totalRecords ?? 0,
  );
  public readonly searchTerm: Signal<string> =
    this.searchTermSignal.asReadonly();
  public readonly visibilityFilter = this.visibilityFilterSignal.asReadonly();
  public readonly adminReviewStatusFilter =
    this.adminReviewStatusFilterSignal.asReadonly();
  public readonly categoryFilter = this.categoryFilterSignal.asReadonly();
  public readonly typeFilter = this.typeFilterSignal.asReadonly();
  public readonly currentPage: Signal<number> =
    this.currentPageSignal.asReadonly();
  public readonly pageSize: Signal<number> = this.pageSizeSignal.asReadonly();
  public readonly sortField: Signal<ParkItemAdminSortField> =
    this.sortFieldSignal.asReadonly();
  public readonly sortOrder: Signal<1 | -1> = this.sortOrderSignal.asReadonly();
  public readonly filters = computed<ParkItemAdminListFilters>(() => ({
    isVisible: this.visibilityFilterSignal(),
    adminReviewStatus: this.adminReviewStatusFilterSignal(),
    category: this.categoryFilterSignal(),
    type: this.typeFilterSignal(),
  }));
  public readonly sort = computed<ParkItemAdminListSort>(() => ({
    sortBy: this.sortFieldSignal(),
    sortDirection: this.sortOrderSignal() === -1 ? 'desc' : 'asc',
  }));

  constructor(
    private readonly parkZonesApiService: ParkZonesApiService,
    private readonly parkItemsApiService: ParkItemsApiService,
    private readonly destroyRef: DestroyRef,
  ) {}

  loadData(parkId: string, forceReload: boolean = false): void {
    if (!parkId) {
      return;
    }

    const requestKey: string = this.buildRequestKey(parkId);
    if (
      !forceReload &&
      (this.inFlightRequestKeys.has(requestKey) ||
        this.lastLoadedRequestKey === requestKey)
    ) {
      return;
    }

    const previousData: AdminParkItemsViewModel | undefined =
      this.screenStateStore.data();
    this.activeRequestKey = requestKey;
    this.inFlightRequestKeys.add(requestKey);
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      zones: this.getParkZones(parkId),
      rowsResponse: this.parkItemsApiService.getParkItemsPaginated(
        this.currentPageSignal(),
        this.pageSizeSignal(),
        parkId,
        this.searchTermSignal().trim(),
        this.filters(),
        this.sort(),
      ),
    })
      .pipe(
        finalize(() => this.inFlightRequestKeys.delete(requestKey)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({
          zones,
          rowsResponse,
        }: {
          zones: ParkZone[];
          rowsResponse: ApiResponse<ParkItemAdminRow>;
        }) => {
          if (this.activeRequestKey !== requestKey) {
            return;
          }

          const items: ParkItemAdminRow[] = (rowsResponse.data ?? []).map(
            (item: ParkItemAdminRow) => ({
              ...item,
              adminReviewStatus: item.adminReviewStatus ?? 'ToReview',
            }),
          );
          const viewModel: AdminParkItemsViewModel = {
            zones,
            items,
            totalRecords: rowsResponse.pagination?.totalItems ?? items.length,
          };

          this.lastLoadedRequestKey = requestKey;

          if (viewModel.totalRecords === 0) {
            this.screenStateStore.setEmpty(viewModel);
            return;
          }

          this.screenStateStore.setReady(viewModel);
        },
        error: (error: unknown) => {
          if (this.activeRequestKey !== requestKey) {
            return;
          }

          console.error('Error loading park items', error);
          this.screenStateStore.setError('common.errorMessage', previousData);
        },
      });
  }

  updateFilters(
    parkId: string,
    filters: {
      searchTerm: string;
      isVisible?: boolean | null;
      adminReviewStatus?: AdminReviewStatus | null;
      category?: ParkItemCategory | null;
      type?: ParkItemType | null;
    },
  ): void {
    this.searchTermSignal.set(filters.searchTerm);
    this.visibilityFilterSignal.set(filters.isVisible ?? null);
    this.adminReviewStatusFilterSignal.set(filters.adminReviewStatus ?? null);
    this.categoryFilterSignal.set(filters.category ?? null);
    this.typeFilterSignal.set(filters.type ?? null);
    this.currentPageSignal.set(1);
    this.loadData(parkId);
  }

  updateSort(
    parkId: string,
    event: { sortBy: ParkItemAdminSortField; sortOrder: 1 | -1 },
  ): void {
    this.sortFieldSignal.set(event.sortBy);
    this.sortOrderSignal.set(event.sortOrder);
    this.currentPageSignal.set(1);
    this.loadData(parkId);
  }

  updatePage(
    parkId: string,
    event: { page?: number; rows?: number; first?: number },
  ): void {
    const rows: number = event.rows ?? this.pageSizeSignal();
    const pageIndex: number =
      event.page ?? Math.floor((event.first ?? 0) / Math.max(rows, 1));

    this.currentPageSignal.set(pageIndex + 1);
    this.pageSizeSignal.set(event.rows ?? this.pageSizeSignal());
    this.loadData(parkId);
  }

  updateBulkAdministration(
    request: BulkAdministrationUpdateRequest,
  ): Observable<BulkAdministrationUpdateResult> {
    return this.parkItemsApiService.updateParkItemsBulkAdministration(request);
  }

  invalidateCurrentPage(): void {
    this.lastLoadedRequestKey = null;
  }

  private getParkZones(parkId: string): Observable<ParkZone[]> {
    const normalizedParkId: string = parkId.trim();
    const existingRequest: Observable<ParkZone[]> | undefined =
      this.zoneRequestsByParkId.get(normalizedParkId);
    if (existingRequest) {
      return existingRequest;
    }

    const request$: Observable<ParkZone[]> = this.parkZonesApiService
      .getParkZonesByParkId(normalizedParkId)
      .pipe(shareReplay({ bufferSize: 1, refCount: false }));
    this.zoneRequestsByParkId.set(normalizedParkId, request$);
    return request$;
  }

  private buildRequestKey(parkId: string): string {
    return JSON.stringify({
      parkId: parkId.trim(),
      page: this.currentPageSignal(),
      pageSize: this.pageSizeSignal(),
      search: this.searchTermSignal().trim(),
      filters: this.filters(),
      sort: this.sort(),
    });
  }
}
