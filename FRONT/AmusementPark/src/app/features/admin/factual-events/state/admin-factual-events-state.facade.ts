import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize } from 'rxjs';

import {
  FactualChangeEventAdmin,
  FactualChangeEventMutationRequest,
  FactualChangeEventQuery,
  FactualEventAdminAction,
  FactualEventAdminActionError,
} from '@app/models/admin/factual-events/factual-event-administration.models';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import {
  ADMIN_FACTUAL_EVENTS_STATE_PORT,
  AdminFactualEventsStatePort,
} from './admin-factual-events-state-data.ports';

@Injectable()
export class AdminFactualEventsStateFacade {
  private querySequence = 0;
  private readonly eventsState = signal<FactualChangeEventAdmin[]>([]);
  private readonly paginationState = signal<PaginationContract | null>(null);
  private readonly loadingState = signal<boolean>(false);
  private readonly loadErrorState = signal<boolean>(false);
  private readonly actionEventIdState = signal<string | null>(null);
  private readonly actionErrorState = signal<FactualEventAdminActionError>(null);
  private readonly lastQueryState = signal<FactualChangeEventQuery>({ page: 1, size: 20, status: 'Draft' });

  public readonly events: Signal<FactualChangeEventAdmin[]> = this.eventsState.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationState.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingState.asReadonly();
  public readonly loadError: Signal<boolean> = this.loadErrorState.asReadonly();
  public readonly actionEventId: Signal<string | null> = this.actionEventIdState.asReadonly();
  public readonly actionError: Signal<FactualEventAdminActionError> = this.actionErrorState.asReadonly();
  public readonly isEmpty: Signal<boolean> = computed(
    (): boolean => !this.loadingState() && !this.loadErrorState() && this.eventsState().length === 0,
  );

  public constructor(
    @Inject(ADMIN_FACTUAL_EVENTS_STATE_PORT) private readonly port: AdminFactualEventsStatePort,
    private readonly destroyRef: DestroyRef,
  ) {}

  public load(query: FactualChangeEventQuery): void {
    const querySequence: number = ++this.querySequence;
    this.lastQueryState.set(query);
    this.loadingState.set(true);
    this.loadErrorState.set(false);
    this.actionErrorState.set(null);
    this.port.search(query)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => {
          if (this.isCurrentQuery(querySequence)) {
            this.loadingState.set(false);
          }
        }),
      )
      .subscribe({
        next: (response: PagedResult<FactualChangeEventAdmin>): void => {
          if (this.isCurrentQuery(querySequence)) {
            this.setPage(response);
          }
        },
        error: (): void => {
          if (this.isCurrentQuery(querySequence)) {
            this.clearPageForLoadFailure();
          }
        },
      });
  }

  public changeStatus(event: FactualChangeEventAdmin, action: FactualEventAdminAction): void {
    if (this.actionEventIdState()) {
      return;
    }

    const request: FactualChangeEventMutationRequest = { expectedVersion: event.version };
    const operation: Observable<void> = action === 'verify'
      ? this.port.verify(event.eventId, request)
      : this.port.publish(event.eventId, request);
    this.actionEventIdState.set(event.eventId);
    this.actionErrorState.set(null);
    operation.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.actionEventIdState.set(null)),
    ).subscribe({
      next: (): void => {
        this.load(this.lastQueryState());
      },
      error: (error: unknown): void => {
        this.actionErrorState.set(error instanceof HttpErrorResponse && error.status === 409
          ? 'conflict'
          : 'failure');
        if (error instanceof HttpErrorResponse && error.status === 409) {
          this.reloadAfterConflict(this.lastQueryState(), this.querySequence);
        }
      },
    });
  }

  private reloadAfterConflict(query: FactualChangeEventQuery, querySequence: number): void {
    this.port.search(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: PagedResult<FactualChangeEventAdmin>): void => {
          if (this.isCurrentQuery(querySequence)) {
            this.setPage(response);
          }
        },
        error: (): void => {
          if (this.isCurrentQuery(querySequence)) {
            this.clearPageForLoadFailure();
            this.actionErrorState.set('failure');
          }
        },
      });
  }

  private isCurrentQuery(querySequence: number): boolean {
    return this.querySequence === querySequence;
  }

  private setPage(response: PagedResult<FactualChangeEventAdmin>): void {
    this.eventsState.set(response.items);
    this.paginationState.set(response.pagination);
  }

  private clearPageForLoadFailure(): void {
    this.eventsState.set([]);
    this.paginationState.set(null);
    this.loadErrorState.set(true);
  }
}
