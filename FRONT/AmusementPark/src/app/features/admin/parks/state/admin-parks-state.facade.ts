import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult, AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkOpeningHoursAdminFilter } from '@app/models/parks/park-opening-hours';
import { ParkType } from '@app/models/parks/park-type';
import { Pagination } from '@app/models/shared/pagination';
import { ParkAdminListFilters, ParkAdminListSort, ParkAdminListSortDirection, ParkAdminListSortField } from '@data-access/parks/parks-api-endpoints';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  ADMIN_PARKS_STATE_PARKS_API_SERVICE_PORT,
  AdminParksStateParksApiServicePort
} from './admin-parks-state-data.ports';
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
  private readonly audienceClassificationFilterSignal = signal<ParkAudienceClassificationFilter | null>(null);
  private readonly countryCodeFilterSignal = signal('');
  private readonly validCoordinatesFilterSignal = signal<boolean | null>(null);
  private readonly openingHoursFilterSignal = signal<ParkOpeningHoursAdminFilter>('all');
  private readonly sortFieldSignal = signal<ParkAdminListSortField>('default');
  private readonly sortDirectionSignal = signal<ParkAdminListSortDirection>('asc');

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
  public readonly audienceClassificationFilter = this.audienceClassificationFilterSignal.asReadonly();
  public readonly countryCodeFilter = this.countryCodeFilterSignal.asReadonly();
  public readonly validCoordinatesFilter = this.validCoordinatesFilterSignal.asReadonly();
  public readonly openingHoursFilter = this.openingHoursFilterSignal.asReadonly();
  public readonly sortField = this.sortFieldSignal.asReadonly();
  public readonly sortDirection = this.sortDirectionSignal.asReadonly();
  public readonly filters = computed<ParkAdminListFilters>(() => ({
    isVisible: this.visibilityFilterSignal(),
    adminReviewStatus: this.adminReviewStatusFilterSignal(),
    type: this.typeFilterSignal(),
    audienceClassification: this.audienceClassificationFilterSignal(),
    countryCode: this.countryCodeFilterSignal().trim() || null,
    hasValidCoordinates: this.validCoordinatesFilterSignal(),
    openingHoursStatus: this.openingHoursFilterSignal()
  }));
  public readonly sort = computed<ParkAdminListSort>(() => ({
    sortBy: this.sortFieldSignal(),
    sortDirection: this.sortDirectionSignal()
  }));

  constructor(@Inject(ADMIN_PARKS_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: AdminParksStateParksApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadParks(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const previousData: AdminParksViewModel | undefined = this.screenStateStore.data();
    const trimmedQuery: string = this.searchQuerySignal().trim();
    const filters: ParkAdminListFilters = this.filters();
    const sort: ParkAdminListSort = this.sort();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const handleResponse = (response: ParksApiResponse, currentPage: number, currentSize: number) => {
      const parks: Park[] = (response.data ?? []).map((park: Park) => ({
        ...park,
        isVisible: park.isVisible ?? false,
        adminReviewStatus: park.adminReviewStatus ?? 'ToReview'
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
      this.parksApiService.searchParks(trimmedQuery, page, size, false, null, filters, { sort }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

    this.parksApiService.getParksPaginated(page, size, false, null, filters, { sort }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.audienceClassificationFilterSignal.set(filters.audienceClassification ?? null);
    this.countryCodeFilterSignal.set(filters.countryCode ?? '');
    this.validCoordinatesFilterSignal.set(filters.hasValidCoordinates ?? null);
    this.openingHoursFilterSignal.set(filters.openingHoursStatus ?? 'all');
    this.loadParks(1, this.pageSizeSignal());
  }

  updateSort(sortField: ParkAdminListSortField, sortDirection: ParkAdminListSortDirection): boolean {
    const changed: boolean = this.sortFieldSignal() !== sortField || this.sortDirectionSignal() !== sortDirection;
    this.sortFieldSignal.set(sortField);
    this.sortDirectionSignal.set(sortDirection);
    return changed;
  }

  updateBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    return this.parksApiService.updateParksBulkAdministration(request);
  }

  makeFilteredValidCoordinateParksVisible(): Observable<BulkAdministrationUpdateResult> {
    const filters: ParkAdminListFilters = this.filters();

    return this.parksApiService.updateParksBulkAdministration({
      ids: [],
      isVisible: true,
      adminReviewStatus: null,
      filterIsVisible: null,
      filterAdminReviewStatus: filters.adminReviewStatus,
      filterType: filters.type,
      filterAudienceClassification: filters.audienceClassification,
      filterCountryCode: filters.countryCode,
      filterHasValidCoordinates: true
    });
  }
}
