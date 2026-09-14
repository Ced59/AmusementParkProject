import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  ParkFitDataQuality,
  ParkFitDataQualityPage
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

  private readonly screenStateStore = new SignalScreenStateStore<ParkFitDataQualityPage>();
  private readonly requestedPageSignal = signal<number>(1);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly assessments: Signal<readonly ParkFitDataQuality[]> = computed(
    () => this.screenStateStore.data()?.items ?? []);
  public readonly pagination: Signal<PaginationContract> = computed(() =>
    this.screenStateStore.data()?.pagination ?? {
      totalItems: 0,
      totalPages: 0,
      currentPage: this.requestedPageSignal(),
      itemsPerPage: AdminParkFitDataQualityFacade.PageSize
    });
  public readonly eligibleCount: Signal<number> = computed(() =>
    this.assessments().filter((item: ParkFitDataQuality) =>
      item.status === 'EligibleForFitComparison').length);
  public readonly actionRequiredCount: Signal<number> = computed(() =>
    this.assessments().filter((item: ParkFitDataQuality) => item.issueItemCount > 0).length);
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
    const previousData: ParkFitDataQualityPage | undefined = this.screenStateStore.data();
    this.requestedPageSignal.set(safePage);
    this.screenStateStore.setLoading(previousData);
    this.apiService.getPage(safePage, AdminParkFitDataQualityFacade.PageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: ParkFitDataQualityPage): void => this.screenStateStore.setReady(result),
        error: (error: unknown): void => {
          console.error('Error loading park fit data quality', error);
          this.screenStateStore.setError('admin.parkFitDataQuality.loadError', previousData);
        }
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
}
