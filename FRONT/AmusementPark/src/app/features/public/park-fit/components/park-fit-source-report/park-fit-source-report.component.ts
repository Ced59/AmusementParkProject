import { ChangeDetectionStrategy, Component, Input, Signal, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkFitEvidenceKind,
  ParkFitSourceReportReason,
  ParkFitSourceReportRequest
} from '@app/models/park-fit/park-fit-search.models';
import { UiButtonDirective } from '@ui/primitives';
import {
  ParkFitSourceReportFacade,
  ParkFitSourceReportSubmissionStatus
} from '../../state/park-fit-source-report.facade';

@Component({
  selector: 'app-park-fit-source-report',
  templateUrl: './park-fit-source-report.component.html',
  styleUrl: './park-fit-source-report.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkFitSourceReportFacade],
  imports: [ReactiveFormsModule, TranslateModule, UiButtonDirective]
})
export class ParkFitSourceReportComponent {
  @Input({ required: true }) public parkId = '';
  @Input({ required: true }) public evidenceKind: ParkFitEvidenceKind = 'GeneralParkData';
  @Input() public sourceUrl: string | null = null;
  @Input() public sourceReference: string | null = null;

  protected readonly expanded = signal<boolean>(false);
  protected readonly status: Signal<ParkFitSourceReportSubmissionStatus> = this.facade.status;
  protected readonly form = new FormGroup({
    reason: new FormControl<ParkFitSourceReportReason>('Outdated', { nonNullable: true }),
    details: new FormControl<string>('', { nonNullable: true })
  });

  constructor(private readonly facade: ParkFitSourceReportFacade) {
  }

  protected toggle(): void {
    this.expanded.update((value: boolean): boolean => !value);
    this.facade.reset();
  }

  protected submit(): void {
    const details: string = this.form.controls.details.value.trim();
    const request: ParkFitSourceReportRequest = {
      parkId: this.parkId,
      evidenceKind: this.evidenceKind,
      sourceUrl: this.sourceUrl,
      sourceReference: this.sourceReference,
      reason: this.form.controls.reason.value,
      details: details.length > 0 ? details : null
    };
    this.facade.submit(request);
  }
}
