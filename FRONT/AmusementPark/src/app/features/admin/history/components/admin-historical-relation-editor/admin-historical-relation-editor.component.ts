import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminHistoricalEvidence,
  AdminHistoricalLocalizedText,
  AdminHistoricalPeriod,
  AdminHistoricalRelation,
  AdminHistoricalSource,
  AdminHistoricalSubject,
  HistoricalDateRequest,
  HistoricalFactState,
  HistoricalPeriodRequest,
  SaveHistoricalRelationRequest
} from '@app/models/history/admin-historical-workbench.models';
import { HISTORICAL_LANGUAGE_CODES, HISTORICAL_RELATION_TYPES } from '../../models/admin-historical-workbench-options';

interface HistoricalRelationForm {
  sourceSubjectKey: FormControl<string>;
  targetSubjectKey: FormControl<string>;
  type: FormControl<string>;
  direction: FormControl<string>;
  state: FormControl<HistoricalFactState>;
  year: FormControl<number>;
  approximate: FormControl<boolean>;
  confidence: FormControl<string>;
  editorialNote: FormControl<string>;
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
  selector: 'app-admin-historical-relation-editor',
  templateUrl: './admin-historical-relation-editor.component.html',
  styleUrl: './admin-historical-relation-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TranslateModule]
})
export class AdminHistoricalRelationEditorComponent implements OnChanges {
  @Input() public subjects: readonly AdminHistoricalSubject[] = [];
  @Input() public sources: readonly AdminHistoricalSource[] = [];
  @Input() public relation: AdminHistoricalRelation | null = null;
  @Input() public busy = false;
  @Output() public readonly submitted = new EventEmitter<{ relationId: string | null; request: SaveHistoricalRelationRequest }>();
  @Output() public readonly cancelled = new EventEmitter<void>();

  protected readonly relationTypes = HISTORICAL_RELATION_TYPES;
  protected readonly languages = HISTORICAL_LANGUAGE_CODES;
  protected readonly selectedSourceIds = signal<ReadonlyMap<string, number>>(new Map<string, number>());
  protected readonly contradictingSourceIds = signal<ReadonlySet<string>>(new Set<string>());
  protected readonly uncertaintyMissing = signal<boolean>(false);
  protected readonly form = new FormGroup<HistoricalRelationForm>({
    sourceSubjectKey: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    targetSubjectKey: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    type: new FormControl<string>('RenamedTo', { nonNullable: true, validators: [Validators.required] }),
    direction: new FormControl<string>('Directed', { nonNullable: true }),
    state: new FormControl<HistoricalFactState>('Unverified', { nonNullable: true }),
    year: new FormControl<number>(new Date().getFullYear(), { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.max(9999)] }),
    approximate: new FormControl<boolean>(false, { nonNullable: true }),
    confidence: new FormControl<string>('Confirmed', { nonNullable: true }),
    editorialNote: new FormControl<string>('', { nonNullable: true }),
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
    if (changes['relation'] || changes['subjects']) {
      this.resetFromRelation();
    }
  }

  protected toggleSource(sourceId: string): void {
    const selected: Map<string, number> = new Map(this.selectedSourceIds());
    if (selected.has(sourceId)) {
      selected.delete(sourceId);
      if (this.contradictingSourceIds().has(sourceId)) {
        const contradictions: Set<string> = new Set(this.contradictingSourceIds());
        contradictions.delete(sourceId);
        this.contradictingSourceIds.set(contradictions);
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
    if (this.selectedSourceIds().has(sourceId)) {
      const contradictions: Set<string> = new Set(this.contradictingSourceIds());
      if (contradictions.has(sourceId)) {
        contradictions.delete(sourceId);
      } else {
        contradictions.add(sourceId);
      }

      this.contradictingSourceIds.set(contradictions);
    }
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
    const sourceSubject: AdminHistoricalSubject | undefined = this.resolveSubject(value.sourceSubjectKey);
    const targetSubject: AdminHistoricalSubject | undefined = this.resolveSubject(value.targetSubjectKey);
    if (!sourceSubject || !targetSubject || sourceSubject === targetSubject) {
      return;
    }

    const explanations: AdminHistoricalLocalizedText[] = this.buildExplanations(value);
    if ((value.state === 'Probable' || value.state === 'Disputed') && explanations.length !== this.languages.length) {
      this.uncertaintyMissing.set(true);
      return;
    }

    const date: HistoricalDateRequest = {
      year: value.year,
      month: null,
      day: null,
      precision: 'Year',
      isApproximate: value.approximate,
      qualifier: null
    };
    const selected: ReadonlyMap<string, number> = this.selectedSourceIds();
    this.submitted.emit({
      relationId: this.relation?.id ?? null,
      request: {
        expectedRevision: this.relation?.revision ?? null,
        sourceSubjectType: sourceSubject.type,
        sourceSubjectId: sourceSubject.id,
        sourceSubjectContextParkId: sourceSubject.contextParkId,
        targetSubjectType: targetSubject.type,
        targetSubjectId: targetSubject.id,
        targetSubjectContextParkId: targetSubject.contextParkId,
        type: value.type,
        direction: value.direction,
        period: this.buildPeriod(value, date),
        state: value.state,
        publicUncertaintyExplanation: explanations,
        sources: Array.from(selected.entries()).map(([sourceId, revision]: [string, number]) => ({
          sourceId,
          revision,
          position: this.contradictingSourceIds().has(sourceId) ? 'Contradicts' as const : 'Supports' as const
        })),
        editorialNote: this.optional(value.editorialNote),
        reviewNote: this.optional(value.reviewNote)
      }
    });
  }

  protected subjectKey(subject: AdminHistoricalSubject): string {
    return `${subject.type}::${subject.id}::${subject.contextParkId ?? ''}`;
  }

  private resetFromRelation(): void {
    const relation: AdminHistoricalRelation | null = this.relation;
    const explanations: Record<string, string> = Object.fromEntries(
      (relation?.publicUncertaintyExplanation ?? []).map((item: AdminHistoricalLocalizedText): [string, string] => [item.languageCode, item.value])
    );
    this.form.reset({
      sourceSubjectKey: relation ? this.subjectKey(relation.source) : this.subjects[0] ? this.subjectKey(this.subjects[0]) : '',
      targetSubjectKey: relation ? this.subjectKey(relation.target) : this.subjects[1] ? this.subjectKey(this.subjects[1]) : '',
      type: relation?.type ?? 'RenamedTo',
      direction: relation?.direction ?? 'Directed',
      state: relation?.state === 'Retracted' ? 'Unverified' : relation?.state ?? 'Unverified',
      year: relation?.period.start?.year ?? new Date().getFullYear(),
      approximate: relation?.period.start?.isApproximate ?? false,
      confidence: relation?.period.startConfidence ?? 'Confirmed',
      editorialNote: relation?.editorialNote ?? '',
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
      (relation?.sources ?? []).map((source: AdminHistoricalEvidence): [string, number] => [source.sourceId, source.revision])
    ));
    this.contradictingSourceIds.set(new Set(
      (relation?.sources ?? [])
        .filter((source: AdminHistoricalEvidence): boolean => source.position === 'Contradicts')
        .map((source: AdminHistoricalEvidence): string => source.sourceId)
    ));
    this.uncertaintyMissing.set(false);
  }

  private buildExplanations(value: ReturnType<HistoricalRelationFormGroup['getRawValue']>): AdminHistoricalLocalizedText[] {
    const values: Record<string, string> = {
      fr: value.uncertaintyFr, en: value.uncertaintyEn, de: value.uncertaintyDe, nl: value.uncertaintyNl,
      it: value.uncertaintyIt, es: value.uncertaintyEs, pl: value.uncertaintyPl, pt: value.uncertaintyPt
    };
    return this.languages
      .map((languageCode: string): AdminHistoricalLocalizedText => ({ languageCode, value: values[languageCode]?.trim() ?? '' }))
      .filter((item: AdminHistoricalLocalizedText): boolean => item.value.length > 0);
  }

  private buildPeriod(
    value: ReturnType<HistoricalRelationFormGroup['getRawValue']>,
    newRelationDate: HistoricalDateRequest
  ): HistoricalPeriodRequest {
    const period: AdminHistoricalPeriod | undefined = this.relation?.period;
    if (!period) {
      return {
        start: newRelationDate,
        end: newRelationDate,
        startConfidence: value.confidence,
        endConfidence: value.confidence
      };
    }

    const startChanged: boolean = this.form.controls.year.dirty
      || this.form.controls.approximate.dirty
      || this.form.controls.confidence.dirty
      || value.year !== period.start?.year
      || value.approximate !== period.start?.isApproximate
      || value.confidence !== period.startConfidence;
    if (!startChanged || !period.start) {
      return this.toPeriodRequest(period);
    }

    const updatedStart: HistoricalDateRequest = {
      ...period.start,
      year: value.year,
      isApproximate: value.approximate,
      qualifier: period.start.qualifier === 'Circa' && !value.approximate
        ? null
        : period.start.qualifier
    };
    const wasPoint: boolean = !!period.end && this.sameDate(period.start, period.end);
    const hadSharedConfidence: boolean = period.startConfidence === period.endConfidence;
    return {
      start: updatedStart,
      end: wasPoint ? { ...updatedStart } : period.end ? { ...period.end } : null,
      startConfidence: value.confidence,
      endConfidence: wasPoint && hadSharedConfidence ? value.confidence : period.endConfidence
    };
  }

  private sameDate(left: HistoricalDateRequest, right: HistoricalDateRequest): boolean {
    return left.year === right.year
      && left.month === right.month
      && left.day === right.day
      && left.precision === right.precision
      && left.isApproximate === right.isApproximate
      && left.qualifier === right.qualifier;
  }

  private toPeriodRequest(period: AdminHistoricalPeriod): HistoricalPeriodRequest {
    return {
      start: period.start ? { ...period.start } : null,
      end: period.end ? { ...period.end } : null,
      startConfidence: period.startConfidence,
      endConfidence: period.endConfidence
    };
  }

  private resolveSubject(key: string): AdminHistoricalSubject | undefined {
    return this.subjects.find((subject: AdminHistoricalSubject): boolean => this.subjectKey(subject) === key);
  }

  private optional(value: string): string | null {
    const normalizedValue: string = value.trim();
    return normalizedValue || null;
  }
}

type HistoricalRelationFormGroup = FormGroup<HistoricalRelationForm>;
