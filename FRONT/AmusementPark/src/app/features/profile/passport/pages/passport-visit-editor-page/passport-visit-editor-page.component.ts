import { ChangeDetectionStrategy, Component, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs';

import {
  PassportRideOccurrence,
  PassportRideOccurrenceStatus
} from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisitDatePrecision, PassportVisitStatus } from '@app/models/passport/passport-visit.models';
import { TranslationService } from '@app/services/translation.service';
import { LocalizedPluralPipe } from '@shared/pipes';
import {
  findNearestLanguageActivatedRoute,
  resolveLanguageFromActivatedRoute,
  resolveLanguageFromParamMap
} from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  PassportAttractionSelectionDraft,
  PassportOccurrenceEditDraft,
  PassportVisitEditorAttraction
} from '../../models/passport-visit-editor.models';
import { PassportVisitEditorStateFacade } from '../../state/passport-visit-editor-state.facade';

interface PassportStatusOption {
  value: PassportRideOccurrenceStatus;
  labelKey: string;
}

const supportedLifecycleStatuses: ReadonlySet<string> = new Set<string>([
  'Operating',
  'UnderConstruction',
  'TemporarilyClosed',
  'ClosedDefinitively',
  'Removed',
  'Planned',
  'Unknown'
]);

@Component({
  selector: 'app-passport-visit-editor-page',
  templateUrl: './passport-visit-editor-page.component.html',
  styleUrl: './passport-visit-editor-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PassportVisitEditorStateFacade],
  imports: [
    ReactiveFormsModule,
    TranslateModule,
    LocalizedPluralPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class PassportVisitEditorPageComponent {
  protected readonly facade: PassportVisitEditorStateFacade;
  protected readonly searchControl = new FormControl<string>('', { nonNullable: true });
  protected readonly zoneControl = new FormControl<string>('', { nonNullable: true });
  protected readonly deleteConfirmationId = signal<string | null>(null);
  protected readonly assessmentDeleteConfirmation = signal<boolean>(false);
  protected readonly rideAssessmentDeleteConfirmationId = signal<string | null>(null);
  protected readonly currentLanguage = signal<string>('en');
  protected readonly assessmentValues: readonly number[] = [0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5];
  protected readonly statusOptions: readonly PassportStatusOption[] = [
    { value: 'Completed', labelKey: 'passport.editor.status.completed' },
    { value: 'Attempted', labelKey: 'passport.editor.status.attempted' },
    { value: 'MissedClosed', labelKey: 'passport.editor.status.missedClosed' },
    { value: 'MissedUnavailable', labelKey: 'passport.editor.status.missedUnavailable' },
    { value: 'SkippedByChoice', labelKey: 'passport.editor.status.skippedByChoice' }
  ];

  private visitId: string;

  constructor(
    facade: PassportVisitEditorStateFacade,
    route: ActivatedRoute,
    private readonly router: Router,
    translationService: TranslationService,
    destroyRef: DestroyRef
  ) {
    this.facade = facade;
    const initialLanguage: string = resolveLanguageFromActivatedRoute(
      route,
      translationService.getCurrentLang() || 'en'
    );
    this.currentLanguage.set(initialLanguage);
    this.visitId = route.snapshot.paramMap.get('visitId')?.trim() ?? '';

    this.facade.load(this.visitId, initialLanguage);

    route.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(destroyRef)
    ).subscribe((params: ParamMap): void => {
      const visitId: string = params.get('visitId')?.trim() ?? '';
      if (!visitId || visitId === this.visitId) {
        return;
      }

      this.visitId = visitId;
      this.deleteConfirmationId.set(null);
      this.assessmentDeleteConfirmation.set(false);
      this.rideAssessmentDeleteConfirmationId.set(null);
      this.searchControl.setValue('', { emitEvent: false });
      this.zoneControl.setValue('', { emitEvent: false });
      this.facade.load(visitId, this.currentLanguage());
    });

    findNearestLanguageActivatedRoute(route)?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(destroyRef)
    ).subscribe((params: ParamMap): void => {
      const language: string = resolveLanguageFromParamMap(params, this.currentLanguage());
      if (language === this.currentLanguage()) {
        return;
      }

      this.currentLanguage.set(language);
      this.facade.changeLanguage(language);
    });
  }

  protected goBack(): void {
    void this.router.navigate(['/', this.currentLanguage(), 'profile']);
  }

  protected retryLoad(): void {
    this.facade.retryLoad();
  }

  protected applyFilters(): void {
    this.facade.applyAttractionFilters(this.searchControl.value, this.zoneControl.value || null);
  }

  protected clearFilters(): void {
    this.searchControl.setValue('');
    this.zoneControl.setValue('');
    this.facade.applyAttractionFilters('', null);
  }

  protected visitStatusLabelKey(status: PassportVisitStatus): string {
    return `passport.editor.visit.status.${status}`;
  }

  protected shouldDisplayAssessment(
    status: PassportVisitStatus | null | undefined,
    hasAssessment: boolean
  ): boolean {
    return status === 'Draft' || hasAssessment;
  }

  protected updateVisitPrecision(event: Event): void {
    this.facade.updateVisitMetadataDraft({
      precision: this.eventValue(event) as PassportVisitDatePrecision
    });
  }

  protected updateVisitYear(event: Event): void {
    this.facade.updateVisitMetadataDraft({ year: this.eventNullableNumber(event) });
  }

  protected updateVisitMonth(event: Event): void {
    this.facade.updateVisitMetadataDraft({ month: this.eventNullableNumber(event) });
  }

  protected updateVisitDay(event: Event): void {
    this.facade.updateVisitMetadataDraft({ day: this.eventNullableNumber(event) });
  }

  protected updateVisitApproximate(event: Event): void {
    this.facade.updateVisitMetadataDraft({ isApproximate: this.eventChecked(event) });
  }

  protected updateVisitTimeZone(event: Event): void {
    this.facade.updateVisitMetadataDraft({ timeZoneId: this.eventValue(event) });
  }

  protected updateVisitTitle(event: Event): void {
    this.facade.updateVisitMetadataDraft({ title: this.eventValue(event) });
  }

  protected updateVisitPrivateNote(event: Event): void {
    this.facade.updateVisitMetadataDraft({ privateNote: this.eventValue(event) });
  }

  protected selectAssessmentValue(value: number): void {
    this.facade.updateParkAssessmentDraft({ value });
  }

  protected updateAssessmentComment(event: Event): void {
    this.facade.updateParkAssessmentDraft({ privateComment: this.eventValue(event) });
  }

  protected saveAssessment(): void {
    this.assessmentDeleteConfirmation.set(false);
    this.facade.saveParkAssessment();
  }

  protected requestAssessmentDelete(): void {
    this.assessmentDeleteConfirmation.set(true);
  }

  protected cancelAssessmentDelete(): void {
    this.assessmentDeleteConfirmation.set(false);
  }

  protected confirmAssessmentDelete(): void {
    this.assessmentDeleteConfirmation.set(false);
    this.facade.deleteParkAssessment();
  }

  protected assessmentValueLabel(value: number): string {
    return new Intl.NumberFormat(this.currentLanguage(), {
      minimumFractionDigits: value % 1 === 0 ? 0 : 1,
      maximumFractionDigits: 1
    }).format(value);
  }

  protected selectRideAssessmentValue(occurrenceId: string, value: number): void {
    this.facade.updateRideAssessmentDraft(occurrenceId, { value });
  }

  protected updateRideAssessmentComment(occurrenceId: string, event: Event): void {
    this.facade.updateRideAssessmentDraft(occurrenceId, { privateComment: this.eventValue(event) });
  }

  protected saveRideAssessment(occurrence: PassportRideOccurrence): void {
    this.rideAssessmentDeleteConfirmationId.set(null);
    this.facade.saveRideAssessment(occurrence);
  }

  protected requestRideAssessmentDelete(occurrenceId: string): void {
    this.rideAssessmentDeleteConfirmationId.set(occurrenceId);
  }

  protected cancelRideAssessmentDelete(): void {
    this.rideAssessmentDeleteConfirmationId.set(null);
  }

  protected confirmRideAssessmentDelete(occurrence: PassportRideOccurrence): void {
    this.rideAssessmentDeleteConfirmationId.set(null);
    this.facade.deleteRideAssessment(occurrence);
  }

  protected toggleAttraction(attraction: PassportVisitEditorAttraction): void {
    this.facade.toggleAttraction(attraction);
  }

  protected selectionFor(parkItemId: string): PassportAttractionSelectionDraft | null {
    return this.facade.selectedAttractions().find(
      (selection: PassportAttractionSelectionDraft): boolean => selection.parkItemId === parkItemId
    ) ?? null;
  }

  protected updateSelectionStatus(parkItemId: string, event: Event): void {
    this.facade.updateSelection(parkItemId, { status: this.eventValue(event) as PassportRideOccurrenceStatus });
  }

  protected updateSelectionCount(parkItemId: string, event: Event): void {
    this.facade.updateSelection(parkItemId, { count: Number(this.eventValue(event)) });
  }

  protected updateSelectionTime(parkItemId: string, event: Event): void {
    this.facade.updateSelection(parkItemId, { localTime: this.eventValue(event) });
  }

  protected updateSelectionApproximate(parkItemId: string, event: Event): void {
    this.facade.updateSelection(parkItemId, { isApproximate: this.eventChecked(event) });
  }

  protected updateSelectionNote(parkItemId: string, event: Event): void {
    this.facade.updateSelection(parkItemId, { privateNote: this.eventValue(event) });
  }

  protected updateSelectionHistoricalConfirmation(parkItemId: string, event: Event): void {
    this.facade.updateSelection(parkItemId, { confirmHistoricalConflict: this.eventChecked(event) });
  }

  protected updateEditStatus(occurrenceId: string, event: Event): void {
    this.patchEditDraft(occurrenceId, { status: this.eventValue(event) as PassportRideOccurrenceStatus });
  }

  protected updateEditTime(occurrenceId: string, event: Event): void {
    this.patchEditDraft(occurrenceId, { localTime: this.eventValue(event) });
  }

  protected updateEditApproximate(occurrenceId: string, event: Event): void {
    this.patchEditDraft(occurrenceId, { isApproximate: this.eventChecked(event) });
  }

  protected updateEditNote(occurrenceId: string, event: Event): void {
    this.patchEditDraft(occurrenceId, { privateNote: this.eventValue(event) });
  }

  protected updateEditHistoricalConfirmation(occurrenceId: string, event: Event): void {
    this.patchEditDraft(occurrenceId, { confirmHistoricalConflict: this.eventChecked(event) });
  }

  protected saveOccurrence(occurrence: PassportRideOccurrence): void {
    const draft: PassportOccurrenceEditDraft | undefined = this.facade.editDrafts()[occurrence.id];
    if (draft) {
      this.facade.updateOccurrence(occurrence, draft);
    }
  }

  protected requestDelete(occurrenceId: string): void {
    this.deleteConfirmationId.set(occurrenceId);
  }

  protected cancelDelete(): void {
    this.deleteConfirmationId.set(null);
  }

  protected confirmDelete(occurrence: PassportRideOccurrence): void {
    this.deleteConfirmationId.set(null);
    this.facade.deleteOccurrence(occurrence);
  }

  protected isOccurrenceBusy(occurrenceId: string): boolean {
    return this.facade.busyOccurrenceIds().has(occurrenceId);
  }

  protected trackAttraction(_index: number, attraction: PassportVisitEditorAttraction): string {
    return attraction.id;
  }

  protected trackSelection(_index: number, selection: PassportAttractionSelectionDraft): string {
    return selection.parkItemId;
  }

  protected visitDateLabel(): string {
    const visit = this.facade.visit();
    if (!visit) {
      return '';
    }

    const date: Date = new Date(Date.UTC(visit.date.year, (visit.date.month ?? 1) - 1, visit.date.day ?? 1));
    const options: Intl.DateTimeFormatOptions = visit.date.precision === 'Year'
      ? { year: 'numeric', timeZone: 'UTC' }
      : visit.date.precision === 'Month'
        ? { month: 'long', year: 'numeric', timeZone: 'UTC' }
        : { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' };
    return new Intl.DateTimeFormat(this.currentLanguage(), options).format(date);
  }

  protected lifecycleLabelKey(status: string | null): string {
    const normalizedStatus: string = status?.trim() ?? '';
    return supportedLifecycleStatuses.has(normalizedStatus)
      ? `passport.editor.lifecycle.${normalizedStatus}`
      : 'passport.editor.lifecycle.Unknown';
  }

  protected statusLabelKey(status: PassportRideOccurrenceStatus): string {
    return this.statusOptions.find((option: PassportStatusOption): boolean => option.value === status)?.labelKey
      ?? 'passport.editor.status.completed';
  }

  protected consistencyLabelKey(consistency: string): string {
    if (consistency === 'Verified') {
      return 'passport.editor.consistency.verified';
    }

    if (consistency === 'ConfirmedConflict') {
      return 'passport.editor.consistency.confirmedConflict';
    }

    return 'passport.editor.consistency.unverified';
  }

  protected timeLabel(localTime: string | null): string {
    return localTime?.slice(0, 5) ?? '';
  }

  private patchEditDraft(occurrenceId: string, patch: Partial<PassportOccurrenceEditDraft>): void {
    this.facade.updateOccurrenceDraft(occurrenceId, patch);
  }

  private eventValue(event: Event): string {
    return event.target instanceof HTMLInputElement
      || event.target instanceof HTMLSelectElement
      || event.target instanceof HTMLTextAreaElement
      ? event.target.value
      : '';
  }

  private eventChecked(event: Event): boolean {
    return event.target instanceof HTMLInputElement && event.target.checked;
  }

  private eventNullableNumber(event: Event): number | null {
    const value: string = this.eventValue(event);
    return value.length > 0 ? Number(value) : null;
  }
}
