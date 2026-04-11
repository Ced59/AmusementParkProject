import { Injectable, Signal, computed, signal } from '@angular/core';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { Park } from '@app/models/parks/park';
import { Pagination } from '@app/models/shared/pagination';

interface ParkListViewModel {
  parks: Park[];
  pagination: Pagination | null;
}

@Injectable()
export class ParkListStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkListViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(9);

  public readonly state = this.screenStateStore.state;
  public readonly parks: Signal<Park[]> = computed(() => this.screenStateStore.data()?.parks ?? []);
  public readonly pagination: Signal<Pagination | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(private readonly parksApiService: ParksApiService) {
  }

  loadParks(page: number, size: number, term: string): void {
    const normalizedTerm: string = term.trim();
    const previousData: ParkListViewModel | undefined = this.screenStateStore.data();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const request$ = normalizedTerm
      ? this.parksApiService.searchParks(normalizedTerm, page, size)
      : this.parksApiService.getParksPaginated(page, size);

    request$.subscribe({
      next: (response: { data?: Park[] | null; pagination?: Pagination | null }) => {
        const parks: Park[] = response.data ?? [];
        const viewModel: ParkListViewModel = {
          parks,
          pagination: response.pagination ?? null
        };

        if (parks.length === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error fetching parks:', error);
        this.screenStateStore.setError('parks.errorMessage', previousData);
      }
    });
  }
}
