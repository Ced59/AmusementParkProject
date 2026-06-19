import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ParkRatingRanking, ParkRatingRankingCategory, ParkRatingRankingItem } from '@app/models/ratings/rating.models';
import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { RatingTreeComponent, RatingTreePark } from '@shared/components/rating-tree/rating-tree.component';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiSectionHeaderComponent } from '@ui/primitives';
import { RankingsStateFacade } from '../state/rankings-state.facade';

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
    RatingTreeComponent,
    TranslateModule,
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
  protected readonly currentLang = signal<string>('en');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly loadingMore: Signal<boolean> = this.stateFacade.loadingMore;
  protected readonly hasMore: Signal<boolean> = this.stateFacade.hasMore;
  protected readonly items: Signal<ParkRatingRanking[]> = this.stateFacade.items;
  protected readonly treeParks: Signal<RatingTreePark[]> = computed(() => {
    return this.items().map((item: ParkRatingRanking): RatingTreePark => this.mapRankingToTree(item));
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
    this.stateFacade.load(filter.category, this.searchTerm());
  }

  protected loadMore(): void {
    this.stateFacade.loadMore();
  }

  protected updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  protected applySearch(): void {
    this.stateFacade.load(this.currentFilter().category, this.searchTerm());
  }

  protected clearSearch(): void {
    this.searchTerm.set('');
    this.stateFacade.load(this.currentFilter().category);
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
      rank: item.rank,
      name: item.parkName,
      score: item.score,
      ratingCount: item.ratingCount,
      route: this.parkRoute(item),
      metrics: [
        { labelKey: 'ratings.rankings.parkSignal', value: item.parkAverageRating },
        { labelKey: 'ratings.rankings.itemsSignal', value: item.itemsAverageRating }
      ],
      sections: item.categories.map((category: ParkRatingRankingCategory) => {
        return {
          id: category.parkItemCategory,
          titleKey: this.categoryLabelKey(category),
          score: category.averageRating,
          items: category.items.map((parkItem: ParkRatingRankingItem) => {
            return {
              id: parkItem.targetId,
              name: parkItem.targetName,
              score: parkItem.averageRating,
              route: this.itemRoute(item, parkItem)
            };
          })
        };
      })
    };
  }
}
