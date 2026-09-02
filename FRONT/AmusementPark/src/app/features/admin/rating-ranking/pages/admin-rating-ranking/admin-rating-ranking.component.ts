import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, effect } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';

import {
  RatingRankingPolicyCandidateRequest,
  RatingRankingScopeDiagnostics
} from '@app/models/admin/ratings/rating-ranking-administration.models';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { UiTemplate } from '@shared/ui/primitives/api';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { Card } from '@shared/ui/primitives/card';
import { Checkbox } from '@shared/ui/primitives/checkbox';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Tag } from '@shared/ui/primitives/tag';
import { AdminRatingRankingStateFacade } from '../../state/admin-rating-ranking-state.facade';

interface RatingPolicyForm {
  version: FormControl<string>;
  provisionalMinUniqueContributors: FormControl<number>;
  eligibleMinUniqueContributors: FormControl<number>;
  establishedMinUniqueContributors: FormControl<number>;
  strongEvidenceMinUniqueContributors: FormControl<number>;
  minimumEligibleEntriesPerRanking: FormControl<number>;
  minimumEligibleItemsForParkItemComponent: FormControl<number>;
  minimumEligibleItemsPerCategory: FormControl<number>;
  minimumEligibleCategories: FormControl<number>;
  scoreTieEpsilon: FormControl<number>;
}

@Component({
  selector: 'app-admin-rating-ranking',
  templateUrl: './admin-rating-ranking.component.html',
  styleUrls: ['./admin-rating-ranking.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminRatingRankingStateFacade],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    TranslateModule,
    PageStateComponent,
    UiTemplate,
    ButtonDirective,
    Card,
    Checkbox,
    InputText,
    Tag
  ]
})
export class AdminRatingRankingComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly dashboard = this.facade.dashboard;
  protected readonly previewing = this.facade.previewing;
  protected readonly rebuilding = this.facade.rebuilding;
  protected readonly impact = this.facade.impact;
  protected readonly rebuildResult = this.facade.rebuildResult;
  protected readonly actionMessageKey = this.facade.actionMessageKey;
  protected readonly rebuildConfirmed = new FormControl<boolean>(false, { nonNullable: true });
  protected readonly policyForm = new FormGroup<RatingPolicyForm>({
    version: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    provisionalMinUniqueContributors: this.positiveNumberControl(),
    eligibleMinUniqueContributors: this.positiveNumberControl(),
    establishedMinUniqueContributors: this.positiveNumberControl(),
    strongEvidenceMinUniqueContributors: this.positiveNumberControl(),
    minimumEligibleEntriesPerRanking: this.positiveNumberControl(),
    minimumEligibleItemsForParkItemComponent: this.positiveNumberControl(),
    minimumEligibleItemsPerCategory: this.positiveNumberControl(),
    minimumEligibleCategories: this.positiveNumberControl(),
    scoreTieEpsilon: new FormControl<number>(0.0001, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.000001), Validators.max(0.1)]
    })
  });
  private policyInitialized: boolean = false;

  constructor(private readonly facade: AdminRatingRankingStateFacade) {
    effect((): void => {
      const methodology = this.dashboard()?.currentMethodology;
      if (!methodology || this.policyInitialized) {
        return;
      }

      this.policyInitialized = true;
      this.policyForm.setValue({
        version: this.nextMethodologyVersion(methodology.version),
        provisionalMinUniqueContributors: methodology.evidenceThresholds.provisional,
        eligibleMinUniqueContributors: methodology.evidenceThresholds.eligible,
        establishedMinUniqueContributors: methodology.evidenceThresholds.established,
        strongEvidenceMinUniqueContributors: methodology.evidenceThresholds.strong,
        minimumEligibleEntriesPerRanking: methodology.publicationRules.minimumEligibleEntries,
        minimumEligibleItemsForParkItemComponent: methodology.parkComposition.minimumEligibleItems,
        minimumEligibleItemsPerCategory: methodology.parkComposition.minimumItemsPerCategory,
        minimumEligibleCategories: methodology.parkComposition.minimumCategories,
        scoreTieEpsilon: methodology.publicationRules.scoreTieEpsilon
      }, { emitEvent: false });
    });
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected refresh(): void {
    this.facade.load();
  }

  protected preview(): void {
    if (this.policyForm.invalid) {
      this.policyForm.markAllAsTouched();
      return;
    }

    const request: RatingRankingPolicyCandidateRequest = this.policyForm.getRawValue();
    this.facade.preview(request);
  }

  protected rebuild(): void {
    if (!this.rebuildConfirmed.value) {
      return;
    }

    this.rebuildConfirmed.setValue(false);
    this.facade.rebuild();
  }

  protected scopeSeverity(
    scope: RatingRankingScopeDiagnostics
  ): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (scope.lastErrorCode) {
      return 'danger';
    }

    if (scope.isDiagnosticSourceTruncated) {
      return 'warn';
    }

    if (scope.isRebuildOutstanding) {
      return 'warn';
    }

    return scope.currentSnapshotId ? 'success' : 'secondary';
  }

  protected scopeStatusKey(scope: RatingRankingScopeDiagnostics): string {
    if (scope.lastErrorCode) {
      return 'admin.ratingRanking.scopeStatus.error';
    }

    if (scope.isDiagnosticSourceTruncated) {
      return 'admin.ratingRanking.scopeStatus.truncated';
    }

    if (scope.isRebuildOutstanding) {
      return 'admin.ratingRanking.scopeStatus.outstanding';
    }

    return scope.currentSnapshotId
      ? 'admin.ratingRanking.scopeStatus.current'
      : 'admin.ratingRanking.scopeStatus.missing';
  }

  protected enumLabelKey(group: 'levels' | 'reasons' | 'categories', value: string): string {
    const normalizedValue: string = value.length === 0
      ? value
      : `${value[0].toLowerCase()}${value.slice(1)}`;
    return `admin.ratingRanking.${group}.${normalizedValue}`;
  }

  protected dataAnomalyCount(): number {
    const anomalies = this.dashboard()?.dataDiagnostics.anomalies;
    return anomalies
      ? Object.values(anomalies).reduce(
        (total: number, count: number): number => total + count,
        0)
      : 0;
  }

  protected aggregateIssueCount(): number {
    const integrity = this.dashboard()?.dataDiagnostics.aggregateIntegrity;
    return integrity
      ? integrity.missingAggregateCount
        + integrity.divergentAggregateCount
        + integrity.orphanAggregateCount
      : 0;
  }

  private positiveNumberControl(): FormControl<number> {
    return new FormControl<number>(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1)]
    });
  }

  private nextMethodologyVersion(currentVersion: string): string {
    const match: RegExpMatchArray | null = currentVersion.match(/^(.*?)(\d+)$/);
    if (!match) {
      return `${currentVersion}-candidate`;
    }

    const prefix: string = match[1];
    const numericPart: string = match[2];
    const nextNumericPart: string = `${Number(numericPart) + 1}`.padStart(numericPart.length, '0');
    return `${prefix}${nextNumericPart}`;
  }
}
