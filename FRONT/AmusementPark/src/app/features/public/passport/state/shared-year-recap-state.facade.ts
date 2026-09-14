import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import {
  SHARE_PRODUCT_ANALYTICS_PORT,
  ShareProductAnalyticsPort
} from '@core/analytics/share-product-analytics.port';
import { SHARED_YEAR_RECAP_PORT, SharedYearRecapPort } from './shared-year-recap-state-data.ports';

@Injectable()
export class SharedYearRecapStateFacade {
  private readonly recapSignal = signal<SharedYearRecap | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;
  private openedTracked: boolean = false;
  private renderFailureTracked: boolean = false;

  public readonly recap: Signal<SharedYearRecap | null> = this.recapSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(SHARED_YEAR_RECAP_PORT) private readonly recapPort: SharedYearRecapPort,
    private readonly destroyRef: DestroyRef,
    @Inject(SHARE_PRODUCT_ANALYTICS_PORT)
    private readonly productAnalytics: ShareProductAnalyticsPort = { track: (): void => undefined }
  ) {
  }

  load(shareId: string): void {
    const generation: number = ++this.requestGeneration;
    this.recapSignal.set(null);
    this.loadingSignal.set(true);
    this.notFoundSignal.set(false);
    this.errorSignal.set(false);
    this.recapPort.getSharedYear(shareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (recap: SharedYearRecap): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.recapSignal.set(recap);
        this.loadingSignal.set(false);
        if (!this.openedTracked) {
          this.openedTracked = true;
          this.productAnalytics.track({ type: 'share_opened', recapType: 'year-recap' });
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
          this.productAnalytics.track({ type: 'share_render_failed', recapType: 'year-recap' });
        }
      }
    });
  }

  public trackPassportCta(): void {
    this.productAnalytics.track({
      type: 'share_cta_passport_started',
      recapType: 'year-recap'
    });
  }
}
