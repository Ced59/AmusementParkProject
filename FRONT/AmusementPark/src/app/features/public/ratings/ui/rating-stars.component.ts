import { ChangeDetectionStrategy, Component, Input, OnChanges, Signal, SimpleChanges, computed, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { RatingSummary, RatingTargetType } from '@app/models/ratings/rating.models';
import { PublicRatingStateFacade } from '../state/public-rating-state.facade';
import { LocalizedPluralPipe } from '@shared/pipes';

@Component({
  selector: 'app-rating-stars',
  templateUrl: './rating-stars.component.html',
  styleUrls: ['./rating-stars.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicRatingStateFacade],
  imports: [TranslateModule, LocalizedPluralPipe]
})
export class RatingStarsComponent implements OnChanges {
  protected readonly starIndexes: readonly number[] = [1, 2, 3, 4, 5];
  protected readonly hoverValue = signal<number | null>(null);
  protected readonly summary: Signal<RatingSummary | null> = this.stateFacade.summary;
  protected readonly saving: Signal<boolean> = this.stateFacade.saving;
  protected readonly messageKey: Signal<string | null> = this.stateFacade.messageKey;
  protected readonly selectedValue: Signal<number | null> = this.stateFacade.userRatingValue;
  protected readonly displayValue: Signal<number> = computed(() => {
    return this.hoverValue() ?? this.summary()?.averageRating ?? 0;
  });

  @Input({ required: true }) targetType!: RatingTargetType;
  @Input({ required: true }) targetId!: string;
  @Input() initialSummary: RatingSummary | null = null;

  constructor(private readonly stateFacade: PublicRatingStateFacade) {
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

  protected fillPercent(starIndex: number): string {
    const value: number = this.displayValue();
    const filled: number = Math.max(0, Math.min(1, value - (starIndex - 1)));
    return `${filled * 100}%`;
  }

  protected formattedAverage(): string {
    const average: number = this.summary()?.averageRating ?? 0;
    return average > 0 ? average.toFixed(1).replace('.', ',') : '-';
  }

  protected count(): number {
    return this.summary()?.ratingCount ?? 0;
  }
}
