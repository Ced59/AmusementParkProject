import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  SocialShareDimensionCount,
  SocialShareStatsQuery,
  SocialShareStatsResult,
  SocialShareTopTarget
} from '@app/models/social-share/social-share.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_SOCIAL_SHARE_STATS_DATA_PORT,
  AdminSocialShareStatsDataPort
} from './admin-social-share-stats-state-data.ports';

@Injectable()
export class AdminSocialShareStatsFacade {
  private readonly screenStateStore = new SignalScreenStateStore<SocialShareStatsResult>();

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly stats: Signal<SocialShareStatsResult | null> = computed(() => this.screenStateStore.data() ?? null);
  public readonly totalEvents: Signal<number> = computed(() => this.stats()?.totalEvents ?? 0);
  public readonly anonymousEvents: Signal<number> = computed(() => this.stats()?.anonymousEvents ?? 0);
  public readonly authenticatedEvents: Signal<number> = computed(() => this.stats()?.authenticatedEvents ?? 0);
  public readonly channels: Signal<SocialShareDimensionCount[]> = computed(() => this.stats()?.channels ?? []);
  public readonly targetTypes: Signal<SocialShareDimensionCount[]> = computed(() => this.stats()?.targetTypes ?? []);
  public readonly visitorKinds: Signal<SocialShareDimensionCount[]> = computed(() => this.stats()?.visitorKinds ?? []);
  public readonly topTargets: Signal<SocialShareTopTarget[]> = computed(() => this.stats()?.topTargets ?? []);

  constructor(
    @Inject(ADMIN_SOCIAL_SHARE_STATS_DATA_PORT) private readonly dataPort: AdminSocialShareStatsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: SocialShareStatsQuery = {}): void {
    const previousData: SocialShareStatsResult | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.dataPort.getStats(query).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (stats: SocialShareStatsResult) => {
        if (stats.totalEvents === 0) {
          this.screenStateStore.setEmpty(stats);
          return;
        }

        this.screenStateStore.setReady(stats);
      },
      error: (error: unknown) => {
        console.error('Error loading social share stats', error);
        this.screenStateStore.setError('admin.socialShare.loadError', previousData);
      }
    });
  }
}
