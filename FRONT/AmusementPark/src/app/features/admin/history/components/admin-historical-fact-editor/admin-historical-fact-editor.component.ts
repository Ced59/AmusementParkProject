import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminHistoricalEvidence,
  AdminHistoricalFact,
  AdminHistoricalLocalizedText,
  AdminHistoricalPeriod,
  AdminHistoricalSource,
  AdminHistoricalSubject,
  HistoricalDateRequest,
  HistoricalFactState,
  HistoricalPeriodRequest,
  HistoricalSubjectType,
  SaveHistoricalFactRequest
} from '@app/models/history/admin-historical-workbench.models';
import { HISTORICAL_FACT_TYPES, HISTORICAL_LANGUAGE_CODES } from '../../models/admin-historical-workbench-options';

interface HistoricalFactForm {
  subjectKey: FormControl<string>;
  type: FormControl<string>;
  state: FormControl<HistoricalFactState>;
  importance: FormControl<'Standard' | 'Major'>;
  precision: FormControl<'Year' | 'Month' | 'Day'>;
  startYear: FormControl<number>;
  startMonth: FormControl<number | null>;
  startDay: FormControl<number | null>;
  hasEnd: FormControl<boolean>;
  endYear: FormControl<number | null>;
  endMonth: FormControl<number | null>;
  endDay: FormControl<number | null>;
  approximate: FormControl<boolean>;
  confidence: FormControl<string>;
  structuredValue: FormControl<string>;
  otherTypeLabel: FormControl<string>;
  reviewNote: FormControl<string>;
  uncertaintyFr: FormControl<string>;
  uncertaintyEn: FormControl<string>;
  uncertaintyDe: FormControl<string>;
  uncertaintyNl: FormControl<string>;
  uncertaintyIt: FormControl<string>;
  uncertaintyEs: FormControl<string>;
  uncertaintyPl: FormControl<string>;
  uncertaintyPt: FormControl<string>;
}

@Component({
  selector: 'app-admin-historical-fact-editor',
  templateUrl: './admin-historical-fact-editor.component.html',
  styleUrl: './admin-historical-fact-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslateModule]
})
export class AdminHistoricalFactEditorComponent implements OnChanges {
  @Input() public subjects: readonly AdminHistoricalSubject[] = [];
  @Input() public sources: readonly AdminHistoricalSource[] = [];
  @Input() public fact: AdminHistoricalFact | null = null;
  @Input() public busy = false;
  @Output() public readonly submitted = new EventEmitter<{ factId: string | null; request: SaveHistoricalFactRequest }>();
  @Output() public readonly cancelled = new EventEmitter<void>();

  protected readonly factTypes = HISTORICAL_FACT_TYPES;
  protected readonly languages = HISTORICAL_LANGUAGE_CODES;
  protected readonly selectedSourceIds = signal<ReadonlyMap<string, number>>(new Map<string, number>());
  protected readonly contradictingSourceId = signal<string | null>(null);
  protected readonly uncertaintyMissing = signal<boolean>(false);
  protected readonly form = new FormGroup<HistoricalFactForm>({
    subjectKey: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    type: new FormControl<string>('Opening', { nonNullable: true, validators: [Validators.required] }),
    state: new FormControl<HistoricalFactState>('Unverified', { nonNullable: true }),
    importance: new FormControl<'Standard' | 'Major'>('Standard', { nonNullable: true }),
    precision: new FormControl<'Year' | 'Month' | 'Day'>('Year', { nonNullable: true }),
    startYear: new FormControl<number>(new Date().getFullYear(), { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.max(9999)] }),
    startMonth: new FormControl<number | null>(null),
    startDay: new FormControl<number | null>(null),
    hasEnd: new FormControl<boolean>(false, { nonNullable: true }),
    endYear: new FormControl<number | null>(null),
    endMonth: new FormControl<number | null>(null),
    endDay: new FormControl<number | null>(null),
    approximate: new FormControl<boolean>(false, { nonNullable: true }),
    confidence: new FormControl<string>('Confirmed', { nonNullable: true }),
    structuredValue: new FormControl<string>('', { nonNullable: true }),
    otherTypeLabel: new FormControl<string>('', { nonNullable: true }),
    reviewNote: new FormControl<string>('', { nonNullable: true }),
    uncertaintyFr: new FormControl<string>('', { nonNullable: true }),
    uncertaintyEn: new FormControl<string>('', { nonNullable: true }),
    uncertaintyDe: new FormControl<string>('', { nonNullable: true }),
    uncertaintyNl: new FormControl<string>('', { nonNullable: true }),
    uncertaintyIt: new FormControl<string>('', { nonNullable: true }),
    uncertaintyEs: new FormControl<string>('', { nonNullable: true }),
    uncertaintyPl: new FormControl<string>('', { nonNullable: true }),
    uncertaintyPt: new FormControl<string>('', { nonNullable: true })
  });

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['fact'] || changes['subjects']) {
      this.resetFromFact();
    }
  }

  protected toggleSource(sourceId: string): void {
    const selected: Map<string, number> = new Map(this.selectedSourceIds());
    if (selected.has(sourceId)) {
      selected.delete(sourceId);
      if (this.contradictingSourceId() === sourceId) {
        this.contradictingSourceId.set(null);
      }
    } else {
      const source: AdminHistoricalSource | undefined = this.sources.find(
        (candidate: AdminHistoricalSource): boolean => candidate.id === sourceId
      );
      if (!source) {
        return;
      }

      selected.set(sourceId, source.revision);
    }

    this.selectedSourceIds.set(selected);
  }

  protected markContradicting(sourceId: string): void {
    if (!this.selectedSourceIds().has(sourceId)) {
      return;
    }

    this.contradictingSourceId.set(this.contradictingSourceId() === sourceId ? null : sourceId);
  }

  protected isUncertain(): boolean {
    const state: HistoricalFactState = this.form.controls.state.value;
    return state === 'Probable' || state === 'Disputed';
  }

  protected submit(): void {
    this.uncertaintyMissing.set(false);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const subject: AdminHistoricalSubject | undefined = this.subjects.find(
      (candidate: AdminHistoricalSubject): boolean => this.subjectKey(candidate) === value.subjectKey
    );
    if (!subject) {
      return;
    }

    const explanations: AdminHistoricalLocalizedText[] = this.buildExplanations(value);
    if ((value.state === 'Probable' || value.state === 'Disputed') && explanations.length !== this.languages.length) {
      this.uncertaintyMissing.set(true);
      return;
    }

    const period: HistoricalPeriodRequest | null = this.buildPeriod(value);
    if (!period) {
      this.form.markAllAsTouched();
      return;
    }

    const preservesFactType: boolean = this.fact?.type === value.type;
    const lifecycleBoundaryMeaning: string | null = preservesFactType
      ? this.fact?.lifecycleBoundaryMeaning ?? null
      : this.lifecycleBoundary(value.type);
    const attributeKind: string | null = preservesFactType
      ? this.fact?.attributeKind ?? null
      : this.attributeKind(value.type);
    const selectedSourceIds: ReadonlyMap<string, number> = this.selectedSourceIds();
    this.submitted.emit({
      factId: this.fact?.id ?? null,
      request: {
        expectedRevision: this.fact?.revision ?? null,
        subjectType: subject.type,
        subjectId: subject.id,
        type: value.type,
        period,
        state: value.state,
        importance: value.importance,
        publicUncertaintyExplanation: explanations,
        lifecycleBoundaryMeaning,
        attributeKind,
        attributeBoundaryMeaning: preservesFactType
          ? this.fact?.attributeBoundaryMeaning ?? null
          : attributeKind ? 'FirstDayOfNewValue' : null,
        sequenceWithinDate: this.fact?.sequenceWithinDate ?? null,
        sources: Array.from(selectedSourceIds.entries()).map(([sourceId, revision]: [string, number]) => ({
          sourceId,
          revision,
          position: this.contradictingSourceId() === sourceId ? 'Contradicts' as const : 'Supports' as const
        })),
        structuredValue: this.optional(value.structuredValue),
        otherTypeLabel: value.type === 'Other' ? this.optional(value.otherTypeLabel) : null,
        narrativeContentId: this.fact?.narrativeContentId ?? null,
        reviewNote: this.optional(value.reviewNote)
      }
    });
  }

  protected subjectKey(subject: AdminHistoricalSubject): string {
    return `${subject.type}::${subject.id}`;
  }

  protected formatSource(source: AdminHistoricalSource): string {
    return `${source.title} · ${source.publisherOrAuthor}`;
  }

  private resetFromFact(): void {
    const fact: AdminHistoricalFact | null = this.fact;
    const start = fact?.period.start;
    const end = fact?.period.end;
    const explanations: Record<string, string> = Object.fromEntries(
      (fact?.publicUncertaintyExplanation ?? []).map((item: AdminHistoricalLocalizedText) => [item.languageCode, item.value])
    );
    this.form.reset({
      subjectKey: fact ? this.subjectKey(fact.subject) : this.subjects[0] ? this.subjectKey(this.subjects[0]) : '',
      type: fact?.type ?? 'Opening',
      state: fact?.state === 'Retracted' ? 'Unverified' : fact?.state ?? 'Unverified',
      importance: fact?.importance ?? 'Standard',
      precision: start?.precision ?? 'Year',
      startYear: start?.year ?? new Date().getFullYear(),
      startMonth: start?.month ?? null,
      startDay: start?.day ?? null,
      hasEnd: !!end && JSON.stringify(start) !== JSON.stringify(end),
      endYear: end?.year ?? null,
      endMonth: end?.month ?? null,
      endDay: end?.day ?? null,
      approximate: start?.isApproximate ?? false,
      confidence: fact?.period.startConfidence ?? 'Confirmed',
      structuredValue: fact?.structuredValue ?? '',
      otherTypeLabel: fact?.otherTypeLabel ?? '',
      reviewNote: '',
      uncertaintyFr: explanations['fr'] ?? '',
      uncertaintyEn: explanations['en'] ?? '',
      uncertaintyDe: explanations['de'] ?? '',
      uncertaintyNl: explanations['nl'] ?? '',
      uncertaintyIt: explanations['it'] ?? '',
      uncertaintyEs: explanations['es'] ?? '',
      uncertaintyPl: explanations['pl'] ?? '',
      uncertaintyPt: explanations['pt'] ?? ''
    });
    this.selectedSourceIds.set(new Map(
      (fact?.sources ?? []).map((source: AdminHistoricalEvidence): [string, number] => [source.sourceId, source.revision])
    ));
    this.contradictingSourceId.set(fact?.sources.find((source): boolean => source.position === 'Contradicts')?.sourceId ?? null);
    this.uncertaintyMissing.set(false);
  }

  private buildPeriod(value: ReturnType<HistoricalFactFormGroup['getRawValue']>): HistoricalPeriodRequest | null {
    if (this.fact && !this.hasPeriodEditorChanged(value, this.fact.period)) {
      return this.toPeriodRequest(this.fact.period);
    }

    const originalStartQualifier: string | null = this.resolvePreservedQualifier(
      this.fact?.period.start?.qualifier ?? null,
      value.approximate
    );
    const start: HistoricalDateRequest | null = this.buildDate(
      value.startYear,
      value.startMonth,
      value.startDay,
      value.precision,
      value.approximate,
      originalStartQualifier
    );
    const originalEnd: HistoricalDateRequest | null = this.fact?.period.end ?? null;
    const preserveOriginalEnd: boolean = !!originalEnd
      && value.hasEnd
      && !this.hasEndEditorChanged(value, this.fact?.period ?? null);
    const endApproximation: boolean = this.resolveEndApproximation(value);
    const end: HistoricalDateRequest | null = preserveOriginalEnd && originalEnd
      ? { ...originalEnd }
      : value.hasEnd
        ? this.buildDate(
            value.endYear,
            value.endMonth,
            value.endDay,
            originalEnd?.precision ?? value.precision,
            endApproximation,
            this.resolvePreservedQualifier(originalEnd?.qualifier ?? null, endApproximation)
          )
        : originalEnd === null ? null : start;
    if (!start || value.hasEnd && !end) {
      return null;
    }

    return {
      start,
      end,
      startConfidence: value.confidence,
      endConfidence: this.resolveEndConfidence(value)
    };
  }

  private buildDate(
    year: number | null,
    month: number | null,
    day: number | null,
    precision: 'Year' | 'Month' | 'Day',
    approximate: boolean,
    qualifier: string | null
  ): HistoricalDateRequest | null {
    if (!year || precision !== 'Year' && !month || precision === 'Day' && !day) {
      return null;
    }

    return {
      year,
      month: precision === 'Year' ? null : month,
      day: precision === 'Day' ? day : null,
      precision,
      isApproximate: approximate,
      qualifier
    };
  }

  private hasPeriodEditorChanged(
    value: ReturnType<HistoricalFactFormGroup['getRawValue']>,
    period: AdminHistoricalPeriod
  ): boolean {
    const controls = this.form.controls;
    if (
      controls.precision.dirty
      || controls.startYear.dirty
      || controls.startMonth.dirty
      || controls.startDay.dirty
      || controls.hasEnd.dirty
      || controls.endYear.dirty
      || controls.endMonth.dirty
      || controls.endDay.dirty
      || controls.approximate.dirty
      || controls.confidence.dirty
    ) {
      return true;
    }

    if (!period.start) {
      return false;
    }

    const hasDistinctEnd: boolean = !!period.end && !this.sameDate(period.start, period.end);
    return value.precision !== period.start.precision
      || value.startYear !== period.start.year
      || value.startMonth !== period.start.month
      || value.startDay !== period.start.day
      || value.hasEnd !== hasDistinctEnd
      || hasDistinctEnd && (
        value.endYear !== period.end?.year
        || value.endMonth !== period.end?.month
        || value.endDay !== period.end?.day
      )
      || value.approximate !== period.start.isApproximate
      || value.confidence !== period.startConfidence;
  }

  private sameDate(left: HistoricalDateRequest, right: HistoricalDateRequest): boolean {
    return left.year === right.year
      && left.month === right.month
      && left.day === right.day
      && left.precision === right.precision
      && left.isApproximate === right.isApproximate
      && left.qualifier === right.qualifier;
  }

  private hasEndEditorChanged(
    value: ReturnType<HistoricalFactFormGroup['getRawValue']>,
    period: AdminHistoricalPeriod | null
  ): boolean {
    const controls = this.form.controls;
    if (controls.hasEnd.dirty || controls.endYear.dirty || controls.endMonth.dirty || controls.endDay.dirty) {
      return true;
    }

    const originalHasDistinctEnd: boolean = !!period?.end
      && (!period.start || !this.sameDate(period.start, period.end));
    return value.hasEnd !== originalHasDistinctEnd
      || value.hasEnd && (
        value.endYear !== period?.end?.year
        || value.endMonth !== period?.end?.month
        || value.endDay !== period?.end?.day
      );
  }

  private toPeriodRequest(period: AdminHistoricalPeriod): HistoricalPeriodRequest {
    return {
      start: period.start ? { ...period.start } : null,
      end: period.end ? { ...period.end } : null,
      startConfidence: period.startConfidence,
      endConfidence: period.endConfidence
    };
  }

  private resolveEndApproximation(value: ReturnType<HistoricalFactFormGroup['getRawValue']>): boolean {
    const originalPeriod: AdminHistoricalPeriod | undefined = this.fact?.period;
    return originalPeriod && value.approximate === originalPeriod.start?.isApproximate
      ? originalPeriod.end?.isApproximate ?? value.approximate
      : value.approximate;
  }

  private resolveEndConfidence(value: ReturnType<HistoricalFactFormGroup['getRawValue']>): string {
    const originalPeriod: AdminHistoricalPeriod | undefined = this.fact?.period;
    return originalPeriod && value.confidence === originalPeriod.startConfidence
      ? originalPeriod.endConfidence
      : value.confidence;
  }

  private resolvePreservedQualifier(qualifier: string | null, approximate: boolean): string | null {
    return qualifier === 'Circa' && !approximate ? null : qualifier;
  }

  private buildExplanations(value: ReturnType<HistoricalFactFormGroup['getRawValue']>): AdminHistoricalLocalizedText[] {
    const values: Record<string, string> = {
      fr: value.uncertaintyFr, en: value.uncertaintyEn, de: value.uncertaintyDe, nl: value.uncertaintyNl,
      it: value.uncertaintyIt, es: value.uncertaintyEs, pl: value.uncertaintyPl, pt: value.uncertaintyPt
    };
    return this.languages
      .map((languageCode: string) => ({ languageCode, value: values[languageCode]?.trim() ?? '' }))
      .filter((item: AdminHistoricalLocalizedText): boolean => item.value.length > 0);
  }

  private lifecycleBoundary(type: string): string | null {
    if (type === 'Opening' || type === 'Reopening') {
      return 'FirstOperatingDay';
    }

    return ['Closure', 'TemporaryClosure', 'DefinitiveClosure'].includes(type) ? 'LastOperatingDay' : null;
  }

  private attributeKind(type: string): string | null {
    const attributes: Record<string, string> = {
      Renaming: 'Name', ZoneRenaming: 'Name', OperatorChange: 'Operator', OwnerChange: 'Owner',
      PositioningChange: 'MarketPositioning', Relocation: 'Location', Retheming: 'Theme',
      ManufacturerChange: 'Manufacturer', ZoneMove: 'Zone', LogoChange: 'Logo'
    };
    return attributes[type] ?? null;
  }

  private optional(value: string): string | null {
    const normalizedValue: string = value.trim();
    return normalizedValue || null;
  }
}

type HistoricalFactFormGroup = FormGroup<HistoricalFactForm>;
