import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AdminHistoricalParkDiagnostics } from '@app/models/history/admin-historical-park-diagnostics.models';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  ADMIN_HISTORY_DIAGNOSTICS_DATA_PORT,
  AdminHistoryDiagnosticsDataPort
} from './admin-history-diagnostics-data.port';
import {
  ADMIN_HISTORY_DIAGNOSTICS_PARKS_PORT,
  AdminHistoryDiagnosticsParksPort
} from './admin-history-diagnostics-parks.port';

@Injectable()
export class AdminHistoryDiagnosticsStateFacade {
  private readonly searchResultsSignal = signal<readonly Park[]>([]);
  private readonly diagnosticsSignal = signal<AdminHistoricalParkDiagnostics | null>(null);
  private readonly searchingSignal = signal<boolean>(false);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);

  public readonly searchResults: Signal<readonly Park[]> = this.searchResultsSignal.asReadonly();
  public readonly diagnostics: Signal<AdminHistoricalParkDiagnostics | null> =
    this.diagnosticsSignal.asReadonly();
  public readonly searching: Signal<boolean> = this.searchingSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly hasBlockingIssues: Signal<boolean> = computed(
    (): boolean => (this.diagnosticsSignal()?.blockingIssueCount ?? 0) > 0
  );

  constructor(
    @Inject(ADMIN_HISTORY_DIAGNOSTICS_DATA_PORT)
    private readonly diagnosticsPort: AdminHistoryDiagnosticsDataPort,
    @Inject(ADMIN_HISTORY_DIAGNOSTICS_PARKS_PORT)
    private readonly parksPort: AdminHistoryDiagnosticsParksPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  public search(query: string): void {
    const normalizedQuery: string = query.trim();
    if (normalizedQuery.length < 2 || this.searchingSignal()) {
      return;
    }

    this.searchingSignal.set(true);
    this.errorKeySignal.set(null);
    this.parksPort.searchParks(normalizedQuery, 1, 12, false)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: ParksApiResponse): void => {
          this.searchResultsSignal.set(response.data ?? []);
          this.searchingSignal.set(false);
        },
        error: (): void => {
          this.searchResultsSignal.set([]);
          this.searchingSignal.set(false);
          this.errorKeySignal.set('admin.history.diagnostics.errors.searchFailed');
        }
      });
  }

  public load(parkId: string): void {
    const normalizedParkId: string = parkId.trim();
    if (normalizedParkId.length === 0 || this.loadingSignal()) {
      return;
    }

    this.loadingSignal.set(true);
    this.errorKeySignal.set(null);
    this.diagnosticsPort.getAdminParkDiagnostics(normalizedParkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (diagnostics: AdminHistoricalParkDiagnostics): void => {
          this.diagnosticsSignal.set(diagnostics);
          this.loadingSignal.set(false);
        },
        error: (): void => {
          this.diagnosticsSignal.set(null);
          this.loadingSignal.set(false);
          this.errorKeySignal.set('admin.history.diagnostics.errors.loadFailed');
        }
      });
  }
}
