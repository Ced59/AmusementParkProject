import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkFounder } from '@app/models/parks/park-founder';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  ADMIN_FOUNDERS_STATE_PARK_FOUNDERS_API_SERVICE_PORT,
  AdminFoundersStateParkFoundersApiServicePort
} from './admin-founders-state-data.ports';
interface AdminFoundersViewModel {
  founders: ParkFounder[];
  filteredFounders: ParkFounder[];
  searchQuery: string;
}

@Injectable()
export class AdminFoundersStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminFoundersViewModel>();
  private readonly foundersSignal = signal<ParkFounder[]>([]);
  private readonly searchQuerySignal = signal('');
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly filteredFounders: Signal<ParkFounder[]> = computed(() => this.screenStateStore.data()?.filteredFounders ?? []);
  public readonly pagedFounders: Signal<ParkFounder[]> = computed(() => {
    const page: number = this.currentPageSignal();
    const pageSize: number = this.pageSizeSignal();
    const start: number = (page - 1) * pageSize;
    return this.filteredFounders().slice(start, start + pageSize);
  });
  public readonly totalCount = computed(() => this.filteredFounders().length);

  constructor(
    @Inject(ADMIN_FOUNDERS_STATE_PARK_FOUNDERS_API_SERVICE_PORT) private readonly parkFoundersApiService: AdminFoundersStateParkFoundersApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadFounders(): void {
    const previousData: AdminFoundersViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkFoundersApiService.getAllParkFounders().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (founders: ParkFounder[]) => {
        this.foundersSignal.set(founders);
        this.ensureCurrentPageIsValid();
        this.pushDerivedState();
      },
      error: (error: unknown) => {
        console.error('Error loading founders', error);
        this.screenStateStore.setError('admin.parkFounders.loadError', previousData);
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
    const founders: ParkFounder[] = this.foundersSignal();
    const filteredFounders: ParkFounder[] = this.computeFilteredFounders();

    const viewModel: AdminFoundersViewModel = {
      founders,
      filteredFounders,
      searchQuery: this.searchQuerySignal()
    };

    if (founders.length === 0 || filteredFounders.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    this.screenStateStore.setReady(viewModel);
  }

  private ensureCurrentPageIsValid(): void {
    const pageSize: number = this.pageSizeSignal();
    const totalItems: number = this.computeFilteredFounders().length;
    const totalPages: number = Math.max(Math.ceil(totalItems / pageSize), 1);
    if (this.currentPageSignal() > totalPages) {
      this.currentPageSignal.set(totalPages);
    }
  }

  private computeFilteredFounders(): ParkFounder[] {
    const founders: ParkFounder[] = this.foundersSignal();
    const normalizedQuery: string = this.searchQuerySignal().trim().toLowerCase();

    if (normalizedQuery.length === 0) {
      return founders;
    }

    return founders.filter((founder: ParkFounder) => {
      return founder.name.toLowerCase().includes(normalizedQuery)
        || (founder.occupation ?? '').toLowerCase().includes(normalizedQuery)
        || (founder.birthPlace ?? '').toLowerCase().includes(normalizedQuery)
        || (founder.nationalityCountryCode ?? '').toLowerCase().includes(normalizedQuery);
    });
  }
}
