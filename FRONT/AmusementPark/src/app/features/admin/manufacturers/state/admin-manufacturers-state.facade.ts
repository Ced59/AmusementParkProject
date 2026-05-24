import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminManufacturersViewModel {
  manufacturers: AttractionManufacturer[];
  filteredManufacturers: AttractionManufacturer[];
  searchQuery: string;
  adminReviewStatus: AdminReviewStatus | null;
}

@Injectable()
export class AdminManufacturersStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminManufacturersViewModel>();
  private readonly manufacturersSignal = signal<AttractionManufacturer[]>([]);
  private readonly searchQuerySignal = signal('');
  private readonly adminReviewStatusFilterSignal = signal<AdminReviewStatus | null>(null);
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);
  private readonly selectedManufacturerIdsSignal = signal<string[]>([]);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly adminReviewStatusFilter = this.adminReviewStatusFilterSignal.asReadonly();
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly selectedManufacturerIds = this.selectedManufacturerIdsSignal.asReadonly();
  public readonly selectedCount = computed(() => this.selectedManufacturerIdsSignal().length);
  public readonly filteredManufacturers: Signal<AttractionManufacturer[]> = computed(() => this.screenStateStore.data()?.filteredManufacturers ?? []);
  public readonly pagedManufacturers: Signal<AttractionManufacturer[]> = computed(() => {
    const page: number = this.currentPageSignal();
    const pageSize: number = this.pageSizeSignal();
    const start: number = (page - 1) * pageSize;
    return this.filteredManufacturers().slice(start, start + pageSize);
  });
  public readonly totalCount = computed(() => this.filteredManufacturers().length);

  constructor(
    private readonly manufacturersApiService: ManufacturersApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadManufacturers(): void {
    const previousData: AdminManufacturersViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.manufacturersApiService.getAllAttractionManufacturers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (manufacturers: AttractionManufacturer[]) => {
        this.manufacturersSignal.set(manufacturers.map((manufacturer: AttractionManufacturer) => ({ ...manufacturer, adminReviewStatus: manufacturer.adminReviewStatus ?? 'ToReview' })));
        this.ensureCurrentPageIsValid();
        this.pruneSelection();
        this.pushDerivedState();
      },
      error: (error: unknown) => {
        console.error('Error loading manufacturers', error);
        this.screenStateStore.setError('admin.manufacturers.loadError', previousData);
      }
    });
  }

  setSearchQuery(searchQuery: string): void {
    this.searchQuerySignal.set(searchQuery);
    this.currentPageSignal.set(1);
    this.pushDerivedState();
  }

  setAdminReviewStatusFilter(adminReviewStatus: AdminReviewStatus | null): void {
    this.adminReviewStatusFilterSignal.set(adminReviewStatus);
    this.currentPageSignal.set(1);
    this.pushDerivedState();
  }

  setPage(page: number, pageSize: number): void {
    this.pageSizeSignal.set(pageSize);
    this.currentPageSignal.set(Math.max(page, 1));
    this.ensureCurrentPageIsValid();
    this.pushDerivedState();
  }

  setManufacturerSelection(manufacturerId: string, selected: boolean): void {
    const currentSelection: string[] = this.selectedManufacturerIdsSignal();
    if (selected) {
      this.selectedManufacturerIdsSignal.set([...new Set([...currentSelection, manufacturerId])]);
      return;
    }

    this.selectedManufacturerIdsSignal.set(currentSelection.filter((id: string) => id !== manufacturerId));
  }

  setCurrentPageSelection(selected: boolean): void {
    const currentPageIds: string[] = this.pagedManufacturers()
      .map((manufacturer: AttractionManufacturer) => manufacturer.id)
      .filter((id: string | undefined): id is string => !!id);

    if (selected) {
      this.selectedManufacturerIdsSignal.set([...new Set([...this.selectedManufacturerIdsSignal(), ...currentPageIds])]);
      return;
    }

    this.selectedManufacturerIdsSignal.set(this.selectedManufacturerIdsSignal().filter((id: string) => !currentPageIds.includes(id)));
  }

  clearSelection(): void {
    this.selectedManufacturerIdsSignal.set([]);
  }

  updateSelectedReviewStatus(adminReviewStatus: AdminReviewStatus): void {
    const ids: string[] = this.selectedManufacturerIdsSignal();
    if (ids.length === 0) {
      return;
    }

    this.manufacturersApiService.updateAttractionManufacturersBulkReviewStatus({ ids, adminReviewStatus }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.manufacturersSignal.update((manufacturers: AttractionManufacturer[]) => manufacturers.map((manufacturer: AttractionManufacturer) => {
          if (!manufacturer.id || !ids.includes(manufacturer.id)) {
            return manufacturer;
          }

          return { ...manufacturer, adminReviewStatus };
        }));
        this.clearSelection();
        this.pushDerivedState();
      },
      error: (error: unknown) => {
        console.error('Error updating manufacturers review status', error);
      }
    });
  }

  private pushDerivedState(): void {
    const manufacturers: AttractionManufacturer[] = this.manufacturersSignal();
    const filteredManufacturers: AttractionManufacturer[] = this.computeFilteredManufacturers();

    const viewModel: AdminManufacturersViewModel = {
      manufacturers,
      filteredManufacturers,
      searchQuery: this.searchQuerySignal(),
      adminReviewStatus: this.adminReviewStatusFilterSignal()
    };

    if (manufacturers.length === 0 || filteredManufacturers.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    this.screenStateStore.setReady(viewModel);
  }

  private ensureCurrentPageIsValid(): void {
    const pageSize: number = this.pageSizeSignal();
    const totalItems: number = this.computeFilteredManufacturers().length;
    const totalPages: number = Math.max(Math.ceil(totalItems / pageSize), 1);
    if (this.currentPageSignal() > totalPages) {
      this.currentPageSignal.set(totalPages);
    }
  }

  private computeFilteredManufacturers(): AttractionManufacturer[] {
    const manufacturers: AttractionManufacturer[] = this.manufacturersSignal();
    const normalizedQuery: string = this.searchQuerySignal().trim().toLowerCase();
    const adminReviewStatus: AdminReviewStatus | null = this.adminReviewStatusFilterSignal();

    return manufacturers.filter((manufacturer: AttractionManufacturer) => {
      const matchesQuery: boolean = normalizedQuery.length === 0 || manufacturer.name.toLowerCase().includes(normalizedQuery);
      const matchesStatus: boolean = adminReviewStatus === null || (manufacturer.adminReviewStatus ?? 'ToReview') === adminReviewStatus;
      return matchesQuery && matchesStatus;
    });
  }

  private pruneSelection(): void {
    const validIds: Set<string> = new Set(this.manufacturersSignal().map((manufacturer: AttractionManufacturer) => manufacturer.id).filter((id: string | undefined): id is string => !!id));
    this.selectedManufacturerIdsSignal.set(this.selectedManufacturerIdsSignal().filter((id: string) => validIds.has(id)));
  }
}
