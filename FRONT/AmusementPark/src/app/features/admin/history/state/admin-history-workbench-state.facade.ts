import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, finalize, map, of, switchMap, tap, throwError } from 'rxjs';

import {
  AdminHistoricalParkWorkbench,
  HistoricalEditorialMutation,
  HistoricalEditorialResourceType,
  HistoricalPublicationImpactPreview,
  SaveHistoricalFactRequest,
  SaveHistoricalRelationRequest,
  SaveHistoricalSourceRequest
} from '@app/models/history/admin-historical-workbench.models';
import {
  ADMIN_HISTORY_WORKBENCH_DATA_PORT,
  AdminHistoryWorkbenchDataPort
} from './admin-history-workbench-data.port';

@Injectable()
export class AdminHistoryWorkbenchStateFacade {
  private readonly workbenchSignal = signal<AdminHistoricalParkWorkbench | null>(null);
  private readonly previewSignal = signal<HistoricalPublicationImpactPreview | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly busySignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly messageKeySignal = signal<string | null>(null);
  private readonly loadRequests = new Subject<string>();
  private parkId = '';

  public readonly workbench: Signal<AdminHistoricalParkWorkbench | null> = this.workbenchSignal.asReadonly();
  public readonly preview: Signal<HistoricalPublicationImpactPreview | null> = this.previewSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly busy: Signal<boolean> = this.busySignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly messageKey: Signal<string | null> = this.messageKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_HISTORY_WORKBENCH_DATA_PORT)
    private readonly dataPort: AdminHistoryWorkbenchDataPort,
    private readonly destroyRef: DestroyRef
  ) {
    this.loadRequests.pipe(
      tap((): void => {
        this.loadingSignal.set(true);
        this.errorKeySignal.set(null);
      }),
      switchMap((parkId: string) => this.dataPort.getAdminParkWorkbench(parkId).pipe(
        map((workbench: AdminHistoricalParkWorkbench) => ({ workbench, errorKey: null as string | null })),
        catchError(() => of({ workbench: null, errorKey: 'admin.history.workbench.errors.loadFailed' }))
      )),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(({ workbench, errorKey }): void => {
      this.workbenchSignal.set(workbench);
      this.errorKeySignal.set(errorKey);
      this.loadingSignal.set(false);
    });
  }

  public load(parkId: string): void {
    const normalizedParkId: string = parkId.trim();
    if (!normalizedParkId) {
      return;
    }

    this.parkId = normalizedParkId;
    this.previewSignal.set(null);
    this.loadRequests.next(normalizedParkId);
  }

  public saveSource(sourceId: string | null, request: SaveHistoricalSourceRequest): Observable<HistoricalEditorialMutation> {
    return this.runMutation(this.dataPort.saveAdminHistoricalSource(sourceId, request));
  }

  public saveFact(factId: string | null, request: SaveHistoricalFactRequest): Observable<HistoricalEditorialMutation> {
    return this.runMutation(this.dataPort.saveAdminHistoricalFact(this.parkId, factId, request));
  }

  public saveRelation(relationId: string | null, request: SaveHistoricalRelationRequest): Observable<HistoricalEditorialMutation> {
    return this.runMutation(this.dataPort.saveAdminHistoricalRelation(this.parkId, relationId, request));
  }

  public advance(resourceType: HistoricalEditorialResourceType, resourceId: string, expectedRevision: number): Observable<HistoricalEditorialMutation> {
    return this.runMutation(this.dataPort.advanceAdminHistoricalResource(
      resourceType,
      resourceId,
      expectedRevision,
      null
    ));
  }

  public retract(resourceType: HistoricalEditorialResourceType, resourceId: string, expectedRevision: number): Observable<HistoricalEditorialMutation> {
    return this.runMutation(this.dataPort.retractAdminHistoricalResource(
      resourceType,
      resourceId,
      expectedRevision,
      null
    ));
  }

  public previewImpact(resourceType: HistoricalEditorialResourceType, resourceId: string, year: number | null): void {
    if (!this.parkId || this.busySignal()) {
      return;
    }

    this.busySignal.set(true);
    this.errorKeySignal.set(null);
    this.dataPort.previewAdminHistoricalImpact(this.parkId, resourceType, resourceId, year)
      .pipe(
        finalize((): void => this.busySignal.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (preview: HistoricalPublicationImpactPreview): void => this.previewSignal.set(preview),
        error: (): void => this.errorKeySignal.set('admin.history.workbench.errors.previewFailed')
      });
  }

  public clearFeedback(): void {
    this.errorKeySignal.set(null);
    this.messageKeySignal.set(null);
  }

  private runMutation(request: Observable<HistoricalEditorialMutation>): Observable<HistoricalEditorialMutation> {
    this.busySignal.set(true);
    this.clearFeedback();
    return request.pipe(
      tap((): void => {
        this.messageKeySignal.set('admin.history.workbench.messages.saved');
        this.load(this.parkId);
      }),
      catchError((error: unknown) => {
        this.errorKeySignal.set('admin.history.workbench.errors.saveFailed');
        return throwError(() => error);
      }),
      finalize((): void => this.busySignal.set(false))
    );
  }
}
