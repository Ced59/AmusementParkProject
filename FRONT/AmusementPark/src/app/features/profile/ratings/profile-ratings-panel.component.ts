import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';

import { UserRatingListItem, UserRatingStatBucket, UserRatingStats } from '@app/models/ratings/rating.models';
import { TranslationService } from '@app/services/translation.service';
import { RatingTreeComponent, RatingTreePark, RatingTreeSection } from '@shared/components/rating-tree/rating-tree.component';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { ProfileRatingsStateFacade } from './profile-ratings-state.facade';

interface ProfileRatingSectionGroup {
  key: string;
  titleKey: string;
  order: number;
  ratings: UserRatingListItem[];
}

@Component({
  selector: 'app-profile-ratings-panel',
  templateUrl: './profile-ratings-panel.component.html',
  styleUrls: ['./profile-ratings-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileRatingsStateFacade],
  imports: [
    RatingTreeComponent,
    TranslateModule,
    UiButtonDirective,
    UiSectionHeaderComponent
  ]
})
export class ProfileRatingsPanelComponent implements OnInit {
  protected readonly searchTerm = signal<string>('');
  protected readonly currentLang = signal<string>('en');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly loadingMore: Signal<boolean> = this.stateFacade.loadingMore;
  protected readonly hasMore: Signal<boolean> = this.stateFacade.hasMore;
  protected readonly ratings: Signal<UserRatingListItem[]> = this.stateFacade.ratings;
  protected readonly stats: Signal<UserRatingStats | null> = this.stateFacade.stats;
  protected readonly isEmpty: Signal<boolean> = this.stateFacade.isEmpty;
  protected readonly ratingParks: Signal<RatingTreePark[]> = computed(() => {
    const language: string = this.currentLang();
    return this.groupRatingsByPark(this.ratings(), language);
  });

  constructor(
    private readonly stateFacade: ProfileRatingsStateFacade,
    private readonly translationService: TranslationService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang.set(this.translationService.getCurrentLang() || 'en');
    this.stateFacade.load();

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string): void => {
      this.currentLang.set(lang);
    });
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

  private targetRoute(rating: UserRatingListItem, language: string): string[] | null {
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

  private parkRoute(parkId: string, parkName: string, language: string): string[] | null {
    return buildPublicParkRouteCommands({
      language,
      parkId,
      parkName
    });
  }

  private groupRatingsByPark(ratings: UserRatingListItem[], language: string): RatingTreePark[] {
    const groups = new Map<string, UserRatingListItem[]>();
    for (const rating of ratings) {
      const key: string = rating.parkId || rating.targetId;
      const existingRatings: UserRatingListItem[] | undefined = groups.get(key);
      if (existingRatings) {
        existingRatings.push(rating);
      } else {
        groups.set(key, [rating]);
      }
    }

    return Array.from(groups.entries()).map(([parkId, groupRatings]: [string, UserRatingListItem[]]) => {
      const ratingSum: number = groupRatings.reduce((sum: number, rating: UserRatingListItem) => sum + rating.value, 0);
      const parkName: string = groupRatings[0]?.parkName || groupRatings[0]?.targetName || parkId;
      return {
        id: parkId,
        rank: null,
        name: parkName,
        score: ratingSum / Math.max(groupRatings.length, 1),
        ratingCount: groupRatings.length,
        route: this.parkRoute(parkId, parkName, language),
        metrics: [],
        sections: this.groupRatingsBySection(groupRatings, language)
      };
    }).sort((left: RatingTreePark, right: RatingTreePark) => {
      if (right.score !== left.score) {
        return right.score - left.score;
      }

      if (right.ratingCount !== left.ratingCount) {
        return right.ratingCount - left.ratingCount;
      }

      return left.name.localeCompare(right.name);
    });
  }

  private groupRatingsBySection(ratings: UserRatingListItem[], language: string): RatingTreeSection[] {
    const groups = new Map<string, ProfileRatingSectionGroup>();

    for (const rating of ratings) {
      const section: ProfileRatingSectionGroup = this.resolveSectionGroup(rating);
      const existingSection: ProfileRatingSectionGroup | undefined = groups.get(section.key);
      if (existingSection) {
        existingSection.ratings.push(rating);
      } else {
        groups.set(section.key, {
          ...section,
          ratings: [rating]
        });
      }
    }

    return Array.from(groups.values()).sort((left: ProfileRatingSectionGroup, right: ProfileRatingSectionGroup) => {
      if (left.order !== right.order) {
        return left.order - right.order;
      }

      return left.titleKey.localeCompare(right.titleKey);
    }).map((section: ProfileRatingSectionGroup): RatingTreeSection => {
      const ratingSum: number = section.ratings.reduce((sum: number, rating: UserRatingListItem) => sum + rating.value, 0);
      return {
        id: section.key,
        titleKey: section.titleKey,
        score: ratingSum / Math.max(section.ratings.length, 1),
        items: [...section.ratings].sort((left: UserRatingListItem, right: UserRatingListItem) => {
          if (right.value !== left.value) {
            return right.value - left.value;
          }

          return left.targetName.localeCompare(right.targetName);
        }).map((rating: UserRatingListItem) => {
          return {
            id: rating.id,
            name: rating.targetName,
            score: rating.value,
            route: this.targetRoute(rating, language),
            secondaryLabelKey: 'ratings.profile.communityAverage',
            secondaryScore: rating.summary.averageRating
          };
        })
      };
    });
  }

  private resolveSectionGroup(rating: UserRatingListItem): ProfileRatingSectionGroup {
    if (rating.targetType === 'Park') {
      return {
        key: 'Park',
        titleKey: 'ratings.targetTypes.Park',
        order: 0,
        ratings: []
      };
    }

    if (rating.parkItemCategory) {
      return {
        key: rating.parkItemCategory,
        titleKey: `ratings.categories.${rating.parkItemCategory}`,
        order: this.categoryOrder(rating.parkItemCategory),
        ratings: []
      };
    }

    return {
      key: 'ParkItem',
      titleKey: 'ratings.targetTypes.ParkItem',
      order: 99,
      ratings: []
    };
  }

  private categoryOrder(category: string): number {
    switch (category) {
      case 'Attraction':
        return 10;
      case 'Restaurant':
        return 20;
      case 'Hotel':
        return 30;
      case 'Service':
        return 40;
      default:
        return 90;
    }
  }
}
