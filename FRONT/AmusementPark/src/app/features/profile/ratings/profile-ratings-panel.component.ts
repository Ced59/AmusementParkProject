import { ChangeDetectionStrategy, Component, OnInit, Signal, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UserRatingListItem, UserRatingStatBucket, UserRatingStats } from '@app/models/ratings/rating.models';
import { TranslationService } from '@app/services/translation.service';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent } from '@ui/primitives';
import { ProfileRatingsStateFacade } from './profile-ratings-state.facade';

interface ProfileRatingParkGroup {
  parkId: string;
  parkName: string;
  averageRating: number;
  ratingCount: number;
  ratings: UserRatingListItem[];
}

@Component({
  selector: 'app-profile-ratings-panel',
  templateUrl: './profile-ratings-panel.component.html',
  styleUrls: ['./profile-ratings-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileRatingsStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiSectionHeaderComponent
  ]
})
export class ProfileRatingsPanelComponent implements OnInit {
  protected readonly searchTerm = signal<string>('');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly loadingMore: Signal<boolean> = this.stateFacade.loadingMore;
  protected readonly hasMore: Signal<boolean> = this.stateFacade.hasMore;
  protected readonly ratings: Signal<UserRatingListItem[]> = this.stateFacade.ratings;
  protected readonly stats: Signal<UserRatingStats | null> = this.stateFacade.stats;
  protected readonly isEmpty: Signal<boolean> = this.stateFacade.isEmpty;
  protected readonly ratingGroups: Signal<ProfileRatingParkGroup[]> = computed(() => this.groupRatingsByPark(this.ratings()));

  constructor(
    private readonly stateFacade: ProfileRatingsStateFacade,
    private readonly translationService: TranslationService
  ) {
  }

  ngOnInit(): void {
    this.stateFacade.load();
  }

  protected updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  protected applySearch(): void {
    this.stateFacade.load(1, this.searchTerm());
  }

  protected clearSearch(): void {
    this.searchTerm.set('');
    this.stateFacade.load();
  }

  protected loadMore(): void {
    this.stateFacade.loadMore();
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

  protected parkRoute(group: ProfileRatingParkGroup): string[] | null {
    const language: string = this.translationService.getCurrentLang() || 'en';

    return buildPublicParkRouteCommands({
      language,
      parkId: group.parkId,
      parkName: group.parkName
    });
  }

  private groupRatingsByPark(ratings: UserRatingListItem[]): ProfileRatingParkGroup[] {
    const groups = new Map<string, UserRatingListItem[]>();
    for (const rating of ratings) {
      const key: string = rating.parkId || rating.targetId;
      groups.set(key, [...(groups.get(key) ?? []), rating]);
    }

    return Array.from(groups.entries()).map(([parkId, groupRatings]: [string, UserRatingListItem[]]) => {
      const ratingSum: number = groupRatings.reduce((sum: number, rating: UserRatingListItem) => sum + rating.value, 0);
      return {
        parkId,
        parkName: groupRatings[0]?.parkName || groupRatings[0]?.targetName || parkId,
        averageRating: ratingSum / Math.max(groupRatings.length, 1),
        ratingCount: groupRatings.length,
        ratings: [...groupRatings].sort((left: UserRatingListItem, right: UserRatingListItem) => {
          if (right.value !== left.value) {
            return right.value - left.value;
          }

          return left.targetName.localeCompare(right.targetName);
        })
      };
    }).sort((left: ProfileRatingParkGroup, right: ProfileRatingParkGroup) => {
      if (right.averageRating !== left.averageRating) {
        return right.averageRating - left.averageRating;
      }

      if (right.ratingCount !== left.ratingCount) {
        return right.ratingCount - left.ratingCount;
      }

      return left.parkName.localeCompare(right.parkName);
    });
  }
}
