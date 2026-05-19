import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminOperatorsViewModel {
  operators: ParkOperator[];
  filteredOperators: ParkOperator[];
  searchQuery: string;
}

@Injectable()
export class AdminOperatorsStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminOperatorsViewModel>();
  private readonly operatorsSignal = signal<ParkOperator[]>([]);
  private readonly searchQuerySignal = signal('');
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly filteredOperators: Signal<ParkOperator[]> = computed(() => this.screenStateStore.data()?.filteredOperators ?? []);
  public readonly pagedOperators: Signal<ParkOperator[]> = computed(() => {
    const page: number = this.currentPageSignal();
    const pageSize: number = this.pageSizeSignal();
    const start: number = (page - 1) * pageSize;
    return this.filteredOperators().slice(start, start + pageSize);
  });
  public readonly totalCount = computed(() => this.filteredOperators().length);

  constructor(private readonly parkOperatorsApiService: ParkOperatorsApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadOperators(): void {
    const previousData: AdminOperatorsViewModel | undefined = this.screenStateStore.data();

    this.screenStateStore.setLoading(previousData);

    this.parkOperatorsApiService.getAllParkOperators().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (operators: ParkOperator[]) => {
        this.operatorsSignal.set(operators);
        this.ensureCurrentPageIsValid();
        this.pushDerivedState();
      },
      error: (error: unknown) => {
        console.error('Error loading operators', error);
        this.screenStateStore.setError('admin.operators.loadError', previousData);
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
    const operators: ParkOperator[] = this.operatorsSignal();
    const normalizedQuery: string = this.searchQuerySignal().trim().toLowerCase();
    const filteredOperators: ParkOperator[] = normalizedQuery.length === 0
      ? [...operators]
      : operators.filter((parkOperator: ParkOperator) => parkOperator.name.toLowerCase().includes(normalizedQuery));

    const viewModel: AdminOperatorsViewModel = {
      operators,
      filteredOperators,
      searchQuery: this.searchQuerySignal()
    };

    if (operators.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    if (filteredOperators.length === 0) {
      this.screenStateStore.setEmpty(viewModel);
      return;
    }

    this.screenStateStore.setReady(viewModel);
  }

  private ensureCurrentPageIsValid(): void {
    const pageSize: number = this.pageSizeSignal();
    const totalItems: number = this.computeFilteredOperators().length;
    const totalPages: number = Math.max(Math.ceil(totalItems / pageSize), 1);
    if (this.currentPageSignal() > totalPages) {
      this.currentPageSignal.set(totalPages);
    }
  }

  private computeFilteredOperators(): ParkOperator[] {
    const operators: ParkOperator[] = this.operatorsSignal();
    const normalizedQuery: string = this.searchQuerySignal().trim().toLowerCase();
    if (normalizedQuery.length === 0) {
      return [...operators];
    }

    return operators.filter((parkOperator: ParkOperator) => parkOperator.name.toLowerCase().includes(normalizedQuery));
  }
}
