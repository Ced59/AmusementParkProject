import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges, effect, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PassportRideOccurrenceStatus } from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { PassportVisitQuickCreateComponent } from '@features/profile/passport/components/passport-visit-quick-create/passport-visit-quick-create.component';
import { RatingInputComponent } from '@shared/components/rating-input/rating-input.component';
import { LocalizedPluralPipe } from '@shared/pipes/localized-plural.pipe';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  formatParkItemRideReferenceDate
} from '../mappers/park-item-passport-ride.mapper';
import {
  ParkItemPassportRideDraft,
  ParkItemPassportRideOutcome,
  ParkItemPassportRideVisitOption
} from '../models/park-item-passport-ride.models';
import { ParkItemPassportRideStateFacade } from '../state/park-item-passport-ride-state.facade';

interface ParkItemPassportRideStatusOption {
  value: PassportRideOccurrenceStatus;
  labelKey: string;
}

type ParkItemPassportRideForm = FormGroup<{
  count: FormControl<number>;
  status: FormControl<PassportRideOccurrenceStatus>;
  localTime: FormControl<string>;
  isApproximate: FormControl<boolean>;
  rating: FormControl<number | null>;
  confirmHistoricalConflict: FormControl<boolean>;
}>;

@Component({
  selector: 'app-park-item-passport-ride-panel',
  templateUrl: './park-item-passport-ride-panel.component.html',
  styleUrl: './park-item-passport-ride-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemPassportRideStateFacade],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    PassportVisitQuickCreateComponent,
    RatingInputComponent,
    LocalizedPluralPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class ParkItemPassportRidePanelComponent implements OnChanges {
  @Input({ required: true }) parkItemId: string = '';
  @Input({ required: true }) parkItemName: string = '';
  @Input({ required: true }) parkId: string = '';
  @Input() parkName: string = '';
  @Input() currentLanguage: string = 'en';

  protected readonly expanded = signal<boolean>(false);
  protected readonly quickCreateVisible = signal<boolean>(false);
  protected readonly facade: ParkItemPassportRideStateFacade;
  protected readonly statusOptions: readonly ParkItemPassportRideStatusOption[] = [
    { value: 'Completed', labelKey: 'passport.editor.status.completed' },
    { value: 'Attempted', labelKey: 'passport.editor.status.attempted' },
    { value: 'MissedClosed', labelKey: 'passport.editor.status.missedClosed' },
    { value: 'MissedUnavailable', labelKey: 'passport.editor.status.missedUnavailable' },
    { value: 'SkippedByChoice', labelKey: 'passport.editor.status.skippedByChoice' }
  ];
  protected readonly form: ParkItemPassportRideForm = new FormGroup({
    count: new FormControl<number>(1, { nonNullable: true }),
    status: new FormControl<PassportRideOccurrenceStatus>('Completed', { nonNullable: true }),
    localTime: new FormControl<string>('', { nonNullable: true }),
    isApproximate: new FormControl<boolean>(false, { nonNullable: true }),
    rating: new FormControl<number | null>(null),
    confirmHistoricalConflict: new FormControl<boolean>(false, { nonNullable: true })
  });
  private targetKey: string = '';

  constructor(facade: ParkItemPassportRideStateFacade) {
    this.facade = facade;
    effect((): void => {
      const selectedVisit: ParkItemPassportRideVisitOption | null = this.facade.selectedVisit();
      if (selectedVisit && !selectedVisit.acceptsLocalTime) {
        this.form.patchValue({ localTime: '', isApproximate: false }, { emitEvent: false });
      }
    });
  }

  ngOnChanges(_changes: SimpleChanges): void {
    const nextTargetKey: string = `${this.parkId.trim()}:${this.parkItemId.trim()}`;
    this.facade.configure({
      parkItemId: this.parkItemId,
      parkItemName: this.parkItemName,
      parkId: this.parkId,
      parkName: this.parkName,
      language: this.currentLanguage
    });
    if (nextTargetKey === this.targetKey) {
      return;
    }

    this.targetKey = nextTargetKey;
    this.expanded.set(false);
    this.quickCreateVisible.set(false);
    this.resetDraft();
  }

  protected open(): void {
    this.expanded.set(true);
    this.facade.load();
  }

  protected close(): void {
    this.expanded.set(false);
    this.facade.clearError();
  }

  protected selectVisit(visitId: string): void {
    this.form.controls.confirmHistoricalConflict.setValue(false);
    this.facade.selectVisit(visitId);
  }

  protected isSelected(visitId: string): boolean {
    return this.facade.selectedVisitId() === visitId;
  }

  protected decreaseCount(): void {
    this.form.controls.count.setValue(Math.max(1, this.normalizedCount() - 1));
  }

  protected increaseCount(): void {
    this.form.controls.count.setValue(Math.min(100, this.normalizedCount() + 1));
    this.clearGroupedRating();
  }

  protected normalizeCount(): void {
    this.form.controls.count.setValue(this.normalizedCount());
    this.clearGroupedRating();
  }

  protected onCountInput(): void {
    this.clearGroupedRating();
  }

  protected updateRating(value: number | null): void {
    this.form.controls.rating.setValue(value);
  }

  protected submit(): void {
    const visitId: string = this.facade.selectedVisitId() ?? '';
    const value = this.form.getRawValue();
    const draft: ParkItemPassportRideDraft = {
      visitId,
      count: value.count,
      status: value.status,
      localTime: value.localTime,
      isApproximate: value.isApproximate,
      rating: value.rating,
      confirmHistoricalConflict: value.confirmHistoricalConflict
    };
    this.facade.addRide(draft);
  }

  protected openQuickCreate(): void {
    this.quickCreateVisible.set(true);
  }

  protected onQuickCreateVisibleChange(visible: boolean): void {
    this.quickCreateVisible.set(visible);
  }

  protected onVisitCreated(visit: PassportVisit): void {
    this.quickCreateVisible.set(false);
    this.facade.addCreatedVisit(visit);
  }

  protected outcomeKey(outcome: ParkItemPassportRideOutcome): string {
    if (outcome === 'rideAndRatingSaved') {
      return 'parkItems.passportRide.success.rideAndRating';
    }

    return outcome === 'rideSavedRatingFailed'
      ? 'parkItems.passportRide.success.ratingFailed'
      : 'parkItems.passportRide.success.ride';
  }

  protected formatReferenceDate(value: string | null): string {
    return formatParkItemRideReferenceDate(value, this.currentLanguage);
  }

  protected startAnotherRide(): void {
    this.facade.dismissOutcome();
    this.resetDraft();
  }

  protected trackVisit(_index: number, visit: ParkItemPassportRideVisitOption): string {
    return visit.id;
  }

  private normalizedCount(): number {
    const value: number = this.form.controls.count.value;
    return Math.min(100, Math.max(1, Number.isFinite(value) ? Math.trunc(value) : 1));
  }

  private clearGroupedRating(): void {
    if (this.normalizedCount() > 1 && this.form.controls.rating.value !== null) {
      this.form.controls.rating.setValue(null);
    }
  }

  private resetDraft(): void {
    this.form.reset({
      count: 1,
      status: 'Completed',
      localTime: '',
      isApproximate: false,
      rating: null,
      confirmHistoricalConflict: false
    });
  }
}
