import { DestroyRef, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AdminAuditLog, AdminAuditLogQuery, AdminAuditLogResponse } from '@app/models/admin/audit/admin-audit-log.models';
import { Pagination } from '@app/models/shared/pagination';
import { AdminAuditLogsApiService } from '@data-access/admin/admin-audit-logs-api.service';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminAuditLogsViewModel {
  logs: AdminAuditLog[];
  pagination: Pagination | null;
  totalRecords: number;
  currentPage: number;
  pageSize: number;
}

@Injectable()
export class AdminAuditLogsStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminAuditLogsViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(20);
  private readonly lastQuerySignal = signal<AdminAuditLogQuery>({ page: 1, size: 20 });

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly logs: Signal<AdminAuditLog[]> = computed(() => this.screenStateStore.data()?.logs ?? []);
  public readonly pagination: Signal<Pagination | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly totalRecords = computed(() => this.screenStateStore.data()?.totalRecords ?? 0);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly lastQuery = this.lastQuerySignal.asReadonly();

  constructor(
    private readonly apiService: AdminAuditLogsApiService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: Partial<AdminAuditLogQuery> = {}): void {
    const previousData: AdminAuditLogsViewModel | undefined = this.screenStateStore.data();
    const effectiveQuery: AdminAuditLogQuery = {
      ...this.lastQuerySignal(),
      ...query,
      page: query.page ?? this.currentPageSignal(),
      size: query.size ?? this.pageSizeSignal()
    };

    this.currentPageSignal.set(effectiveQuery.page);
    this.pageSizeSignal.set(effectiveQuery.size);
    this.lastQuerySignal.set(effectiveQuery);
    this.screenStateStore.setLoading(previousData);

    this.apiService.search(effectiveQuery).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: AdminAuditLogResponse) => {
        const logs: AdminAuditLog[] = response.data ?? [];
        const pagination: Pagination | null = response.pagination ?? null;
        const viewModel: AdminAuditLogsViewModel = {
          logs,
          pagination,
          totalRecords: pagination?.totalItems ?? logs.length,
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
        console.error('Error loading admin audit logs', error);
        this.screenStateStore.setError('admin.auditLogs.loadError', previousData);
      }
    });
  }
}
