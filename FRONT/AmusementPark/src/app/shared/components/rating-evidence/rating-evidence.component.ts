import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import {
  RankingEvidence,
  RankingEvidenceLevel,
  RankingIneligibilityReason,
  RatingTargetType
} from '@app/models/ratings/rating.models';
import { LocalizedPluralPipe } from '@shared/pipes';

export interface RatingEvidenceViewModel {
  evidence: RankingEvidence;
  uniqueContributorCount: number | null;
  ratingObservationCount: number | null;
  targetType: RatingTargetType;
  rank: number | null;
  methodologyVersion: string | null;
  eligibilityThreshold: number | null;
}

@Component({
  selector: 'app-rating-evidence',
  templateUrl: './rating-evidence.component.html',
  styleUrls: ['./rating-evidence.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslateModule, LocalizedPluralPipe]
})
export class RatingEvidenceComponent {
  @Input({ required: true }) model!: RatingEvidenceViewModel;
  @Input() mode: 'badge' | 'details' = 'details';

  protected levelClass(): string {
    return this.model.evidence.level.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase();
  }

  protected levelLabelKey(): string {
    switch (this.model.evidence.level) {
      case 'NoEvidence':
        return 'ratings.evidence.levels.noEvidence';
      case 'Insufficient':
        return 'ratings.evidence.levels.insufficient';
      case 'Provisional':
        return 'ratings.evidence.levels.provisional';
      case 'Eligible':
        return 'ratings.evidence.levels.eligible';
      case 'Established':
        return 'ratings.evidence.levels.established';
      case 'StrongEvidence':
        return 'ratings.evidence.levels.strongEvidence';
      case 'Excluded':
        return 'ratings.evidence.levels.excluded';
    }
  }

  protected levelIconClass(): string {
    switch (this.model.evidence.level) {
      case 'NoEvidence':
        return 'pi pi-circle';
      case 'Insufficient':
        return 'pi pi-hourglass';
      case 'Provisional':
        return 'pi pi-chart-line';
      case 'Eligible':
        return 'pi pi-check';
      case 'Established':
        return 'pi pi-check-circle';
      case 'StrongEvidence':
        return 'pi pi-shield';
      case 'Excluded':
        return 'pi pi-ban';
    }
  }

  protected contributorCount(): number {
    return Math.max(0, this.model.uniqueContributorCount ?? 0);
  }

  protected messagePluralCount(): number {
    return this.isParkDirectEvidenceProvisional()
      ? Math.max(0, this.model.evidence.directParkContributorCount ?? 0)
      : this.contributorCount();
  }

  protected messageKey(): string {
    const level: RankingEvidenceLevel = this.model.evidence.level;
    if (level === 'NoEvidence') {
      return 'ratings.evidence.messages.noEvidence';
    }
    if (level === 'Excluded') {
      return 'ratings.evidence.messages.excluded';
    }
    if (this.isParkDirectEvidenceProvisional()) {
      return 'ratings.evidence.messages.parkDirectProvisional';
    }
    if (!this.model.evidence.isEligibleForMainRanking) {
      if (this.model.eligibilityThreshold === null) {
        return level === 'Provisional'
          ? 'ratings.evidence.messages.provisionalWithoutThreshold'
          : 'ratings.evidence.messages.insufficientWithoutThreshold';
      }

      return level === 'Provisional'
        ? 'ratings.evidence.messages.provisional'
        : 'ratings.evidence.messages.insufficient';
    }
    if (this.model.rank !== null && this.model.methodologyVersion) {
      return 'ratings.evidence.messages.ranked';
    }

    return 'ratings.evidence.messages.eligibleWithoutRank';
  }

  protected messageParams(): Record<string, unknown> {
    return {
      count: this.messagePluralCount(),
      threshold: this.model.eligibilityThreshold ?? '—',
      rank: this.model.rank ?? '—',
      version: this.model.methodologyVersion ?? '—'
    };
  }

  private isParkDirectEvidenceProvisional(): boolean {
    return this.model.targetType === 'Park'
      && this.model.evidence.level === 'Provisional'
      && this.model.evidence.ineligibilityReason === 'TooFewUniqueContributors'
      && this.model.evidence.directParkContributorCount !== null
      && this.model.evidence.directParkContributorCount !== undefined
      && this.contributorCount() > this.model.evidence.directParkContributorCount;
  }

  protected reasonLabelKey(): string | null {
    const reason: RankingIneligibilityReason | null | undefined = this.model.evidence.ineligibilityReason;
    switch (reason) {
      case 'NoRatings':
        return 'ratings.evidence.reasons.noRatings';
      case 'TooFewUniqueContributors':
        return 'ratings.evidence.reasons.tooFewUniqueContributors';
      case 'TooFewComparableEntries':
        return 'ratings.evidence.reasons.tooFewComparableEntries';
      case 'InsufficientItemCoverage':
        return 'ratings.evidence.reasons.insufficientItemCoverage';
      case 'InsufficientCategoryCoverage':
        return 'ratings.evidence.reasons.insufficientCategoryCoverage';
      case 'TargetUnavailable':
        return 'ratings.evidence.reasons.targetUnavailable';
      case 'TargetExcluded':
        return 'ratings.evidence.reasons.targetExcluded';
      case 'AggregateIntegrityFailure':
        return 'ratings.evidence.reasons.aggregateIntegrityFailure';
      case 'UnsupportedComposition':
        return 'ratings.evidence.reasons.unsupportedComposition';
      default:
        return null;
    }
  }

  protected hasParkComposition(): boolean {
    const evidence: RankingEvidence = this.model.evidence;
    return this.model.targetType === 'Park' && [
      evidence.directParkContributorCount,
      evidence.itemContributorCount,
      evidence.eligibleItemCount,
      evidence.eligibleCategoryCount
    ].some((value: number | null | undefined): boolean => value !== null && value !== undefined);
  }

  protected hasNextThreshold(): boolean {
    const threshold: number | null | undefined = this.model.evidence.nextThreshold;
    return threshold !== null && threshold !== undefined && threshold > this.contributorCount();
  }
}
