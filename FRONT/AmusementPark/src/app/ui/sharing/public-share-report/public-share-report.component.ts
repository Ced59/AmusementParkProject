import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ShareModerationReason,
  ShareModerationTargetType,
} from '@app/models/sharing/share-moderation.models';
import { ShareModerationApiService } from '@data-access/sharing/share-moderation-api.service';
import { UiButtonDirective } from '@ui/primitives';
import { PUBLIC_SHARE_REPORT_PORT } from './public-share-report-state-data.ports';
import { PublicShareReportStateFacade } from './public-share-report-state.facade';
import { ShareModerationReasonOption } from './share-moderation-reason-option.model';

@Component({
  selector: 'app-public-share-report',
  templateUrl: './public-share-report.component.html',
  styleUrl: './public-share-report.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, TranslateModule, UiButtonDirective],
  providers: [
    PublicShareReportStateFacade,
    { provide: PUBLIC_SHARE_REPORT_PORT, useExisting: ShareModerationApiService },
  ],
})
export class PublicShareReportComponent {
  public readonly targetType = input.required<ShareModerationTargetType>();
  public readonly shareId = input.required<string>();
  protected readonly reason = signal<ShareModerationReason>('PersonalData');
  protected readonly details = signal<string>('');
  protected readonly reasons: readonly ShareModerationReasonOption[] = [
    { value: 'PersonalData', labelKey: 'shareModeration.reasons.PersonalData' },
    { value: 'HarassmentOrHate', labelKey: 'shareModeration.reasons.HarassmentOrHate' },
    { value: 'Impersonation', labelKey: 'shareModeration.reasons.Impersonation' },
    { value: 'InappropriateContent', labelKey: 'shareModeration.reasons.InappropriateContent' },
    { value: 'SpamOrUnsafeLink', labelKey: 'shareModeration.reasons.SpamOrUnsafeLink' },
    { value: 'MisleadingContent', labelKey: 'shareModeration.reasons.MisleadingContent' },
    { value: 'Other', labelKey: 'shareModeration.reasons.Other' },
  ];

  public constructor(protected readonly facade: PublicShareReportStateFacade) {}

  protected selectReason(value: string): void {
    if (this.reasons.some((option: ShareModerationReasonOption): boolean => option.value === value)) {
      this.reason.set(value as ShareModerationReason);
    }
  }

  protected updateDetails(value: string): void {
    this.details.set(value.slice(0, 500));
  }

  protected canSubmit(): boolean {
    return this.shareId().trim().length > 0
      && (this.reason() !== 'Other' || this.details().trim().length > 0)
      && !this.facade.submitting();
  }

  protected submit(): void {
    if (!this.canSubmit()) {
      return;
    }
    this.facade.submit(
      this.targetType(),
      this.shareId(),
      this.reason(),
      this.details(),
    );
  }
}
