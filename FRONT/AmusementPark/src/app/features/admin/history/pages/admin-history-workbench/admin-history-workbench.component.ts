import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminHistoricalFact,
  AdminHistoricalRelation,
  AdminHistoricalSource,
  AdminHistoricalSubject,
  HistoricalEditorialResourceType,
  HistoricalWorkflowState,
  SaveHistoricalFactRequest,
  SaveHistoricalRelationRequest,
  SaveHistoricalSourceRequest
} from '@app/models/history/admin-historical-workbench.models';
import { AdminHistoricalFactEditorComponent } from '../../components/admin-historical-fact-editor/admin-historical-fact-editor.component';
import { AdminHistoricalRelationEditorComponent } from '../../components/admin-historical-relation-editor/admin-historical-relation-editor.component';
import { AdminHistoricalSourceEditorComponent } from '../../components/admin-historical-source-editor/admin-historical-source-editor.component';
import { HISTORICAL_WORKFLOW_STAGES } from '../../models/admin-historical-workbench-options';
import { filterHistoricalFactSubjectsForPark } from '../../models/admin-historical-workbench-subjects';
import { AdminHistoryWorkbenchStateFacade } from '../../state/admin-history-workbench-state.facade';

type WorkbenchEditor = 'fact' | 'relation' | 'source' | null;

@Component({
  selector: 'app-admin-history-workbench',
  templateUrl: './admin-history-workbench.component.html',
  styleUrl: './admin-history-workbench.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminHistoryWorkbenchStateFacade],
  imports: [
    CommonModule,
    RouterLink,
    TranslateModule,
    AdminHistoricalFactEditorComponent,
    AdminHistoricalRelationEditorComponent,
    AdminHistoricalSourceEditorComponent
  ]
})
export class AdminHistoryWorkbenchComponent implements OnInit {
  protected readonly facade = inject(AdminHistoryWorkbenchStateFacade);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly stages = HISTORICAL_WORKFLOW_STAGES;
  protected readonly activeEditor = signal<WorkbenchEditor>(null);
  protected readonly editingFact = signal<AdminHistoricalFact | null>(null);
  protected readonly editingRelation = signal<AdminHistoricalRelation | null>(null);
  protected readonly editingSource = signal<AdminHistoricalSource | null>(null);
  protected readonly factSubjects = computed<readonly AdminHistoricalSubject[]>(() => {
    const workbench = this.facade.workbench();
    return workbench
      ? filterHistoricalFactSubjectsForPark(workbench.subjects, workbench.parkId)
      : [];
  });

  public ngOnInit(): void {
    const parkId: string = this.route.snapshot.paramMap.get('parkId')?.trim() ?? '';
    this.facade.load(parkId);
  }

  protected openFactEditor(fact: AdminHistoricalFact | null = null): void {
    this.editingFact.set(fact);
    this.activeEditor.set('fact');
  }

  protected openRelationEditor(relation: AdminHistoricalRelation | null = null): void {
    this.editingRelation.set(relation);
    this.activeEditor.set('relation');
  }

  protected openSourceEditor(source: AdminHistoricalSource | null = null): void {
    this.editingSource.set(source);
    this.activeEditor.set('source');
  }

  protected closeEditor(): void {
    this.activeEditor.set(null);
    this.editingFact.set(null);
    this.editingRelation.set(null);
    this.editingSource.set(null);
  }

  protected saveFact(event: { factId: string | null; request: SaveHistoricalFactRequest }): void {
    this.facade.saveFact(event.factId, event.request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (): void => this.closeEditor(), error: (): void => undefined });
  }

  protected saveRelation(event: { relationId: string | null; request: SaveHistoricalRelationRequest }): void {
    this.facade.saveRelation(event.relationId, event.request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (): void => this.closeEditor(), error: (): void => undefined });
  }

  protected saveSource(event: { sourceId: string | null; request: SaveHistoricalSourceRequest }): void {
    this.facade.saveSource(event.sourceId, event.request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (): void => this.closeEditor(), error: (): void => undefined });
  }

  protected advance(resourceType: HistoricalEditorialResourceType, resourceId: string, revision: number): void {
    this.facade.advance(resourceType, resourceId, revision)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: (): void => undefined });
  }

  protected retract(resourceType: HistoricalEditorialResourceType, resourceId: string, revision: number): void {
    this.facade.retract(resourceType, resourceId, revision)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ error: (): void => undefined });
  }

  protected preview(resourceType: HistoricalEditorialResourceType, resourceId: string, year: number | null): void {
    this.facade.previewImpact(resourceType, resourceId, year);
  }

  protected canAdvance(stage: HistoricalWorkflowState): boolean {
    return ['Draft', 'SourcesAttached', 'EditorialReview', 'StructuredValidation'].includes(stage);
  }

  protected canRetract(stage: HistoricalWorkflowState): boolean {
    return stage === 'Published' || stage === 'Corrected';
  }

  protected periodLabel(fact: AdminHistoricalFact | AdminHistoricalRelation): string {
    const start: string = this.dateLabel(fact.period.start);
    const end: string = this.dateLabel(fact.period.end);
    return start === end ? start : `${start} → ${end}`;
  }

  protected nextStageKey(stage: HistoricalWorkflowState, resourceType: HistoricalEditorialResourceType): string {
    if (stage === 'Draft' && resourceType === 'Source') {
      return 'admin.history.workbench.workflow.EditorialReview';
    }

    const index: number = this.stages.indexOf(stage);
    return `admin.history.workbench.workflow.${this.stages[index + 1] ?? stage}`;
  }

  protected sourceLabel(sourceId: string): string {
    return this.facade.workbench()?.sources.find((source: AdminHistoricalSource): boolean => source.id === sourceId)?.title
      ?? '—';
  }

  private dateLabel(date: AdminHistoricalFact['period']['start']): string {
    if (!date) {
      return '—';
    }

    const parts: string[] = [String(date.year)];
    if (date.month) {
      parts.push(String(date.month).padStart(2, '0'));
    }
    if (date.day) {
      parts.push(String(date.day).padStart(2, '0'));
    }
    return `${date.isApproximate ? '≈ ' : ''}${parts.reverse().join('/')}`;
  }
}
