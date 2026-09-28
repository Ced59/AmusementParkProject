import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  AdminHistoricalDiagnosticIssue,
  AdminHistoricalWorkflowStage,
  HistoricalDiagnosticSeverity
} from '@app/models/history/admin-historical-park-diagnostics.models';
import { Park } from '@app/models/parks/park';
import { AdminHistoryDiagnosticsStateFacade } from '../../state/admin-history-diagnostics-state.facade';

@Component({
  selector: 'app-admin-history-diagnostics',
  templateUrl: './admin-history-diagnostics.component.html',
  styleUrl: './admin-history-diagnostics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminHistoryDiagnosticsStateFacade],
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule]
})
export class AdminHistoryDiagnosticsComponent implements OnInit {
  protected readonly facade = inject(AdminHistoryDiagnosticsStateFacade);
  private readonly route = inject(ActivatedRoute);

  protected readonly searchControl = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(2)]
  });

  public ngOnInit(): void {
    const parkId: string = this.route.snapshot.queryParamMap.get('parkId')?.trim() ?? '';
    if (parkId.length > 0) {
      this.facade.load(parkId);
    }
  }

  protected search(): void {
    if (this.searchControl.invalid) {
      this.searchControl.markAsTouched();
      return;
    }

    this.facade.search(this.searchControl.value);
  }

  protected selectPark(park: Park): void {
    if (park.id) {
      this.facade.load(park.id);
    }
  }

  protected parkLabel(park: Park): string {
    return park.name?.trim() || '—';
  }

  protected issueKey(issue: AdminHistoricalDiagnosticIssue): string {
    return `admin.history.diagnostics.issues.${issue.code}`;
  }

  protected subjectLabel(issue: AdminHistoricalDiagnosticIssue): string {
    return issue.subjectLabel?.trim() || '';
  }

  protected severityKey(severity: HistoricalDiagnosticSeverity): string {
    return `admin.history.diagnostics.severity.${severity}`;
  }

  protected workflowKey(stage: AdminHistoricalWorkflowStage): string {
    return `admin.history.diagnostics.workflow.${stage.stage}`;
  }

  protected trackPark(_: number, park: Park): string {
    return park.id ?? park.name ?? String(_);
  }

  protected trackIssue(_: number, issue: AdminHistoricalDiagnosticIssue): string {
    return `${issue.code}:${issue.factId ?? issue.relationId ?? issue.subjectId ?? _}`;
  }
}
