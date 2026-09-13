import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import {
  ShareModerationReason,
  ShareModerationTargetType,
} from '@app/models/sharing/share-moderation.models';
import {
  PUBLIC_SHARE_REPORT_PORT,
  PublicShareReportPort,
} from './public-share-report-state-data.ports';

@Injectable()
export class PublicShareReportStateFacade {
  private readonly submittingState = signal<boolean>(false);
  private readonly submittedState = signal<boolean>(false);
  private readonly errorState = signal<boolean>(false);

  public readonly submitting: Signal<boolean> = this.submittingState.asReadonly();
  public readonly submitted: Signal<boolean> = this.submittedState.asReadonly();
  public readonly error: Signal<boolean> = this.errorState.asReadonly();

  public constructor(
    @Inject(PUBLIC_SHARE_REPORT_PORT) private readonly port: PublicShareReportPort,
  ) {}

  public submit(
    targetType: ShareModerationTargetType,
    shareId: string,
    reason: ShareModerationReason,
    details: string,
  ): void {
    if (this.submittingState() || this.submittedState()) {
      return;
    }

    this.submittingState.set(true);
    this.errorState.set(false);
    const normalizedDetails: string = details.trim();
    this.port
      .submit({
        targetType,
        shareId: shareId.trim(),
        reason,
        details: normalizedDetails.length > 0 ? normalizedDetails : null,
      })
      .pipe(finalize((): void => this.submittingState.set(false)))
      .subscribe({
        next: (): void => this.submittedState.set(true),
        error: (): void => this.errorState.set(true),
      });
  }
}
