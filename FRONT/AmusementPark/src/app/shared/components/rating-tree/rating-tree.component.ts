import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { LocalizedPluralPipe } from '@shared/pipes';

export interface RatingTreeEditableScore {
  ratingId: string;
  saving: boolean;
}

export interface RatingTreeMetric {
  labelKey: string;
  value: number;
  editable?: RatingTreeEditableScore | null;
}

export interface RatingTreeItem {
  id: string;
  name: string;
  score: number;
  route: string[] | null;
  secondaryLabelKey?: string | null;
  secondaryScore?: number | null;
  editable?: RatingTreeEditableScore | null;
}

export interface RatingTreeSection {
  id: string;
  titleKey: string;
  score: number;
  items: RatingTreeItem[];
}

export interface RatingTreePark {
  id: string;
  rank: number | null;
  name: string;
  score: number;
  ratingCount: number;
  route: string[] | null;
  metrics: RatingTreeMetric[];
  sections: RatingTreeSection[];
}

export interface RatingTreeRatingChange {
  ratingId: string;
  value: number;
}

@Component({
  selector: 'app-rating-tree',
  templateUrl: './rating-tree.component.html',
  styleUrls: ['./rating-tree.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule, LocalizedPluralPipe]
})
export class RatingTreeComponent {
  protected readonly starIndexes: readonly number[] = [1, 2, 3, 4, 5];

  @Input({ required: true }) parks: RatingTreePark[] = [];
  @Input() ratingCountLabelKey: string = 'ratings.rankings.ratingCount';
  @Input() detailActionKey: string = 'ratings.tree.detailAction';
  @Input() collapseActionKey: string = 'ratings.tree.collapseAction';
  @Input() openParkActionKey: string = 'ratings.tree.openParkAction';
  @Input() rateActionKey: string = 'ratings.stars.rateValue';

  @Output() ratingChange = new EventEmitter<RatingTreeRatingChange>();

  protected formatRating(value: number | null | undefined): string {
    const rating: number = Number(value ?? 0);
    return rating > 0 ? rating.toFixed(1).replace('.', ',') : '-';
  }

  protected fillPercent(value: number | null | undefined, starIndex: number): string {
    const rating: number = Number(value ?? 0);
    const filled: number = Math.max(0, Math.min(1, rating - (starIndex - 1)));
    return `${filled * 100}%`;
  }

  protected changeRating(event: Event, editableScore: RatingTreeEditableScore, value: number): void {
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
