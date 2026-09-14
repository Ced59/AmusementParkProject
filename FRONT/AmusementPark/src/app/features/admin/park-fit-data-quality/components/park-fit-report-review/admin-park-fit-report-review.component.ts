import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { ParkFitSourceReport } from '@app/models/admin/park-fit/park-fit-data-quality.models';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { AdminParkFitDataQualityFacade } from '../../state/admin-park-fit-data-quality.facade';

@Component({
  selector: 'app-admin-park-fit-report-review',
  templateUrl: './admin-park-fit-report-review.component.html',
  styleUrl: './admin-park-fit-report-review.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule, TranslateModule, ButtonDirective]
})
export class AdminParkFitReportReviewComponent {
  @Input({ required: true }) public report!: ParkFitSourceReport;

  protected readonly note = new FormControl<string>('', { nonNullable: true });

  constructor(private readonly facade: AdminParkFitDataQualityFacade) {
  }

  protected get processing(): boolean {
    return this.facade.isProcessing(`report:${this.report.reportId}`);
  }

  protected review(decision: 'Resolved' | 'Dismissed'): void {
    const note: string = this.note.value.trim();
    this.facade.reviewReport(this.report, decision, note.length > 0 ? note : null);
  }
}
