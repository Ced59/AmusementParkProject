import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ShareModerationDecision,
  ShareModerationReason,
  ShareModerationReport,
  ShareModerationReportStatus,
  ShareModerationTargetType,
} from '@app/models/sharing/share-moderation.models';
import { AdminShareModerationStateFacade } from '@features/admin/share-moderation/state/admin-share-moderation-state.facade';
import { UiButtonDirective } from '@ui/primitives';

@Component({
  selector: 'app-admin-share-moderation',
  templateUrl: './admin-share-moderation.component.html',
  styleUrl: './admin-share-moderation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminShareModerationStateFacade],
  imports: [DatePipe, FormsModule, TranslateModule, UiButtonDirective],
})
export class AdminShareModerationComponent implements OnInit {
  protected readonly status = signal<ShareModerationReportStatus | ''>('Pending');
  protected readonly targetType = signal<ShareModerationTargetType | ''>('');
  protected readonly reason = signal<ShareModerationReason | ''>('');
  protected readonly notes = signal<Readonly<Record<string, string>>>({});
  protected readonly statuses: readonly ShareModerationReportStatus[] = [
    'Pending', 'PublicationSuspended', 'PublicationRestored', 'Dismissed',
  ];
  protected readonly targetTypes: readonly ShareModerationTargetType[] = [
    'VisitRecap', 'YearRecap', 'PassportProfile', 'PersonalRanking', 'ProfileComparison',
  ];
  protected readonly reasons: readonly ShareModerationReason[] = [
    'PersonalData', 'HarassmentOrHate', 'Impersonation', 'InappropriateContent',
    'SpamOrUnsafeLink', 'MisleadingContent', 'Other',
  ];

  public constructor(protected readonly facade: AdminShareModerationStateFacade) {}

  public ngOnInit(): void {
    this.load(1);
  }

  protected applyFilters(): void {
    this.load(1);
  }

  protected resetFilters(): void {
    this.status.set('Pending');
    this.targetType.set('');
    this.reason.set('');
    this.load(1);
  }

  protected changeStatus(value: string): void {
    this.status.set(this.statuses.includes(value as ShareModerationReportStatus)
      ? value as ShareModerationReportStatus : '');
  }

  protected changeTargetType(value: string): void {
    this.targetType.set(this.targetTypes.includes(value as ShareModerationTargetType)
      ? value as ShareModerationTargetType : '');
  }

  protected changeReason(value: string): void {
    this.reason.set(this.reasons.includes(value as ShareModerationReason)
      ? value as ShareModerationReason : '');
  }

  protected updateNote(reportId: string, value: string): void {
    this.notes.update((notes: Readonly<Record<string, string>>): Readonly<Record<string, string>> => ({
      ...notes,
      [reportId]: value.slice(0, 500),
    }));
  }

  protected note(reportId: string): string {
    return this.notes()[reportId] ?? '';
  }

  protected review(report: ShareModerationReport, decision: ShareModerationDecision): void {
    this.facade.review(report.reportId, decision, this.note(report.reportId));
  }

  protected previousPage(): void {
    const page: number = this.facade.pagination()?.currentPage ?? 1;
    if (page > 1) {
      this.load(page - 1);
    }
  }

  protected nextPage(): void {
    const pagination = this.facade.pagination();
    if (pagination && pagination.currentPage < pagination.totalPages) {
      this.load(pagination.currentPage + 1);
    }
  }

  private load(page: number): void {
    this.facade.load({
      page,
      size: 20,
      status: this.status() || undefined,
      targetType: this.targetType() || undefined,
      reason: this.reason() || undefined,
    });
  }
}
