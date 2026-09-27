import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnChanges,
  Signal,
  SimpleChanges,
  signal
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { PassportHistoricalExistenceReport } from '@app/models/passport/passport-historical-existence-report.models';
import { UiButtonDirective, UiChipComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  PassportHistoricalExistenceReportFacade,
  PassportHistoricalExistenceReportSubmissionStatus
} from '../../state/passport-historical-existence-report.facade';

@Component({
  selector: 'app-passport-historical-existence-report',
  templateUrl: './passport-historical-existence-report.component.html',
  styleUrl: './passport-historical-existence-report.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportHistoricalExistenceReportFacade],
  imports: [
    ReactiveFormsModule,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiSurfaceDirective
  ]
})
export class PassportHistoricalExistenceReportComponent implements OnChanges {
  @Input({ required: true }) public visitId = '';

  protected readonly expanded = signal<boolean>(false);
  protected readonly reports: Signal<readonly PassportHistoricalExistenceReport[]> =
    this.facade.reports;
  protected readonly loading: Signal<boolean> = this.facade.loading;
  protected readonly loadError: Signal<boolean> = this.facade.loadError;
  protected readonly submissionStatus: Signal<PassportHistoricalExistenceReportSubmissionStatus> =
    this.facade.submissionStatus;
  protected readonly form = new FormGroup({
    claimedName: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(250)]
    }),
    sourceUrl: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000), Validators.pattern(/^https:\/\/[^\s]+$/i)]
    }),
    sourceReference: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)]
    }),
    details: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.maxLength(1000)]
    })
  });

  constructor(private readonly facade: PassportHistoricalExistenceReportFacade) {
  }

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['visitId'] && this.visitId.trim()) {
      this.facade.load(this.visitId);
    }
  }

  protected toggle(): void {
    this.expanded.update((value: boolean): boolean => !value);
    this.facade.resetSubmission();
  }

  protected retry(): void {
    this.facade.load(this.visitId);
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.submissionStatus() === 'submitting') {
      return;
    }

    const sourceUrl: string = this.form.controls.sourceUrl.value.trim();
    const sourceReference: string = this.form.controls.sourceReference.value.trim();
    const details: string = this.form.controls.details.value.trim();
    this.facade.submit(this.visitId, {
      claimedName: this.form.controls.claimedName.value.trim(),
      sourceUrl: sourceUrl || null,
      sourceReference: sourceReference || null,
      details: details || null
    });
  }

  protected statusKey(report: PassportHistoricalExistenceReport): string {
    return `passport.historicalExistenceReport.status.${report.status}`;
  }
}
