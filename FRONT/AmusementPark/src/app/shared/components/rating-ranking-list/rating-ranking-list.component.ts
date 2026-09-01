import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LocalizedPluralPipe } from '@shared/pipes';

export interface RatingRankingListEditableScore {
  ratingId: string;
  saving: boolean;
}

export interface RatingRankingListItem {
  id: string;
  rank: number | null;
  name: string;
  score: number;
  ratingCount?: number | null;
  route: string[] | null;
  parkName: string;
  parkRoute: string[] | null;
  editable?: RatingRankingListEditableScore | null;
}

export interface RatingRankingListRatingChange {
  ratingId: string;
  value: number;
}

@Component({
  selector: 'app-rating-ranking-list',
  templateUrl: './rating-ranking-list.component.html',
  styleUrls: ['./rating-ranking-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, LocalizedPluralPipe]
})
export class RatingRankingListComponent {
  protected readonly starIndexes: readonly number[] = [1, 2, 3, 4, 5];

  @Input({ required: true }) items: RatingRankingListItem[] = [];
  @Input() ratingCountLabelKey: string = 'ratings.rankings.ratingCount';
  @Input() scoreLabelKey: string = 'ratings.stars.averageLabel';
  @Input() rateActionKey: string = 'ratings.stars.rateValue';

  @Output() ratingChange = new EventEmitter<RatingRankingListRatingChange>();

  protected formatRating(value: number): string {
    return value > 0 ? value.toFixed(1).replace('.', ',') : '-';
  }

  protected fillPercent(value: number, starIndex: number): string {
    const filled: number = Math.max(0, Math.min(1, value - (starIndex - 1)));
    return `${filled * 100}%`;
  }

  protected changeRating(
    event: Event,
    editableScore: RatingRankingListEditableScore,
    value: number
  ): void {
    event.preventDefault();
    event.stopPropagation();

    if (editableScore.saving) {
      return;
    }

    this.ratingChange.emit({
      ratingId: editableScore.ratingId,
      value
    });
  }
}
