import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SharedProfileComparison } from '@app/models/sharing/profile-comparison.models';
import {
  SHARE_PRODUCT_ANALYTICS_PORT,
  ShareProductAnalyticsPort,
} from '@core/analytics/share-product-analytics.port';
import {
  SHARED_PROFILE_COMPARISON_PORT,
  SharedProfileComparisonPort,
} from './shared-profile-comparison-state-data.ports';

@Injectable()
export class SharedProfileComparisonStateFacade {
  private readonly comparisonSignal = signal<SharedProfileComparison | null>(
    null,
  );
  private readonly loadingSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;
  private openedTracked: boolean = false;
  private renderFailureTracked: boolean = false;

  public readonly comparison: Signal<SharedProfileComparison | null> =
    this.comparisonSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(SHARED_PROFILE_COMPARISON_PORT)
    private readonly port: SharedProfileComparisonPort,
    private readonly destroyRef: DestroyRef,
    @Inject(SHARE_PRODUCT_ANALYTICS_PORT)
    private readonly productAnalytics: ShareProductAnalyticsPort = { track: (): void => undefined },
  ) {}

  public load(shareId: string): void {
    const generation: number = ++this.requestGeneration;
    this.comparisonSignal.set(null);
    this.loadingSignal.set(true);
    this.notFoundSignal.set(false);
    this.errorSignal.set(false);
    this.port
      .getShared(shareId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (comparison: SharedProfileComparison): void => {
          if (generation !== this.requestGeneration) {
            return;
          }
          this.comparisonSignal.set(comparison);
          this.loadingSignal.set(false);
          if (!this.openedTracked) {
            this.openedTracked = true;
            this.productAnalytics.track({
              type: 'share_opened',
              recapType: 'profile-comparison',
            });
          }
        },
        error: (error: { status?: number }): void => {
          if (generation !== this.requestGeneration) {
            return;
          }
          this.loadingSignal.set(false);
          this.notFoundSignal.set(error?.status === 404);
          this.errorSignal.set(error?.status !== 404);
          if (error?.status !== 404 && !this.renderFailureTracked) {
            this.renderFailureTracked = true;
            this.productAnalytics.track({
              type: 'share_render_failed',
              recapType: 'profile-comparison',
            });
          }
        },
      });
  }
}
