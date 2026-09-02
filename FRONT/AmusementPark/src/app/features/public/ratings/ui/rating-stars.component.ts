import { ChangeDetectionStrategy, Component, Input, OnChanges, Signal, SimpleChanges, computed, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';

import { RatingSummary, RatingTargetType } from '@app/models/ratings/rating.models';
import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { PublicRatingStateFacade } from '../state/public-rating-state.facade';
import { LocalizedPluralPipe } from '@shared/pipes';
import {
  RatingEvidenceComponent,
  RatingEvidenceViewModel
} from '@shared/components/rating-evidence/rating-evidence.component';

@Component({
  selector: 'app-rating-stars',
  templateUrl: './rating-stars.component.html',
  styleUrls: ['./rating-stars.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicRatingStateFacade],
  imports: [TranslateModule, LocalizedPluralPipe, RouterLink, RatingEvidenceComponent]
})
export class RatingStarsComponent implements OnChanges {
  protected readonly starIndexes: readonly number[] = [1, 2, 3, 4, 5];
  protected readonly hoverValue = signal<number | null>(null);
  protected readonly summary: Signal<RatingSummary | null> = this.stateFacade.summary;
  protected readonly saving: Signal<boolean> = this.stateFacade.saving;
  protected readonly messageKey: Signal<string | null> = this.stateFacade.messageKey;
  protected readonly selectedValue: Signal<number | null> = this.stateFacade.userRatingValue;
  protected readonly methodology: Signal<RatingMethodology | null> = this.stateFacade.methodology;
  protected readonly displayValue: Signal<number> = computed(() => {
    return this.hoverValue() ?? this.selectedValue() ?? 0;
  });
  protected readonly visibleRank: Signal<number | null> = computed(() => {
    const summary: RatingSummary | null = this.summary();
    if (!summary) {
      return null;
    }

    return summary.evidence && !summary.evidence.isEligibleForMainRanking
      ? null
      : summary.rank ?? null;
  });
  protected readonly evidenceModel: Signal<RatingEvidenceViewModel | null> = computed(() => {
    const summary: RatingSummary | null = this.summary();
    if (!summary?.evidence) {
      return null;
    }

    const resolvedMethodologyVersion: string | null = summary.methodologyVersion ?? null;
    const methodology: RatingMethodology | null = this.methodology();
    const matchingMethodology: RatingMethodology | null = resolvedMethodologyVersion
      && methodology?.version === resolvedMethodologyVersion
      ? methodology
      : null;

    return {
      evidence: summary.evidence,
      uniqueContributorCount: summary.uniqueContributorCount ?? null,
      ratingObservationCount: summary.ratingObservationCount ?? summary.ratingCount,
      targetType: summary.targetType,
      rank: this.visibleRank(),
      methodologyVersion: resolvedMethodologyVersion,
      eligibilityThreshold: matchingMethodology?.evidenceThresholds.eligible ?? null
    };
  });

  @Input({ required: true }) targetType!: RatingTargetType;
  @Input({ required: true }) targetId!: string;
  @Input() initialSummary: RatingSummary | null = null;
  @Input() contextHintKey: string | null = null;

  constructor(
    private readonly stateFacade: PublicRatingStateFacade,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnChanges(_changes: SimpleChanges): void {
    if (!this.targetType || !this.targetId) {
      return;
    }

    this.stateFacade.configure(this.targetType, this.targetId, this.initialSummary);
  }

  protected preview(value: number): void {
    this.hoverValue.set(value);
  }

  protected clearPreview(): void {
    this.hoverValue.set(null);
  }

  protected rate(value: number): void {
    this.stateFacade.rate(value);
  }

  protected removeRating(): void {
    const confirmed: boolean = confirm(
      this.translateService.instant('ratings.stars.clearRatingConfirm'),
    );
    if (!confirmed) {
      return;
    }

    this.stateFacade.removeRating();
  }

  protected fillPercent(starIndex: number): string {
    const value: number = this.displayValue();
    const filled: number = Math.max(0, Math.min(1, value - (starIndex - 1)));
    return `${filled * 100}%`;
  }

  protected formattedAverage(): string {
    const average: number = this.summary()?.averageRating ?? 0;
    return this.formatRating(average);
  }

  protected formattedSelectedValue(): string {
    const value: number = this.selectedValue() ?? 0;
    return this.formatRating(value);
  }

  protected count(): number {
    return this.summary()?.ratingCount ?? 0;
  }

  protected methodologyRoute(): string[] {
    const language: string = this.translateService.currentLang
      || this.translateService.defaultLang
      || 'en';
    const version: string | null = this.summary()?.methodologyVersion ?? this.methodology()?.version ?? null;
    return version
      ? ['/', language, 'rankings', 'methodology', version]
      : ['/', language, 'rankings', 'methodology'];
  }

  private formatRating(value: number): string {
    if (value <= 0) {
      return '-';
    }

    const locale: string = this.translateService.currentLang
      || this.translateService.defaultLang
      || 'en';
    return new Intl.NumberFormat(locale, {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(value);
  }
}
