import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import {
  FactualChangeEventAdmin,
  FactualChangeStatus,
  FactualDataConfidence,
  FactualEventType,
  FactualTargetType,
} from '@app/models/admin/factual-events/factual-event-administration.models';
import { FactualFactPresentation } from '@app/models/admin/factual-events/factual-fact-presentation.model';
import { presentFactualFact } from '@app/models/admin/factual-events/factual-fact-presenter';
import { AdminFactualEventsStateFacade } from '@features/admin/factual-events/state/admin-factual-events-state.facade';
import { UiButtonDirective } from '@ui/primitives';

@Component({
  selector: 'app-admin-factual-events',
  templateUrl: './admin-factual-events.component.html',
  styleUrl: './admin-factual-events.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminFactualEventsStateFacade],
  imports: [DatePipe, TranslateModule, UiButtonDirective],
})
export class AdminFactualEventsComponent implements OnInit {
  protected readonly status = signal<FactualChangeStatus | ''>('Draft');
  protected readonly targetType = signal<FactualTargetType | ''>('');
  protected readonly eventType = signal<FactualEventType | ''>('');
  protected readonly confidence = signal<FactualDataConfidence | ''>('');
  protected readonly statuses: readonly FactualChangeStatus[] = [
    'Draft', 'Verified', 'Published', 'Corrected', 'Retracted', 'Expired',
  ];
  protected readonly targetTypes: readonly FactualTargetType[] = ['Park', 'ParkItem'];
  protected readonly confidences: readonly FactualDataConfidence[] = ['Low', 'Medium', 'High'];
  protected readonly eventTypes: readonly FactualEventType[] = [
    'OpeningCalendarPublished', 'OpeningCalendarChanged', 'SeasonOpeningConfirmed',
    'SeasonClosingConfirmed', 'ParkTemporaryClosureConfirmed', 'ParkPermanentClosureConfirmed',
    'ParkReopeningConfirmed', 'ParkNameChanged', 'OperatorChanged',
    'TicketPricePublishedOrChanged', 'MajorDataCompletionImproved',
    'AttractionAnnouncedOfficially', 'OpeningDateConfirmed', 'OpeningDateChanged',
    'OpenedConfirmed', 'TemporarilyClosedConfirmed', 'ReopenedConfirmed',
    'PermanentClosureConfirmed', 'Renamed', 'MajorRestrictionChanged',
    'LocationOrCategoryCorrected', 'HistoryPublished', 'MajorHistoryUpdate',
    'VerifiedSourceAdded', 'CorrectionAfterUserReport',
  ];

  public constructor(protected readonly facade: AdminFactualEventsStateFacade) {}

  public ngOnInit(): void {
    this.load(1);
  }

  protected applyFilters(): void {
    this.load(1);
  }

  protected resetFilters(): void {
    this.status.set('Draft');
    this.targetType.set('');
    this.eventType.set('');
    this.confidence.set('');
    this.load(1);
  }

  protected changeStatus(value: string): void {
    this.status.set(this.statuses.includes(value as FactualChangeStatus)
      ? value as FactualChangeStatus
      : '');
  }

  protected changeTargetType(value: string): void {
    this.targetType.set(this.targetTypes.includes(value as FactualTargetType)
      ? value as FactualTargetType
      : '');
  }

  protected changeEventType(value: string): void {
    this.eventType.set(this.eventTypes.includes(value as FactualEventType)
      ? value as FactualEventType
      : '');
  }

  protected changeConfidence(value: string): void {
    this.confidence.set(this.confidences.includes(value as FactualDataConfidence)
      ? value as FactualDataConfidence
      : '');
  }

  protected targetName(event: FactualChangeEventAdmin): string {
    return event.target.name?.trim() || '';
  }

  protected fact(value: FactualChangeEventAdmin['newValue']): FactualFactPresentation {
    return presentFactualFact(value);
  }

  protected verify(event: FactualChangeEventAdmin): void {
    this.facade.changeStatus(event, 'verify');
  }

  protected publish(event: FactualChangeEventAdmin): void {
    this.facade.changeStatus(event, 'publish');
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
      eventType: this.eventType() || undefined,
      confidence: this.confidence() || undefined,
    });
  }
}
