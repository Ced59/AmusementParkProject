import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminManufacturersViewModel {
  manufacturers: AttractionManufacturer[];
  filteredManufacturers: AttractionManufacturer[];
  searchQuery: string;
}

@Injectable()
export class AdminManufacturersStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminManufacturersViewModel>();
  private readonly manufacturersSignal = signal<AttractionManufacturer[]>([]);
  private readonly searchQuerySignal = signal('');
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly filteredManufacturers: Signal<AttractionManufacturer[]> = computed(() => this.screenStateStore.data()?.filteredManufacturers ?? []);
  public readonly pagedManufacturers: Signal<AttractionManufacturer[]> = computed(() => {
    const page: number = this.currentPageSignal();
    const pageSize: number = this.pageSizeSignal();
    const start: number = (page - 1) * pageSize;
    return this.filteredManufacturers().slice(start, start + pageSize);
  });
  public readonly totalCount = computed(() => this.filteredManufacturers().length);

  constructor(private readonly manufacturersApiService: ManufacturersApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadManufacturers(): void {
    const previousData: AdminManufacturersViewModel | undefined = this.screenStateStore.data();

    this.screenStateStore.setLoading(previousData);

    this.manufacturersApiService.getAllAttractionManufacturers().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (manufacturers: AttractionManufacturer[]) => {
        this.manufacturersSignal.set(manufacturers);
        this.ensureCurrentPageIsValid();
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

  setPage(page: number, pageSize: number): void {
    this.pageSizeSignal.set(pageSize);
    this.currentPageSignal.set(Math.max(page, 1));
    this.ensureCurrentPageIsValid();
    this.pushDerivedState();
  }

  private pushDerivedState(): void {
    const manufacturers: AttractionManufacturer[] = this.manufacturersSignal();
    const normalizedQuery: string = this.searchQuerySignal().trim().toLowerCase();
    const filteredManufacturers: AttractionManufacturer[] = normalizedQuery.length === 0
      ? [...manufacturers]
      : manufacturers.filter((manufacturer: AttractionManufacturer) => manufacturer.name.toLowerCase().includes(normalizedQuery));

    const viewModel: AdminManufacturersViewModel = {
      manufacturers,
      filteredManufacturers,
      searchQuery: this.searchQuerySignal()
    };

    if (manufacturers.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    if (filteredManufacturers.length === 0) {
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
    if (normalizedQuery.length === 0) {
      return [...manufacturers];
    }

    return manufacturers.filter((manufacturer: AttractionManufacturer) => manufacturer.name.toLowerCase().includes(normalizedQuery));
  }
}
