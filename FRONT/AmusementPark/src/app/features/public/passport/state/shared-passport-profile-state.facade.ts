import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import {
  SHARE_PRODUCT_ANALYTICS_PORT,
  ShareProductAnalyticsPort
} from '@core/analytics/share-product-analytics.port';
import { SHARED_PASSPORT_PROFILE_PORT, SharedPassportProfilePort } from './shared-passport-profile-state-data.ports';

@Injectable()
export class SharedPassportProfileStateFacade {
  private readonly profileSignal = signal<SharedPassportProfile | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestGeneration: number = 0;
  private openedTracked: boolean = false;
  private renderFailureTracked: boolean = false;

  public readonly profile: Signal<SharedPassportProfile | null> = this.profileSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(SHARED_PASSPORT_PROFILE_PORT) private readonly port: SharedPassportProfilePort,
    private readonly destroyRef: DestroyRef,
    @Inject(SHARE_PRODUCT_ANALYTICS_PORT)
    private readonly productAnalytics: ShareProductAnalyticsPort = { track: (): void => undefined }
  ) {
  }

  public load(shareId: string): void {
    const generation: number = ++this.requestGeneration;
    this.profileSignal.set(null);
    this.loadingSignal.set(true);
    this.notFoundSignal.set(false);
    this.errorSignal.set(false);
    this.port.getSharedPassportProfile(shareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile: SharedPassportProfile): void => {
        if (generation !== this.requestGeneration) {
          return;
        }
        this.profileSignal.set(profile);
        this.loadingSignal.set(false);
        if (!this.openedTracked) {
          this.openedTracked = true;
          this.productAnalytics.track({ type: 'share_opened', recapType: 'passport-profile' });
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
          this.productAnalytics.track({ type: 'share_render_failed', recapType: 'passport-profile' });
        }
      }
    });
  }

  public trackPassportCta(): void {
    this.productAnalytics.track({
      type: 'share_cta_passport_started',
      recapType: 'passport-profile'
    });
  }
}
