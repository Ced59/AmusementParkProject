import { Injectable, Signal, computed, signal } from '@angular/core';
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

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly searchQuery = this.searchQuerySignal.asReadonly();
  public readonly filteredOperators: Signal<ParkOperator[]> = computed(() => this.screenStateStore.data()?.filteredOperators ?? []);
  public readonly totalCount = computed(() => this.filteredOperators().length);

  constructor(private readonly parkOperatorsApiService: ParkOperatorsApiService) {
  }

  loadOperators(): void {
    const previousData: AdminOperatorsViewModel | undefined = this.screenStateStore.data();

    this.screenStateStore.setLoading(previousData);

    this.parkOperatorsApiService.getParkOperators().subscribe({
      next: (operators: ParkOperator[]) => {
        this.operatorsSignal.set(operators);
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
}
