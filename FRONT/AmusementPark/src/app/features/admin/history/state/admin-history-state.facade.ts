import { DestroyRef, Inject, Injectable, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize, tap } from 'rxjs';

import { AdminHistoryEventListQuery } from '@data-access/history/history-api-endpoints';
import { HistoryEvent, HistoryEventWriteModel } from '@app/models/history/history.models';
import { PagedResult } from '@shared/models/contracts';
import { ADMIN_HISTORY_DATA_PORT, AdminHistoryDataPort } from './admin-history-data.ports';

export interface AdminHistoryListState {
  readonly events: HistoryEvent[];
  readonly loading: boolean;
  readonly totalRecords: number;
  readonly page: number;
  readonly size: number;
  readonly errorKey: string | null;
}

@Injectable()
export class AdminHistoryStateFacade {
  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly stateSignal = signal<AdminHistoryListState>({
    events: [],
    loading: false,
    totalRecords: 0,
    page: 1,
    size: 20,
    errorKey: null
  });
  private readonly savingSignal = signal<boolean>(false);
  private readonly deletingSignal = signal<boolean>(false);

  public readonly state: Signal<AdminHistoryListState> = this.stateSignal.asReadonly();
  public readonly events: Signal<HistoryEvent[]> = computed(() => this.stateSignal().events);
  public readonly loading: Signal<boolean> = computed(() => this.stateSignal().loading);
  public readonly totalRecords: Signal<number> = computed(() => this.stateSignal().totalRecords);
  public readonly page: Signal<number> = computed(() => this.stateSignal().page);
  public readonly size: Signal<number> = computed(() => this.stateSignal().size);
  public readonly errorKey: Signal<string | null> = computed(() => this.stateSignal().errorKey);
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly deleting: Signal<boolean> = this.deletingSignal.asReadonly();

  private lastQuery: AdminHistoryEventListQuery = {
    page: 1,
    size: 20,
    includeHidden: true
  };

  constructor(@Inject(ADMIN_HISTORY_DATA_PORT) private readonly dataPort: AdminHistoryDataPort) {
  }

  load(query: AdminHistoryEventListQuery = this.lastQuery): void {
    this.lastQuery = {
      page: Math.max(1, query.page ?? 1),
      size: Math.max(1, query.size ?? 20),
      entityType: query.entityType ?? null,
      ownerId: query.ownerId ?? null,
      search: query.search ?? null,
      includeHidden: query.includeHidden ?? true
    };
    this.stateSignal.update((state: AdminHistoryListState): AdminHistoryListState => ({
      ...state,
      loading: true,
      page: this.lastQuery.page ?? 1,
      size: this.lastQuery.size ?? 20,
      errorKey: null
    }));

    this.dataPort.getAdminEvents(this.lastQuery)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: PagedResult<HistoryEvent>): void => {
          this.stateSignal.set({
            events: result.items,
            loading: false,
            totalRecords: result.pagination.totalItems,
            page: result.pagination.currentPage,
            size: result.pagination.itemsPerPage,
            errorKey: null
          });
        },
        error: (): void => {
          this.stateSignal.update((state: AdminHistoryListState): AdminHistoryListState => ({
            ...state,
            events: [],
            loading: false,
            totalRecords: 0,
            errorKey: 'admin.history.errors.loadFailed'
          }));
        }
      });
  }

  save(eventId: string | null, request: HistoryEventWriteModel): Observable<HistoryEvent> {
    this.savingSignal.set(true);
    const save$: Observable<HistoryEvent> = eventId
      ? this.dataPort.updateAdminEvent(eventId, request)
      : this.dataPort.createAdminEvent(request);

    return save$.pipe(
      tap((): void => this.load(this.lastQuery)),
      finalize((): void => this.savingSignal.set(false))
    );
  }

  delete(eventId: string): Observable<boolean> {
    this.deletingSignal.set(true);
    return this.dataPort.deleteAdminEvent(eventId).pipe(
      tap((): void => this.load(this.lastQuery)),
      finalize((): void => this.deletingSignal.set(false))
    );
  }
}
