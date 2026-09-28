import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminHistoricalLocalizedText,
  AdminHistoricalRelation,
  AdminHistoricalSource,
  AdminHistoricalSubject,
  HistoricalDateRequest,
  HistoricalFactState,
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
  protected readonly selectedSourceIds = signal<ReadonlySet<string>>(new Set<string>());
  protected readonly contradictingSourceId = signal<string | null>(null);
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
    const selected: Set<string> = new Set(this.selectedSourceIds());
    if (selected.has(sourceId)) {
      selected.delete(sourceId);
      if (this.contradictingSourceId() === sourceId) {
        this.contradictingSourceId.set(null);
      }
    } else {
      selected.add(sourceId);
    }

    this.selectedSourceIds.set(selected);
  }

  protected markContradicting(sourceId: string): void {
    if (this.selectedSourceIds().has(sourceId)) {
      this.contradictingSourceId.set(this.contradictingSourceId() === sourceId ? null : sourceId);
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
    const selected: ReadonlySet<string> = this.selectedSourceIds();
    this.submitted.emit({
      relationId: this.relation?.id ?? null,
      request: {
        expectedRevision: this.relation?.revision ?? null,
        sourceSubjectType: sourceSubject.type,
        sourceSubjectId: sourceSubject.id,
        targetSubjectType: targetSubject.type,
        targetSubjectId: targetSubject.id,
        type: value.type,
        direction: value.direction,
        period: {
          start: date,
          end: date,
          startConfidence: value.confidence,
          endConfidence: value.confidence
        },
        state: value.state,
        publicUncertaintyExplanation: explanations,
        sources: this.sources
          .filter((source: AdminHistoricalSource): boolean => selected.has(source.id))
          .map((source: AdminHistoricalSource) => ({
            sourceId: source.id,
            revision: source.revision,
            position: this.contradictingSourceId() === source.id ? 'Contradicts' as const : 'Supports' as const
          })),
        editorialNote: this.optional(value.editorialNote),
        reviewNote: this.optional(value.reviewNote)
      }
    });
  }

  protected subjectKey(subject: AdminHistoricalSubject): string {
    return `${subject.type}::${subject.id}`;
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
    this.selectedSourceIds.set(new Set((relation?.sources ?? []).map((source): string => source.sourceId)));
    this.contradictingSourceId.set(relation?.sources.find((source): boolean => source.position === 'Contradicts')?.sourceId ?? null);
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

  private resolveSubject(key: string): AdminHistoricalSubject | undefined {
    return this.subjects.find((subject: AdminHistoricalSubject): boolean => this.subjectKey(subject) === key);
  }

  private optional(value: string): string | null {
    const normalizedValue: string = value.trim();
    return normalizedValue || null;
  }
}

type HistoricalRelationFormGroup = FormGroup<HistoricalRelationForm>;
