import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ProfileComparisonSummary } from '@app/models/sharing/profile-comparison.models';
import {
  PROFILE_COMPARISON_MANAGEMENT_PORT,
  ProfileComparisonManagementPort,
} from './profile-comparison-management-data.ports';

@Injectable()
export class ProfileComparisonManagementStateFacade {
  private readonly comparisonsSignal = signal<ProfileComparisonSummary[]>([]);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly revokingShareIdSignal = signal<string | null>(null);

  public readonly comparisons: Signal<ProfileComparisonSummary[]> =
    this.comparisonsSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  public readonly revokingShareId: Signal<string | null> =
    this.revokingShareIdSignal.asReadonly();

  constructor(
    @Inject(PROFILE_COMPARISON_MANAGEMENT_PORT)
    private readonly port: ProfileComparisonManagementPort,
    private readonly destroyRef: DestroyRef,
  ) {}

  public load(): void {
    if (this.loadingSignal()) {
      return;
    }
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.port
      .listMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (comparisons: ProfileComparisonSummary[]): void => {
          this.comparisonsSignal.set(comparisons);
          this.loadingSignal.set(false);
        },
        error: (): void => {
          this.loadingSignal.set(false);
          this.errorSignal.set(true);
        },
      });
  }

  public revoke(shareId: string): void {
    if (this.revokingShareIdSignal()) {
      return;
    }
    this.revokingShareIdSignal.set(shareId);
    this.errorSignal.set(false);
    this.port
      .revoke(shareId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.comparisonsSignal.update(
            (comparisons: ProfileComparisonSummary[]) =>
              comparisons.filter(
                (comparison: ProfileComparisonSummary): boolean =>
                  comparison.shareId !== shareId,
              ),
          );
          this.revokingShareIdSignal.set(null);
        },
        error: (): void => {
          this.revokingShareIdSignal.set(null);
          this.errorSignal.set(true);
        },
      });
  }
}
