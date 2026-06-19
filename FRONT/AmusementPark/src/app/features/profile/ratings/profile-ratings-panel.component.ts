import { ChangeDetectionStrategy, Component, OnInit, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UserRatingListItem, UserRatingStatBucket, UserRatingStats } from '@app/models/ratings/rating.models';
import { TranslationService } from '@app/services/translation.service';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiChipComponent, UiSectionHeaderComponent } from '@ui/primitives';
import { ProfileRatingsStateFacade } from './profile-ratings-state.facade';

@Component({
  selector: 'app-profile-ratings-panel',
  templateUrl: './profile-ratings-panel.component.html',
  styleUrls: ['./profile-ratings-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileRatingsStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    UiChipComponent,
    UiSectionHeaderComponent
  ]
})
export class ProfileRatingsPanelComponent implements OnInit {
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly ratings: Signal<UserRatingListItem[]> = this.stateFacade.ratings;
  protected readonly stats: Signal<UserRatingStats | null> = this.stateFacade.stats;
  protected readonly isEmpty: Signal<boolean> = this.stateFacade.isEmpty;

  constructor(
    private readonly stateFacade: ProfileRatingsStateFacade,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.load();
  }

  protected formatRating(value: number | null | undefined): string {
    const rating: number = Number(value ?? 0);
    return rating > 0 ? rating.toFixed(1).replace('.', ',') : '-';
  }

  protected ratingPercent(value: number | null | undefined): string {
    const rating: number = Math.max(0, Math.min(5, Number(value ?? 0)));
    return `${(rating / 5) * 100}%`;
  }

  protected bucketLabel(bucket: UserRatingStatBucket, kind: 'targetType' | 'category' | 'park'): string {
    if (kind === 'park') {
      return bucket.label;
    }

    const keyPrefix: string = kind === 'targetType' ? 'ratings.targetTypes' : 'ratings.categories';
    return `${keyPrefix}.${bucket.key}`;
  }

  protected targetRoute(rating: UserRatingListItem): string[] | null {
    const language: string = this.translationService.getCurrentLang() || 'en';

    if (rating.targetType === 'Park') {
      return buildPublicParkRouteCommands({
        language,
        parkId: rating.parkId,
        parkName: rating.targetName
      });
    }

    return buildPublicParkItemRouteCommands({
      language,
      parkId: rating.parkId,
      parkName: rating.parkName,
      itemId: rating.targetId,
      itemName: rating.targetName
    });
  }
}
