import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, concat, finalize, interval, of, switchMap, takeWhile, tap, timer } from 'rxjs';

import {
  ReviewShareModerationReportRequest,
  ShareModerationDecision,
  ShareModerationReport,
  ShareModerationReportQuery,
} from '@app/models/sharing/share-moderation.models';
import { PaginationContract } from '@shared/models/contracts';
import { PagedResult } from '@shared/models/contracts';
import {
  ADMIN_SHARE_MODERATION_STATE_PORT,
  AdminShareModerationStatePort,
} from './admin-share-moderation-state-data.ports';

@Injectable()
export class AdminShareModerationStateFacade {
  private static readonly DECISION_REFRESH_INTERVAL_MS: number = 30_000;

  private readonly reportsState = signal<ShareModerationReport[]>([]);
  private readonly paginationState = signal<PaginationContract | null>(null);
  private readonly loadingState = signal<boolean>(false);
  private readonly errorState = signal<boolean>(false);
  private readonly reviewingReportIdState = signal<string | null>(null);
  private readonly lastQueryState = signal<ShareModerationReportQuery>({ page: 1, size: 20 });

  public readonly reports: Signal<ShareModerationReport[]> = this.reportsState.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationState.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingState.asReadonly();
  public readonly error: Signal<boolean> = this.errorState.asReadonly();
  public readonly reviewingReportId: Signal<string | null> = this.reviewingReportIdState.asReadonly();
  public readonly isEmpty: Signal<boolean> = computed(
    (): boolean => !this.loadingState() && !this.errorState() && this.reportsState().length === 0,
  );

  public constructor(
    @Inject(ADMIN_SHARE_MODERATION_STATE_PORT) private readonly port: AdminShareModerationStatePort,
    private readonly destroyRef: DestroyRef,
  ) {}

  public load(query: ShareModerationReportQuery): void {
    if (this.loadingState()) {
      return;
    }
    this.lastQueryState.set(query);
    this.loadingState.set(true);
    this.errorState.set(false);
    this.port.search(query)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => this.loadingState.set(false)),
      )
      .subscribe({
        next: (response: PagedResult<ShareModerationReport>): void => {
          this.reportsState.set(response.items);
          this.paginationState.set(response.pagination);
        },
        error: (): void => this.errorState.set(true),
      });
  }

  public review(reportId: string, decision: ShareModerationDecision, note: string): void {
    if (this.reviewingReportIdState()) {
      return;
    }
    const normalizedNote: string = note.trim();
    const request: ReviewShareModerationReportRequest = {
      decision,
      note: normalizedNote.length > 0 ? normalizedNote : null,
    };
    this.reviewingReportIdState.set(reportId);
    this.errorState.set(false);
    this.port.review(reportId, request)
      .pipe(
        switchMap((): Observable<PagedResult<ShareModerationReport>> =>
          this.refreshUntilDecisionSettles(reportId)),
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => this.reviewingReportIdState.set(null)),
      )
      .subscribe({
        error: (): void => this.errorState.set(true),
      });
  }

  private refreshUntilDecisionSettles(
    reportId: string,
  ): Observable<PagedResult<ShareModerationReport>> {
    const query: ShareModerationReportQuery = this.lastQueryState();
    return concat(
      of(0),
      timer(2_000),
      timer(3_000),
      timer(10_000),
      interval(AdminShareModerationStateFacade.DECISION_REFRESH_INTERVAL_MS),
    ).pipe(
      switchMap((): Observable<PagedResult<ShareModerationReport>> => this.port.search(query)),
      tap((response: PagedResult<ShareModerationReport>): void => {
        if (this.lastQueryState() !== query) {
          return;
        }

        this.reportsState.set(response.items);
        this.paginationState.set(response.pagination);
      }),
      takeWhile(
        (response: PagedResult<ShareModerationReport>): boolean =>
          this.lastQueryState() === query
          && response.items.some(
            (report: ShareModerationReport): boolean =>
              report.reportId === reportId && report.status === 'Pending',
          ),
        true,
      ),
    );
  }
}
