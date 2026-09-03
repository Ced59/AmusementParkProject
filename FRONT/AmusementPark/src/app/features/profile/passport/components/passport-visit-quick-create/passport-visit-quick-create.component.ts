import { ChangeDetectionStrategy, Component, DestroyRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import { PassportVisitDatePrecision } from '@app/models/passport/passport-visit.models';
import { Dialog } from '@shared/ui/primitives/dialog';
import { UiButtonDirective } from '@ui/primitives';
import { PassportParkOption, PassportVisitQuickCreateDraft } from '../../models/passport-visit-quick-create.models';
import { PassportVisitQuickCreateStateFacade } from '../../state/passport-visit-quick-create-state.facade';

type PassportVisitQuickCreateForm = FormGroup<{
  parkId: FormControl<string>;
  precision: FormControl<PassportVisitDatePrecision>;
  year: FormControl<number | null>;
  month: FormControl<number | null>;
  day: FormControl<number | null>;
  isApproximate: FormControl<boolean>;
  timeZoneId: FormControl<string>;
  title: FormControl<string>;
  privateNote: FormControl<string>;
}>;

@Component({
  selector: 'app-passport-visit-quick-create',
  templateUrl: './passport-visit-quick-create.component.html',
  styleUrl: './passport-visit-quick-create.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportVisitQuickCreateStateFacade],
  imports: [Dialog, ReactiveFormsModule, TranslateModule, UiButtonDirective]
})
export class PassportVisitQuickCreateComponent implements OnChanges {
  @Input() visible: boolean = false;
  @Input() fixedParkId: string | null = null;
  @Input() fixedParkName: string | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();

  protected readonly facade = inject(PassportVisitQuickCreateStateFacade);
  protected readonly selectedParkName = signal<string | null>(null);
  protected readonly parkSearchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly form: PassportVisitQuickCreateForm = new FormGroup({
    parkId: new FormControl<string>('', { nonNullable: true }),
    precision: new FormControl<PassportVisitDatePrecision>('Day', { nonNullable: true }),
    year: new FormControl<number | null>(null),
    month: new FormControl<number | null>(null),
    day: new FormControl<number | null>(null),
    isApproximate: new FormControl<boolean>(false, { nonNullable: true }),
    timeZoneId: new FormControl<string>('', { nonNullable: true }),
    title: new FormControl<string>('', { nonNullable: true }),
    privateNote: new FormControl<string>('', { nonNullable: true })
  });

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor() {
    this.parkSearchControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((term: string): void => this.facade.searchParks(term));

    this.form.controls.precision.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((precision: PassportVisitDatePrecision): void => this.applyPrecision(precision));
  }

  ngOnChanges(changes: SimpleChanges): void {
    const fixedParkChange = changes['fixedParkId'];
    if (fixedParkChange
      && !fixedParkChange.firstChange
      && fixedParkChange.previousValue !== fixedParkChange.currentValue) {
      this.resetForm();
      return;
    }

    if (changes['fixedParkId'] || changes['fixedParkName'] || changes['visible']) {
      this.applyFixedPark();
    }
  }

  protected setPrecision(precision: PassportVisitDatePrecision): void {
    this.form.controls.precision.setValue(precision);
  }

  protected useToday(): void {
    const today: Date = new Date();
    this.form.patchValue({
      precision: 'Day',
      year: today.getFullYear(),
      month: today.getMonth() + 1,
      day: today.getDate(),
      isApproximate: false
    });
  }

  protected selectPark(option: PassportParkOption): void {
    this.form.controls.parkId.setValue(option.id);
    this.selectedParkName.set(option.name);
    this.parkSearchControl.setValue('', { emitEvent: false });
    this.facade.clearParkSearch();
  }

  protected clearSelectedPark(): void {
    if (this.fixedParkId) {
      return;
    }

    this.form.controls.parkId.setValue('');
    this.selectedParkName.set(null);
    this.parkSearchControl.setValue('');
  }

  protected submit(): void {
    const draft: PassportVisitQuickCreateDraft = this.form.getRawValue();
    this.facade.createVisit(draft);
  }

  protected close(): void {
    const shouldReset: boolean = this.facade.createdVisit() !== null;
    this.visible = false;
    this.visibleChange.emit(false);
    if (shouldReset) {
      this.resetForm();
    }
  }

  protected onDialogVisibleChange(visible: boolean): void {
    this.visible = visible;
    this.visibleChange.emit(visible);
    if (!visible && this.facade.createdVisit()) {
      this.resetForm();
    }
  }

  protected startAnotherVisit(): void {
    this.resetForm();
  }

  protected trackPark(_index: number, option: PassportParkOption): string {
    return option.id;
  }

  private resetForm(): void {
    this.facade.clearCreationResult();
    this.facade.clearParkSearch();
    this.parkSearchControl.setValue('', { emitEvent: false });
    this.selectedParkName.set(null);
    this.form.reset({
      parkId: this.fixedParkId?.trim() ?? '',
      precision: 'Day',
      year: null,
      month: null,
      day: null,
      isApproximate: false,
      timeZoneId: '',
      title: '',
      privateNote: ''
    });
    this.applyFixedPark();
  }

  private applyPrecision(precision: PassportVisitDatePrecision): void {
    if (precision === 'Year') {
      this.form.patchValue({ month: null, day: null }, { emitEvent: false });
      return;
    }

    if (precision === 'Month') {
      this.form.controls.day.setValue(null, { emitEvent: false });
    }
  }

  private applyFixedPark(): void {
    const parkId: string = this.fixedParkId?.trim() ?? '';
    if (!parkId) {
      return;
    }

    this.form.controls.parkId.setValue(parkId, { emitEvent: false });
    this.selectedParkName.set(this.fixedParkName?.trim() || parkId);
  }
}
