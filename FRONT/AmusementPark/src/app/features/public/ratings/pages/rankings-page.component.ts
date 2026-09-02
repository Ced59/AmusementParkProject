import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import {
  ParkItemRatingRanking,
  ParkRatingRanking,
  ParkRatingRankingCategory,
  ParkRatingRankingItem
} from '@app/models/ratings/rating.models';
import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { RatingTreeComponent, RatingTreePark } from '@shared/components/rating-tree/rating-tree.component';
import { RatingEvidenceViewModel } from '@shared/components/rating-evidence/rating-evidence.component';
import {
  RatingRankingListComponent,
  RatingRankingListItem
} from '@shared/components/rating-ranking-list/rating-ranking-list.component';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ATTRACTION_TYPE_OPTIONS, TranslationOption } from '@shared/utils/display/display-options';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { RankingsStateFacade } from '../state/rankings-state.facade';
import { LocalizedPluralPipe } from '@shared/pipes';

interface RankingFilter {
  key: string;
  labelKey: string;
  category: string | null;
}

@Component({
  selector: 'app-rankings-page',
  templateUrl: './rankings-page.component.html',
  styleUrls: ['./rankings-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [RankingsStateFacade],
  imports: [
    DatePipe,
    RatingTreeComponent,
    RatingRankingListComponent,
    RouterLink,
    TranslateModule,
    LocalizedPluralPipe,
    UiButtonDirective,
    UiSectionHeaderComponent
  ]
})
export class RankingsPageComponent implements OnInit {
  protected readonly searchTerm = signal<string>('');
  protected readonly filters: readonly RankingFilter[] = [
    { key: 'all', labelKey: 'ratings.rankings.filters.all', category: null },
    { key: 'attractions', labelKey: 'ratings.rankings.filters.attractions', category: 'Attraction' },
    { key: 'restaurants', labelKey: 'ratings.rankings.filters.restaurants', category: 'Restaurant' },
    { key: 'hotels', labelKey: 'ratings.rankings.filters.hotels', category: 'Hotel' },
    { key: 'services', labelKey: 'ratings.rankings.filters.services', category: 'Service' }
  ];
  protected readonly currentFilter = signal<RankingFilter>(this.filters[0]);
  protected readonly selectedAttractionType = signal<ParkItemType | null>(null);
  protected readonly attractionTypeOptions: ReadonlyArray<TranslationOption<ParkItemType>> = ATTRACTION_TYPE_OPTIONS;
  protected readonly currentLang = signal<string>('en');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly loadingMore: Signal<boolean> = this.stateFacade.loadingMore;
  protected readonly hasMore: Signal<boolean> = this.stateFacade.hasMore;
  protected readonly items: Signal<ParkRatingRanking[]> = this.stateFacade.items;
  protected readonly parkItems: Signal<ParkItemRatingRanking[]> = this.stateFacade.parkItems;
  protected readonly methodology: Signal<RatingMethodology | null> = this.stateFacade.methodology;
  protected readonly isParkItemRanking: Signal<boolean> = computed(() => this.currentFilter().category !== null);
  protected readonly hasRankings: Signal<boolean> = computed(() => {
    return this.isParkItemRanking() ? this.parkItems().length > 0 : this.items().length > 0;
  });
  protected readonly treeParks: Signal<RatingTreePark[]> = computed(() => {
    return this.items().map((item: ParkRatingRanking): RatingTreePark => this.mapRankingToTree(item));
  });
  protected readonly rankedParkItems: Signal<RatingRankingListItem[]> = computed(() => {
    return this.parkItems().map((item: ParkItemRatingRanking): RatingRankingListItem => {
      return {
        id: item.targetId,
        rank: this.visibleRank(item.rank, item.evidence),
        name: item.targetName,
        score: item.averageRating,
        ratingCount: item.ratingCount,
        route: buildPublicParkItemRouteCommands({
          language: this.currentLang(),
          parkId: item.parkId,
          parkName: item.parkName,
          itemId: item.targetId,
          itemName: item.targetName
        }),
        parkName: item.parkName,
        parkRoute: buildPublicParkRouteCommands({
          language: this.currentLang(),
          parkId: item.parkId,
          parkName: item.parkName
        }),
        evidence: this.mapEvidence(
          'ParkItem',
          item.evidence,
          item.uniqueContributorCount,
          item.ratingObservationCount ?? item.ratingCount,
          item.rank,
          item.methodologyVersion
        )
      };
    });
  });
  protected readonly rankedDisplayedCount: Signal<number> = computed(() => {
    return this.displayedEntries().filter((entry: ParkRatingRanking | ParkItemRatingRanking): boolean => {
      return this.visibleRank(entry.rank, entry.evidence) !== null;
    }).length;
  });
  protected readonly provisionalDisplayedCount: Signal<number> = computed(() => {
    return this.displayedEntries().filter((entry: ParkRatingRanking | ParkItemRatingRanking): boolean => {
      return entry.evidence?.level === 'Provisional';
    }).length;
  });
  protected readonly generatedAtUtc: Signal<string | null> = computed(() => {
    return this.displayedEntries().find((entry: ParkRatingRanking | ParkItemRatingRanking): boolean => {
      return Boolean(entry.generatedAtUtc);
    })?.generatedAtUtc ?? null;
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: RankingsStateFacade,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.currentLang.set(language);
    this.seoService.applyRouteDefaults(this.router.url);
    this.stateFacade.load();

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string): void => {
      this.currentLang.set(lang);
      this.seoService.applyRouteDefaults(this.router.url);
    });
  }

  protected selectFilter(filter: RankingFilter): void {
    this.currentFilter.set(filter);
    this.selectedAttractionType.set(null);
    this.stateFacade.load(filter.category, this.searchTerm());
  }

  protected selectAttractionType(value: string): void {
    const selectedType: ParkItemType | null = value.trim().length > 0
      ? value as ParkItemType
      : null;
    this.selectedAttractionType.set(selectedType);
    this.stateFacade.load(this.currentFilter().category, this.searchTerm(), selectedType);
  }

  protected loadMore(): void {
    this.stateFacade.loadMore();
  }

  protected updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  protected applySearch(): void {
    this.stateFacade.load(
      this.currentFilter().category,
      this.searchTerm(),
      this.selectedAttractionType()
    );
  }

  protected clearSearch(): void {
    this.searchTerm.set('');
    this.stateFacade.load(
      this.currentFilter().category,
      null,
      this.selectedAttractionType()
    );
  }

  private parkRoute(item: ParkRatingRanking): string[] | null {
    return buildPublicParkRouteCommands({
      language: this.currentLang(),
      parkId: item.parkId,
      parkName: item.parkName
    });
  }

  private itemRoute(park: ParkRatingRanking, item: ParkRatingRankingItem): string[] | null {
    return buildPublicParkItemRouteCommands({
      language: this.currentLang(),
      parkId: park.parkId,
      parkName: park.parkName,
      itemId: item.targetId,
      itemName: item.targetName
    });
  }

  private categoryLabelKey(category: ParkRatingRankingCategory): string {
    return `ratings.categories.${category.parkItemCategory}`;
  }

  private mapRankingToTree(item: ParkRatingRanking): RatingTreePark {
    return {
      id: item.parkId,
      rank: this.visibleRank(item.rank, item.evidence),
      name: item.parkName,
      score: item.score,
      ratingCount: item.ratingCount,
      route: this.parkRoute(item),
      evidence: this.mapEvidence(
        'Park',
        item.evidence,
        item.uniqueContributorCount,
        item.ratingObservationCount ?? null,
        item.rank,
        item.methodologyVersion
      ),
      metrics: [
        {
          labelKey: 'ratings.rankings.parkSignal',
          value: item.parkAverageRating,
          ratingCount: item.parkRatingCount
        },
        {
          labelKey: 'ratings.rankings.itemsSignal',
          value: item.itemsAverageRating,
          ratingCount: item.itemsRatingCount
        }
      ],
      sections: item.categories.map((category: ParkRatingRankingCategory) => {
        return {
          id: category.parkItemCategory,
          titleKey: this.categoryLabelKey(category),
          score: category.averageRating,
          ratingCount: category.ratingCount,
          items: category.items.map((parkItem: ParkRatingRankingItem) => {
            return {
              id: parkItem.targetId,
              name: parkItem.targetName,
              score: parkItem.averageRating,
              ratingCount: parkItem.ratingCount,
              route: this.itemRoute(item, parkItem)
            };
          })
        };
      })
    };
  }

  private displayedEntries(): Array<ParkRatingRanking | ParkItemRatingRanking> {
    return this.isParkItemRanking() ? this.parkItems() : this.items();
  }

  private visibleRank(
    rank: number | null,
    evidence: ParkRatingRanking['evidence'] | ParkItemRatingRanking['evidence']
  ): number | null {
    return evidence && !evidence.isEligibleForMainRanking ? null : rank;
  }

  private mapEvidence(
    targetType: 'Park' | 'ParkItem',
    evidence: ParkRatingRanking['evidence'] | ParkItemRatingRanking['evidence'],
    uniqueContributorCount: number | null | undefined,
    ratingObservationCount: number | null | undefined,
    rank: number | null,
    methodologyVersion: string | null | undefined
  ): RatingEvidenceViewModel | null {
    if (!evidence) {
      return null;
    }

    const resolvedMethodologyVersion: string | null = methodologyVersion ?? null;
    const methodology: RatingMethodology | null = this.methodology();
    const matchingMethodology: RatingMethodology | null = resolvedMethodologyVersion
      && methodology?.version === resolvedMethodologyVersion
      ? methodology
      : null;

    return {
      evidence,
      uniqueContributorCount: uniqueContributorCount ?? null,
      ratingObservationCount: ratingObservationCount ?? null,
      targetType,
      rank: this.visibleRank(rank, evidence),
      methodologyVersion: resolvedMethodologyVersion,
      eligibilityThreshold: matchingMethodology?.evidenceThresholds.eligible ?? null
    };
  }
}
