import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import {
  ParkFitDataQuality,
  ParkFitDataQualityPage,
  ParkFitOperationalStatusRequest,
  ParkFitOperationsSnapshot,
  ParkFitRecommendationState,
  ParkFitSourceReport,
  ParkFitSourceReportReviewRequest
} from '@app/models/admin/park-fit/park-fit-data-quality.models';
import { PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT,
  AdminParkFitDataQualityStatePort
} from './admin-park-fit-data-quality-state-data.ports';

@Injectable()
export class AdminParkFitDataQualityFacade {
  public static readonly PageSize = 12;

  private readonly screenStateStore = new SignalScreenStateStore<ParkFitOperationsSnapshot>();
  private readonly requestedPageSignal = signal<number>(1);
  private readonly processingKeysSignal = signal<ReadonlySet<string>>(new Set<string>());
  private readonly actionErrorKeySignal = signal<string | null>(null);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly assessments: Signal<readonly ParkFitDataQuality[]> = computed(
    () => this.screenStateStore.data()?.quality.items ?? []);
  public readonly reports: Signal<readonly ParkFitSourceReport[]> = computed(
    () => this.screenStateStore.data()?.reports.items ?? []);
  public readonly pendingReportTotal: Signal<number> = computed(
    () => this.screenStateStore.data()?.reports.pagination.totalItems ?? 0);
  public readonly actionErrorKey: Signal<string | null> = this.actionErrorKeySignal.asReadonly();
  public readonly pagination: Signal<PaginationContract> = computed(() =>
    this.screenStateStore.data()?.quality.pagination ?? {
      totalItems: 0,
      totalPages: 0,
      currentPage: this.requestedPageSignal(),
      itemsPerPage: AdminParkFitDataQualityFacade.PageSize
    });
  public readonly eligibleCount: Signal<number> = computed(() =>
    this.assessments().filter((item: ParkFitDataQuality) =>
      item.status === 'EligibleForFitComparison').length);
  public readonly actionRequiredCount: Signal<number> = computed(() =>
    this.assessments().filter((item: ParkFitDataQuality) =>
      item.status !== 'EligibleForFitComparison').length);
  public readonly averageCoveragePercent: Signal<number> = computed(() => {
    const assessments: readonly ParkFitDataQuality[] = this.assessments();
    if (assessments.length === 0) {
      return 0;
    }

    return Math.round(
      assessments.reduce((total: number, item: ParkFitDataQuality) =>
        total + item.coveragePercent, 0) / assessments.length);
  });

  constructor(
    @Inject(ADMIN_PARK_FIT_DATA_QUALITY_STATE_PORT)
    private readonly apiService: AdminParkFitDataQualityStatePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(page: number = 1): void {
    if (this.loading() && this.screenStateStore.data() !== undefined) {
      return;
    }

    const safePage: number = Math.max(1, page);
    const previousData: ParkFitOperationsSnapshot | undefined = this.screenStateStore.data();
    this.requestedPageSignal.set(safePage);
    this.screenStateStore.setLoading(previousData);
    forkJoin({
      quality: this.apiService.getPage(safePage, AdminParkFitDataQualityFacade.PageSize),
      reports: this.apiService.getPendingReports(1, AdminParkFitDataQualityFacade.PageSize)
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: ParkFitOperationsSnapshot): void => {
          this.actionErrorKeySignal.set(null);
          this.screenStateStore.setReady(result);
        },
        error: (error: unknown): void => {
          console.error('Error loading park fit data quality', error);
          this.screenStateStore.setError('admin.parkFitDataQuality.loadError', previousData);
        }
      });
  }

  isProcessing(key: string): boolean {
    return this.processingKeysSignal().has(key);
  }

  reviewReport(
    report: ParkFitSourceReport,
    decision: 'Resolved' | 'Dismissed',
    decisionNote: string | null
  ): void {
    const key: string = `report:${report.reportId}`;
    if (!this.beginAction(key)) {
      return;
    }

    const request: ParkFitSourceReportReviewRequest = {
      decision,
      decisionNote,
      expectedRevision: report.revision
    };
    this.apiService.reviewReport(report.reportId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => this.completeAction(key),
        error: (): void => this.failAction(key)
      });
  }

  changeOperationalStatus(
    park: ParkFitDataQuality,
    targetState: ParkFitRecommendationState,
    reason: string
  ): void {
    const key: string = `park:${park.parkId}`;
    if (!this.beginAction(key)) {
      return;
    }

    const request: ParkFitOperationalStatusRequest = {
      targetState,
      reason,
      expectedRevision: park.operationalRevision
    };
    this.apiService.changeOperationalStatus(park.parkId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => this.completeAction(key),
        error: (): void => this.failAction(key)
      });
  }

  previousPage(): void {
    const currentPage: number = this.pagination().currentPage;
    if (currentPage > 1) {
      this.load(currentPage - 1);
    }
  }

  nextPage(): void {
    const pagination: PaginationContract = this.pagination();
    if (pagination.currentPage < pagination.totalPages) {
      this.load(pagination.currentPage + 1);
    }
  }

  private beginAction(key: string): boolean {
    if (this.processingKeysSignal().has(key)) {
      return false;
    }

    this.actionErrorKeySignal.set(null);
    this.processingKeysSignal.update((current: ReadonlySet<string>): ReadonlySet<string> => {
      const next: Set<string> = new Set(current);
      next.add(key);
      return next;
    });
    return true;
  }

  private completeAction(key: string): void {
    this.removeProcessingKey(key);
    this.load(this.pagination().currentPage);
  }

  private failAction(key: string): void {
    this.removeProcessingKey(key);
    this.actionErrorKeySignal.set('admin.parkFitDataQuality.operations.error');
  }

  private removeProcessingKey(key: string): void {
    this.processingKeysSignal.update((current: ReadonlySet<string>): ReadonlySet<string> => {
      const next: Set<string> = new Set(current);
      next.delete(key);
      return next;
    });
  }
}
