import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  AdminContactGrievance,
  AdminContactGrievanceQuery,
  AdminContactGrievanceResponse
} from '@app/models/contact/contact-grievance.models';
import { Pagination } from '@app/models/shared/pagination';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_CONTACT_GRIEVANCES_DATA_PORT,
  AdminContactGrievancesDataPort
} from './admin-contact-grievances-data.ports';

interface AdminContactGrievancesViewModel {
  grievances: AdminContactGrievance[];
  pagination: Pagination | null;
  totalRecords: number;
  currentPage: number;
  pageSize: number;
}

@Injectable()
export class AdminContactGrievancesFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminContactGrievancesViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);
  private readonly lastQuerySignal = signal<AdminContactGrievanceQuery>({ page: 1, size: 20 });

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly grievances: Signal<AdminContactGrievance[]> = computed(() => this.screenStateStore.data()?.grievances ?? []);
  public readonly totalRecords: Signal<number> = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTACT_GRIEVANCES_DATA_PORT) private readonly apiService: AdminContactGrievancesDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: Partial<AdminContactGrievanceQuery> = {}): void {
    const previousData: AdminContactGrievancesViewModel | undefined = this.screenStateStore.data();
    const effectiveQuery: AdminContactGrievanceQuery = {
      ...this.lastQuerySignal(),
      ...query,
      page: query.page ?? this.currentPageSignal(),
      size: query.size ?? this.pageSizeSignal()
    };

    this.currentPageSignal.set(effectiveQuery.page);
    this.pageSizeSignal.set(effectiveQuery.size);
    this.lastQuerySignal.set(effectiveQuery);
    this.screenStateStore.setLoading(previousData);

    this.apiService.searchAdminGrievances(effectiveQuery).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: AdminContactGrievanceResponse) => {
        const grievances: AdminContactGrievance[] = response.data ?? [];
        const pagination: Pagination | null = response.pagination ?? null;
        const viewModel: AdminContactGrievancesViewModel = {
          grievances,
          pagination,
          totalRecords: pagination?.totalItems ?? grievances.length,
          currentPage: pagination?.currentPage ?? effectiveQuery.page,
          pageSize: pagination?.itemsPerPage ?? effectiveQuery.size
        };

        this.currentPageSignal.set(viewModel.currentPage);
        this.pageSizeSignal.set(viewModel.pageSize);

        if (viewModel.totalRecords === 0) {
          this.screenStateStore.setEmpty(viewModel);
          return;
        }

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading contact grievances', error);
        this.screenStateStore.setError('admin.contactGrievances.loadError', previousData);
      }
    });
  }
}
