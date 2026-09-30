import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, Input, OnInit, Signal, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule, TranslateService as NgxTranslateService } from '@ngx-translate/core';

import {
  UserParkItemRatingRanking,
  UserParkRatingRanking,
  UserParkRatingRankingCategory,
  UserRatingListItem,
  UserRatingStatBucket,
  UserRatingStats
} from '@app/models/ratings/rating.models';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
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
import { PaginationContract } from '@shared/models/contracts';
import { LocalizedPluralPipe } from '@shared/pipes';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { GlobalRatingSuggestionsComponent } from '../passport/components/global-rating-suggestions/global-rating-suggestions.component';
import { GlobalRatingSuggestionViewModel } from '../passport/models/global-rating-suggestion-view.models';
import { UserRankingShareControlComponent } from '../sharing/components/user-ranking-share-control/user-ranking-share-control.component';
import { ProfileRatingsStateFacade } from './profile-ratings-state.facade';
import { UserRankingShareStateFacade } from './user-ranking-share-state.facade';

interface ProfileRankingFilter {
  key: string;
  labelKey: string;
  iconClass: string;
  category: ParkItemCategory | null;
}

interface ProfileAttractionQuickFilter {
  labelKey: string;
  type: ParkItemType | null;
}

@Component({
  selector: 'app-profile-ratings-panel',
  templateUrl: './profile-ratings-panel.component.html',
  styleUrls: ['./profile-ratings-panel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ProfileRatingsStateFacade, UserRankingShareStateFacade],
  imports: [
    RatingTreeComponent,
    RatingRankingListComponent,
    TranslateModule,
    LocalizedPluralPipe,
    UserRankingShareControlComponent,
    UiButtonDirective,
    UiSectionHeaderComponent,
    GlobalRatingSuggestionsComponent
  ]
})
export class ProfileRatingsPanelComponent implements OnInit {
  @Input() displayName: string = '';

  protected readonly searchTerm = signal<string>('');
  protected readonly currentLang = signal<string>('en');
  protected readonly filters: readonly ProfileRankingFilter[] = [
    { key: 'parks', labelKey: 'ratings.rankings.filters.parks', iconClass: 'pi pi-map', category: null },
    { key: 'attractions', labelKey: 'ratings.categories.Attraction', iconClass: 'pi pi-bolt', category: 'Attraction' },
    { key: 'restaurants', labelKey: 'ratings.categories.Restaurant', iconClass: 'pi pi-shop', category: 'Restaurant' },
    { key: 'hotels', labelKey: 'ratings.categories.Hotel', iconClass: 'pi pi-building', category: 'Hotel' },
    { key: 'animals', labelKey: 'ratings.categories.Animal', iconClass: 'pi pi-heart', category: 'Animal' },
    { key: 'shows', labelKey: 'ratings.categories.Show', iconClass: 'pi pi-ticket', category: 'Show' },
    { key: 'shops', labelKey: 'ratings.categories.Shop', iconClass: 'pi pi-shopping-bag', category: 'Shop' },
    { key: 'services', labelKey: 'ratings.categories.Service', iconClass: 'pi pi-info-circle', category: 'Service' },
    { key: 'transports', labelKey: 'ratings.categories.Transport', iconClass: 'pi pi-send', category: 'Transport' },
    { key: 'other', labelKey: 'ratings.categories.Other', iconClass: 'pi pi-ellipsis-h', category: 'Other' }
  ];
  protected readonly attractionQuickFilters: readonly ProfileAttractionQuickFilter[] = [
    { labelKey: 'ratings.rankings.allAttractionTypes', type: null },
    { labelKey: 'parkExplorer.types.rollerCoaster', type: 'RollerCoaster' },
    { labelKey: 'parkExplorer.types.flatRide', type: 'FlatRide' },
    { labelKey: 'parkExplorer.types.waterRide', type: 'WaterRide' },
    { labelKey: 'parkExplorer.types.darkRide', type: 'DarkRide' }
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
  protected readonly pagination: Signal<PaginationContract | null> = this.stateFacade.pagination;
  protected readonly isEmpty: Signal<boolean> = this.stateFacade.isEmpty;
  protected readonly savingRatingIds: Signal<ReadonlySet<string>> = this.stateFacade.savingRatingIds;
  protected readonly isParkItemRanking: Signal<boolean> = computed(() => this.currentFilter().category !== null);
  protected readonly currentRankingLabelKey: Signal<string> = computed(() => {
    const attractionType: ParkItemType | null = this.selectedAttractionType();
    if (this.currentFilter().category !== 'Attraction' || attractionType === null) {
      return this.currentFilter().labelKey;
    }

    return this.attractionTypeOptions.find(
      (option: TranslationOption<ParkItemType>): boolean => option.value === attractionType
    )?.labelKey ?? this.currentFilter().labelKey;
  });
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
      const targetName: string | null = this.publicLabel(rating.targetName, rating.targetId);
      const parkName: string | null = this.publicLabel(rating.parkName, rating.parkId);
      return {
        id: rating.id,
        rank: ranking.rank,
        name: targetName ?? this.translate.instant('ratings.profile.unavailable.target'),
        score: rating.value,
        route: targetName ? this.targetRoute(rating, language, targetName, parkName) : null,
        parkName: parkName ?? this.translate.instant('ratings.profile.unavailable.park'),
        parkRoute: parkName ? this.parkRoute(rating.parkId, parkName, language) : null,
        editable: this.editableScore(rating.id, savingRatingIds)
      };
    });
  });

  constructor(
    private readonly stateFacade: ProfileRatingsStateFacade,
    private readonly shareStateFacade: UserRankingShareStateFacade,
    private readonly translationService: TranslationService,
    private readonly translate: NgxTranslateService,
    private readonly destroyRef: DestroyRef,
    private readonly elementRef: ElementRef<HTMLElement>
  ) {
    effect((): void => {
      if (this.stateFacade.ratingMutationRevision() > 0) {
        this.shareStateFacade.refreshAfterSourceChange();
      }
    });
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

  protected selectAttractionQuickFilter(filter: ProfileAttractionQuickFilter): void {
    this.selectedAttractionType.set(filter.type);
    this.stateFacade.load(1, 'Attraction', this.searchTerm(), filter.type);
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

  protected showParkRanking(): void {
    this.searchTerm.set('');
    this.selectFilter(this.filters[0]);
  }

  protected loadMore(): void {
    this.stateFacade.loadMore();
  }

  protected updateRating(change: RatingTreeRatingChange | RatingRankingListRatingChange): void {
    this.stateFacade.updateRating(change.ratingId, change.value);
  }

  protected reviewSuggestion(suggestion: GlobalRatingSuggestionViewModel): void {
    const filter: ProfileRankingFilter = suggestion.targetType === 'Park'
      ? this.filters[0]
      : this.filters.find((candidate: ProfileRankingFilter): boolean => {
        return candidate.category === suggestion.parkItemCategory;
      }) ?? this.filters[this.filters.length - 1];
    const search: string = suggestion.targetName;
    this.currentFilter.set(filter);
    this.selectedAttractionType.set(null);
    this.searchTerm.set(search);
    this.stateFacade.load(1, filter.category, search, null, suggestion.targetId);
    setTimeout((): void => {
      this.elementRef.nativeElement.querySelector<HTMLElement>('.profile-ratings__results')
        ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });
  }

  protected formatRating(value: number | null | undefined): string {
    const rating: number = Number(value ?? 0);
    if (rating <= 0) {
      return '-';
    }

    return new Intl.NumberFormat(this.currentLang(), {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(rating);
  }

  protected ratingPercent(value: number | null | undefined): string {
    const rating: number = Math.max(0, Math.min(5, Number(value ?? 0)));
    return `${(rating / 5) * 100}%`;
  }

  protected bucketLabel(bucket: UserRatingStatBucket, kind: 'targetType' | 'category' | 'park'): string {
    if (kind === 'park') {
      return this.publicLabel(bucket.label, bucket.key)
        ?? this.translate.instant('ratings.profile.unavailable.park');
    }

    const keyPrefix: string = kind === 'targetType' ? 'ratings.targetTypes' : 'ratings.categories';
    return `${keyPrefix}.${bucket.key}`;
  }

  private mapParkRanking(
    ranking: UserParkRatingRanking,
    language: string,
    savingRatingIds: ReadonlySet<string>
  ): RatingTreePark {
    const parkName: string | null = this.publicLabel(ranking.parkName, ranking.parkId);
    const itemRatings: UserRatingListItem[] = ranking.categories.flatMap(
      (category: UserParkRatingRankingCategory): UserRatingListItem[] => category.items
    );
    return {
      id: ranking.parkId,
      rank: ranking.rank,
      name: parkName ?? this.translate.instant('ratings.profile.unavailable.park'),
      score: ranking.averageRating,
      ratingCount: ranking.ratingCount,
      route: parkName ? this.parkRoute(ranking.parkId, parkName, language) : null,
      metrics: this.buildMetrics(ranking.parkRating ?? null, itemRatings),
      sections: ranking.categories.map((category: UserParkRatingRankingCategory): RatingTreeSection => {
        return {
          id: category.parkItemCategory,
          titleKey: `ratings.categories.${category.parkItemCategory}`,
          score: category.averageRating,
          items: category.items.map((rating: UserRatingListItem) => {
            const targetName: string | null = this.publicLabel(rating.targetName, rating.targetId);
            return {
              id: rating.id,
              name: targetName ?? this.translate.instant('ratings.profile.unavailable.target'),
              score: rating.value,
              route: targetName ? this.targetRoute(rating, language, targetName, parkName) : null,
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

  private targetRoute(
    rating: UserRatingListItem,
    language: string,
    targetName: string,
    parkName: string | null
  ): string[] | null {
    if (rating.targetType === 'Park') {
      return buildPublicParkRouteCommands({
        language,
        parkId: rating.parkId,
        parkName: targetName
      });
    }

    if (!parkName) {
      return null;
    }

    return buildPublicParkItemRouteCommands({
      language,
      parkId: rating.parkId,
      parkName,
      itemId: rating.targetId,
      itemName: targetName
    });
  }

  private parkRoute(parkId: string, parkName: string, language: string): string[] | null {
    return buildPublicParkRouteCommands({
      language,
      parkId,
      parkName
    });
  }

  private publicLabel(value: string | null | undefined, technicalId: string): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 && normalizedValue !== technicalId
      ? normalizedValue
      : null;
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
