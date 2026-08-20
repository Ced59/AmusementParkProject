import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import { UserRankingShareSettings } from '@app/models/ratings/rating.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { USER_RANKING_SHARE_PORT, UserRankingSharePort } from './user-ranking-share-state-data.ports';

@Injectable()
export class UserRankingShareStateFacade {
  private readonly settingsSignal = signal<UserRankingShareSettings | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  public readonly settings: Signal<UserRankingShareSettings | null> = this.settingsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(USER_RANKING_SHARE_PORT) private readonly sharePort: UserRankingSharePort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(false);

    this.sharePort.getMyShareSettings().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: UserRankingShareSettings): void => {
        this.settingsSignal.set(settings);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading user ranking share settings', error);
        this.loadingSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  setPublic(isPublic: boolean): void {
    if (this.savingSignal()) {
      return;
    }

    this.savingSignal.set(true);
    this.errorSignal.set(false);
    this.sharePort.setMyShareVisibility(isPublic).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: UserRankingShareSettings): void => {
        this.settingsSignal.set(settings);
        this.savingSignal.set(false);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('common.success'),
          this.translateService.instant(isPublic
            ? 'ratings.share.manage.publishedToast'
            : 'ratings.share.manage.privateToast')
        );
      },
      error: (error: unknown): void => {
        console.error('Error updating user ranking share visibility', error);
        this.savingSignal.set(false);
        this.errorSignal.set(true);
        this.toastMessageService.add(
          'error',
          this.translateService.instant('common.error'),
          this.translateService.instant('ratings.share.manage.error')
        );
      }
    });
  }
}
