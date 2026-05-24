import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkOperator } from '@app/models/parks/park-operator';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminOperatorsViewModel {
  operators: ParkOperator[];
  filteredOperators: ParkOperator[];
  searchQuery: string;
  adminReviewStatus: AdminReviewStatus | null;
}

@Injectable()
export class AdminOperatorsStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminOperatorsViewModel>();
  private readonly operatorsSignal = signal<ParkOperator[]>([]);
  private readonly searchQuerySignal = signal('');
  private readonly adminReviewStatusFilterSignal = signal<AdminReviewStatus | null>(null);
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);
  private readonly selectedOperatorIdsSignal = signal<string[]>([]);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly adminReviewStatusFilter = this.adminReviewStatusFilterSignal.asReadonly();
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly selectedOperatorIds = this.selectedOperatorIdsSignal.asReadonly();
  public readonly selectedCount = computed(() => this.selectedOperatorIdsSignal().length);
  public readonly filteredOperators: Signal<ParkOperator[]> = computed(() => this.screenStateStore.data()?.filteredOperators ?? []);
  public readonly pagedOperators: Signal<ParkOperator[]> = computed(() => {
    const page: number = this.currentPageSignal();
    const pageSize: number = this.pageSizeSignal();
    const start: number = (page - 1) * pageSize;
    return this.filteredOperators().slice(start, start + pageSize);
  });
  public readonly totalCount = computed(() => this.filteredOperators().length);

  constructor(
    private readonly parkOperatorsApiService: ParkOperatorsApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadOperators(): void {
    const previousData: AdminOperatorsViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.parkOperatorsApiService.getAllParkOperators().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (operators: ParkOperator[]) => {
        this.operatorsSignal.set(operators.map((operator: ParkOperator) => ({ ...operator, adminReviewStatus: operator.adminReviewStatus ?? 'ToReview' })));
        this.ensureCurrentPageIsValid();
        this.pruneSelection();
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

  setOperatorSelection(operatorId: string, selected: boolean): void {
    const currentSelection: string[] = this.selectedOperatorIdsSignal();
    if (selected) {
      this.selectedOperatorIdsSignal.set([...new Set([...currentSelection, operatorId])]);
      return;
    }

    this.selectedOperatorIdsSignal.set(currentSelection.filter((id: string) => id !== operatorId));
  }

  setCurrentPageSelection(selected: boolean): void {
    const currentPageIds: string[] = this.pagedOperators()
      .map((operator: ParkOperator) => operator.id)
      .filter((id: string | undefined): id is string => !!id);

    if (selected) {
      this.selectedOperatorIdsSignal.set([...new Set([...this.selectedOperatorIdsSignal(), ...currentPageIds])]);
      return;
    }

    this.selectedOperatorIdsSignal.set(this.selectedOperatorIdsSignal().filter((id: string) => !currentPageIds.includes(id)));
  }

  clearSelection(): void {
    this.selectedOperatorIdsSignal.set([]);
  }

  updateSelectedReviewStatus(adminReviewStatus: AdminReviewStatus): void {
    const ids: string[] = this.selectedOperatorIdsSignal();
    if (ids.length === 0) {
      return;
    }

    this.parkOperatorsApiService.updateParkOperatorsBulkReviewStatus({ ids, adminReviewStatus }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.operatorsSignal.update((operators: ParkOperator[]) => operators.map((operator: ParkOperator) => {
          if (!operator.id || !ids.includes(operator.id)) {
            return operator;
          }

          return { ...operator, adminReviewStatus };
        }));
        this.clearSelection();
        this.pushDerivedState();
      },
      error: (error: unknown) => {
        console.error('Error updating operators review status', error);
      }
    });
  }

  private pushDerivedState(): void {
    const operators: ParkOperator[] = this.operatorsSignal();
    const filteredOperators: ParkOperator[] = this.computeFilteredOperators();

    const viewModel: AdminOperatorsViewModel = {
      operators,
      filteredOperators,
      searchQuery: this.searchQuerySignal(),
      adminReviewStatus: this.adminReviewStatusFilterSignal()
    };

    if (operators.length === 0 || filteredOperators.length === 0) {
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
    const adminReviewStatus: AdminReviewStatus | null = this.adminReviewStatusFilterSignal();

    return operators.filter((parkOperator: ParkOperator) => {
      const matchesQuery: boolean = normalizedQuery.length === 0 || parkOperator.name.toLowerCase().includes(normalizedQuery);
      const matchesStatus: boolean = adminReviewStatus === null || (parkOperator.adminReviewStatus ?? 'ToReview') === adminReviewStatus;
      return matchesQuery && matchesStatus;
    });
  }

  private pruneSelection(): void {
    const validIds: Set<string> = new Set(this.operatorsSignal().map((operator: ParkOperator) => operator.id).filter((id: string | undefined): id is string => !!id));
    this.selectedOperatorIdsSignal.set(this.selectedOperatorIdsSignal().filter((id: string) => validIds.has(id)));
  }
}
