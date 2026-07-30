import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';

import {
  UserParkItemRatingRanking,
  UserParkRatingRanking,
  UserParkRatingRankingCategory,
  UserRatingListItem,
  UserRatingStatBucket,
  UserRatingStats
} from '@app/models/ratings/rating.models';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { TranslationService } from '@app/services/translation.service';
import {
  RatingRankingListComponent,
  RatingRankingListItem,
  RatingRankingListRatingChange
} from '@shared/components/rating-ranking-list/rating-ranking-list.component';
import {
  RatingTreeComponent,
  RatingTreeEditableScore,
  RatingTreeMetric,
  RatingTreePark,
  RatingTreeRatingChange,
  RatingTreeSection
} from '@shared/components/rating-tree/rating-tree.component';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { ATTRACTION_TYPE_OPTIONS, TranslationOption } from '@shared/utils/display/display-options';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { ProfileRatingsStateFacade } from './profile-ratings-state.facade';

interface ProfileRankingFilter {
  key: string;
  labelKey: string;
  category: string | null;
}

@Component({
  selector: 'app-profile-ratings-panel',
  templateUrl: './profile-ratings-panel.component.html',
  styleUrls: ['./profile-ratings-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileRatingsStateFacade],
  imports: [
    RatingTreeComponent,
    RatingRankingListComponent,
    TranslateModule,
    UiButtonDirective,
    UiSectionHeaderComponent
  ]
})
export class ProfileRatingsPanelComponent implements OnInit {
  protected readonly searchTerm = signal<string>('');
  protected readonly currentLang = signal<string>('en');
  protected readonly filters: readonly ProfileRankingFilter[] = [
    { key: 'all', labelKey: 'ratings.rankings.filters.all', category: null },
    { key: 'attractions', labelKey: 'ratings.rankings.filters.attractions', category: 'Attraction' },
    { key: 'restaurants', labelKey: 'ratings.rankings.filters.restaurants', category: 'Restaurant' },
    { key: 'hotels', labelKey: 'ratings.rankings.filters.hotels', category: 'Hotel' },
    { key: 'services', labelKey: 'ratings.rankings.filters.services', category: 'Service' }
  ];
  protected readonly currentFilter = signal<ProfileRankingFilter>(this.filters[0]);
  protected readonly selectedAttractionType = signal<ParkItemType | null>(null);
  protected readonly attractionTypeOptions: ReadonlyArray<TranslationOption<ParkItemType>> = ATTRACTION_TYPE_OPTIONS;
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly loadingMore: Signal<boolean> = this.stateFacade.loadingMore;
  protected readonly hasMore: Signal<boolean> = this.stateFacade.hasMore;
  protected readonly parkRankings: Signal<UserParkRatingRanking[]> = this.stateFacade.parkRankings;
  protected readonly parkItemRankings: Signal<UserParkItemRatingRanking[]> = this.stateFacade.parkItemRankings;
  protected readonly stats: Signal<UserRatingStats | null> = this.stateFacade.stats;
  protected readonly isEmpty: Signal<boolean> = this.stateFacade.isEmpty;
  protected readonly savingRatingIds: Signal<ReadonlySet<string>> = this.stateFacade.savingRatingIds;
  protected readonly isParkItemRanking: Signal<boolean> = computed(() => this.currentFilter().category !== null);
  protected readonly ratingParks: Signal<RatingTreePark[]> = computed(() => {
    const language: string = this.currentLang();
    const savingRatingIds: ReadonlySet<string> = this.savingRatingIds();
    return this.parkRankings().map((ranking: UserParkRatingRanking): RatingTreePark => {
      return this.mapParkRanking(ranking, language, savingRatingIds);
    });
  });
  protected readonly rankedParkItems: Signal<RatingRankingListItem[]> = computed(() => {
    const language: string = this.currentLang();
    const savingRatingIds: ReadonlySet<string> = this.savingRatingIds();
    return this.parkItemRankings().map((ranking: UserParkItemRatingRanking): RatingRankingListItem => {
      const rating: UserRatingListItem = ranking.rating;
      return {
        id: rating.id,
        rank: ranking.rank,
        name: rating.targetName,
        score: rating.value,
        route: this.targetRoute(rating, language),
        parkName: rating.parkName || rating.parkId,
        parkRoute: this.parkRoute(rating.parkId, rating.parkName || rating.parkId, language),
        editable: this.editableScore(rating.id, savingRatingIds)
      };
    });
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

  protected selectFilter(filter: ProfileRankingFilter): void {
    this.currentFilter.set(filter);
    this.selectedAttractionType.set(null);
    this.stateFacade.load(1, filter.category, this.searchTerm());
  }

  protected selectAttractionType(value: string): void {
    const selectedType: ParkItemType | null = value.trim().length > 0
      ? value as ParkItemType
      : null;
    this.selectedAttractionType.set(selectedType);
    this.stateFacade.load(1, this.currentFilter().category, this.searchTerm(), selectedType);
  }

  protected updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  protected applySearch(): void {
    this.stateFacade.load(
      1,
      this.currentFilter().category,
      this.searchTerm(),
      this.selectedAttractionType()
    );
  }

  protected clearSearch(): void {
    this.searchTerm.set('');
    this.stateFacade.load(
      1,
      this.currentFilter().category,
      null,
      this.selectedAttractionType()
    );
  }

  protected loadMore(): void {
    this.stateFacade.loadMore();
  }

  protected updateRating(change: RatingTreeRatingChange | RatingRankingListRatingChange): void {
    this.stateFacade.updateRating(change.ratingId, change.value);
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

  private mapParkRanking(
    ranking: UserParkRatingRanking,
    language: string,
    savingRatingIds: ReadonlySet<string>
  ): RatingTreePark {
    const itemRatings: UserRatingListItem[] = ranking.categories.flatMap(
      (category: UserParkRatingRankingCategory): UserRatingListItem[] => category.items
    );
    return {
      id: ranking.parkId,
      rank: ranking.rank,
      name: ranking.parkName,
      score: ranking.averageRating,
      ratingCount: ranking.ratingCount,
      route: this.parkRoute(ranking.parkId, ranking.parkName, language),
      metrics: this.buildMetrics(ranking.parkRating ?? null, itemRatings),
      sections: ranking.categories.map((category: UserParkRatingRankingCategory): RatingTreeSection => {
        return {
          id: category.parkItemCategory,
          titleKey: `ratings.categories.${category.parkItemCategory}`,
          score: category.averageRating,
          items: category.items.map((rating: UserRatingListItem) => {
            return {
              id: rating.id,
              name: rating.targetName,
              score: rating.value,
              route: this.targetRoute(rating, language),
              editable: this.editableScore(rating.id, savingRatingIds)
            };
          })
        };
      })
    };
  }

  private buildMetrics(
    parkRating: UserRatingListItem | null,
    itemRatings: UserRatingListItem[]
  ): RatingTreeMetric[] {
    return [
      {
        labelKey: 'ratings.rankings.parkSignal',
        value: parkRating?.value ?? 0,
        editable: parkRating ? this.editableScore(parkRating.id, this.savingRatingIds()) : null
      },
      {
        labelKey: 'ratings.rankings.itemsSignal',
        value: this.averageRating(itemRatings)
      }
    ];
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

  private averageRating(ratings: UserRatingListItem[]): number {
    if (ratings.length === 0) {
      return 0;
    }

    const ratingSum: number = ratings.reduce(
      (sum: number, rating: UserRatingListItem): number => sum + rating.value,
      0
    );
    return ratingSum / ratings.length;
  }

  private editableScore(ratingId: string, savingRatingIds: ReadonlySet<string>): RatingTreeEditableScore {
    return {
      ratingId,
      saving: savingRatingIds.has(ratingId)
    };
  }
}
