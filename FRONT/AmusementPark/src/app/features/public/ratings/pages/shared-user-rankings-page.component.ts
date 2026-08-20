import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  SharedUserRankingProfile,
  UserParkItemRatingRanking,
  UserParkRatingRanking,
  UserParkRatingRankingCategory,
  UserRatingListItem
} from '@app/models/ratings/rating.models';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { environment } from '../../../../../environments/environment';
import {
  RatingRankingListComponent,
  RatingRankingListItem
} from '@shared/components/rating-ranking-list/rating-ranking-list.component';
import {
  RatingTreeComponent,
  RatingTreeMetric,
  RatingTreePark,
  RatingTreeSection
} from '@shared/components/rating-tree/rating-tree.component';
import { PaginationContract } from '@shared/models/contracts';
import { ATTRACTION_TYPE_OPTIONS, TranslationOption } from '@shared/utils/display/display-options';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiButtonDirective } from '@ui/primitives';
import { SharedUserRankingsStateFacade } from '../state/shared-user-rankings-state.facade';

interface SharedRankingFilter {
  readonly key: string;
  readonly labelKey: string;
  readonly iconClass: string;
  readonly category: ParkItemCategory | null;
}

interface SharedAttractionQuickFilter {
  readonly labelKey: string;
  readonly type: ParkItemType | null;
}

@Component({
  selector: 'app-shared-user-rankings-page',
  templateUrl: './shared-user-rankings-page.component.html',
  styleUrl: './shared-user-rankings-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SharedUserRankingsStateFacade],
  imports: [
    PublicSharePanelComponent,
    RatingRankingListComponent,
    RatingTreeComponent,
    RouterLink,
    TranslateModule,
    UiButtonDirective
  ]
})
export class SharedUserRankingsPageComponent implements OnInit {
  protected readonly filters: readonly SharedRankingFilter[] = [
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
  protected readonly attractionQuickFilters: readonly SharedAttractionQuickFilter[] = [
    { labelKey: 'ratings.rankings.allAttractionTypes', type: null },
    { labelKey: 'parkExplorer.types.rollerCoaster', type: 'RollerCoaster' },
    { labelKey: 'parkExplorer.types.flatRide', type: 'FlatRide' },
    { labelKey: 'parkExplorer.types.waterRide', type: 'WaterRide' },
    { labelKey: 'parkExplorer.types.darkRide', type: 'DarkRide' }
  ];
  protected readonly attractionTypeOptions: ReadonlyArray<TranslationOption<ParkItemType>> = ATTRACTION_TYPE_OPTIONS;
  protected readonly currentFilter = signal<SharedRankingFilter>(this.filters[0]);
  protected readonly selectedAttractionType = signal<ParkItemType | null>(null);
  protected readonly searchTerm = signal<string>('');
  protected readonly currentLang = signal<string>('en');
  protected readonly shareId = signal<string>('');
  protected readonly profile: Signal<SharedUserRankingProfile | null> = this.stateFacade.profile;
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly loadingMore: Signal<boolean> = this.stateFacade.loadingMore;
  protected readonly notFound: Signal<boolean> = this.stateFacade.notFound;
  protected readonly error: Signal<boolean> = this.stateFacade.error;
  protected readonly hasMore: Signal<boolean> = this.stateFacade.hasMore;
  protected readonly isEmpty: Signal<boolean> = this.stateFacade.isEmpty;
  protected readonly pagination: Signal<PaginationContract | null> = this.stateFacade.pagination;
  protected readonly isParkItemRanking: Signal<boolean> = computed(() => this.currentFilter().category !== null);
  protected readonly isLoggedIn: Signal<boolean> = computed(() => this.authService.isLoggedIn());
  protected readonly currentRankingLabelKey: Signal<string> = computed(() => {
    const type: ParkItemType | null = this.selectedAttractionType();
    if (this.currentFilter().category !== 'Attraction' || type === null) {
      return this.currentFilter().labelKey;
    }

    return this.attractionTypeOptions.find(
      (option: TranslationOption<ParkItemType>): boolean => option.value === type
    )?.labelKey ?? this.currentFilter().labelKey;
  });
  protected readonly ratingParks: Signal<RatingTreePark[]> = computed(() => {
    return this.stateFacade.parkRankings().map(
      (ranking: UserParkRatingRanking): RatingTreePark => this.mapParkRanking(ranking)
    );
  });
  protected readonly rankedParkItems: Signal<RatingRankingListItem[]> = computed(() => {
    return this.stateFacade.parkItemRankings().map((ranking: UserParkItemRatingRanking): RatingRankingListItem => {
      const rating: UserRatingListItem = ranking.rating;
      return {
        id: rating.id,
        rank: ranking.rank,
        name: rating.targetName,
        score: rating.value,
        route: this.targetRoute(rating),
        parkName: rating.parkName || rating.parkId,
        parkRoute: this.parkRoute(rating.parkId, rating.parkName || rating.parkId)
      };
    });
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: SharedUserRankingsStateFacade,
    private readonly authService: AuthService,
    private readonly modalService: ModalService,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const currentProfile: SharedUserRankingProfile | null = this.profile();
      if (currentProfile) {
        this.applySeo(currentProfile);
      }

      if (this.notFound()) {
        this.ssrHttpStatusService.setNotFound();
        this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);
      }
    });
  }

  ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    const shareId: string = this.route.snapshot.paramMap.get('shareId')?.trim() ?? '';
    const requestedCategory: string | null = this.route.snapshot.queryParamMap.get('category');
    const initialFilter: SharedRankingFilter = this.filters.find(
      (filter: SharedRankingFilter): boolean => filter.category === requestedCategory,
    ) ?? this.filters[0];
    const requestedType: string | null = this.route.snapshot.queryParamMap.get('type');
    const initialType: ParkItemType | null = initialFilter.category === 'Attraction'
      && this.attractionTypeOptions.some(
        (option: TranslationOption<ParkItemType>): boolean => option.value === requestedType,
      )
      ? requestedType as ParkItemType
      : null;
    this.currentLang.set(language);
    this.shareId.set(shareId);
    this.currentFilter.set(initialFilter);
    this.selectedAttractionType.set(initialType);
    this.seoService.applyRouteDefaults(this.router.url);

    if (shareId.length === 0) {
      this.ssrHttpStatusService.setNotFound();
      this.seoService.applyNotFoundSeo(language, this.router.url);
      return;
    }

    this.stateFacade.loadProfile(shareId, initialFilter.category, null, initialType);
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string): void => {
      this.currentLang.set(lang);
      const currentProfile: SharedUserRankingProfile | null = this.profile();
      if (currentProfile) {
        this.applySeo(currentProfile);
      }
    });
  }

  protected selectFilter(filter: SharedRankingFilter): void {
    this.currentFilter.set(filter);
    this.selectedAttractionType.set(null);
    this.updateRankingRoute();
    this.stateFacade.load(filter.category, this.searchTerm(), null);
  }

  protected selectAttractionQuickFilter(filter: SharedAttractionQuickFilter): void {
    this.selectedAttractionType.set(filter.type);
    this.updateRankingRoute();
    this.stateFacade.load('Attraction', this.searchTerm(), filter.type);
  }

  protected selectAttractionType(value: string): void {
    const selectedType: ParkItemType | null = value.trim().length > 0 ? value as ParkItemType : null;
    this.selectedAttractionType.set(selectedType);
    this.updateRankingRoute();
    this.stateFacade.load('Attraction', this.searchTerm(), selectedType);
  }

  protected updateSearchTerm(value: string): void {
    this.searchTerm.set(value);
  }

  protected applySearch(): void {
    this.stateFacade.load(this.currentFilter().category, this.searchTerm(), this.selectedAttractionType());
  }

  protected clearSearch(): void {
    this.searchTerm.set('');
    this.stateFacade.load(this.currentFilter().category, null, this.selectedAttractionType());
  }

  protected loadMore(): void {
    this.stateFacade.loadMore();
  }

  protected openAccountCreation(): void {
    this.modalService.openModal('loginModal');
  }

  protected formatRating(value: number | null | undefined): string {
    const rating: number = Number(value ?? 0);
    return rating > 0
      ? new Intl.NumberFormat(this.currentLang(), { minimumFractionDigits: 1, maximumFractionDigits: 1 }).format(rating)
      : '-';
  }

  private updateRankingRoute(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        category: this.currentFilter().category,
        type: this.selectedAttractionType()
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });

    const currentProfile: SharedUserRankingProfile | null = this.profile();
    if (currentProfile) {
      this.applySeo(currentProfile);
    }
  }

  private applySeo(profile: SharedUserRankingProfile): void {
    const params: Record<string, string> = { name: profile.displayName };
    const title: string = this.translateService.instant('ratings.share.public.seoTitle', params);
    const description: string = this.translateService.instant('ratings.share.public.seoDescription', params);
    const imageAlt: string = this.translateService.instant('ratings.share.public.imageAlt', params);
    const previewEndpoint: string = `${environment.apiBaseUrl}${this.buildPreviewPath()}`;
    this.seoService.applySharedUserRankingSeo(title, description, this.router.url, previewEndpoint, imageAlt);
  }

  private buildPreviewPath(): string {
    const params: URLSearchParams = new URLSearchParams();
    const category: ParkItemCategory | null = this.currentFilter().category;
    const type: ParkItemType | null = this.selectedAttractionType();
    if (category) {
      params.set('category', category);
    }
    if (type) {
      params.set('type', type);
    }
    const query: string = params.toString();
    return `ratings/shared/${encodeURIComponent(this.shareId())}/preview.png${query ? `?${query}` : ''}`;
  }

  private mapParkRanking(ranking: UserParkRatingRanking): RatingTreePark {
    const itemRatings: UserRatingListItem[] = ranking.categories.flatMap(
      (category: UserParkRatingRankingCategory): UserRatingListItem[] => category.items
    );
    return {
      id: ranking.parkId,
      rank: ranking.rank,
      name: ranking.parkName,
      score: ranking.averageRating,
      ratingCount: ranking.ratingCount,
      route: this.parkRoute(ranking.parkId, ranking.parkName),
      metrics: this.buildMetrics(ranking.parkRating ?? null, itemRatings),
      sections: ranking.categories.map((category: UserParkRatingRankingCategory): RatingTreeSection => ({
        id: category.parkItemCategory,
        titleKey: `ratings.categories.${category.parkItemCategory}`,
        score: category.averageRating,
        items: category.items.map((rating: UserRatingListItem) => ({
          id: rating.id,
          name: rating.targetName,
          score: rating.value,
          route: this.targetRoute(rating)
        }))
      }))
    };
  }

  private buildMetrics(parkRating: UserRatingListItem | null, itemRatings: UserRatingListItem[]): RatingTreeMetric[] {
    return [
      { labelKey: 'ratings.rankings.parkSignal', value: parkRating?.value ?? 0 },
      { labelKey: 'ratings.rankings.itemsSignal', value: this.averageRating(itemRatings) }
    ];
  }

  private targetRoute(rating: UserRatingListItem): string[] | null {
    if (rating.targetType === 'Park') {
      return this.parkRoute(rating.parkId, rating.targetName);
    }

    return buildPublicParkItemRouteCommands({
      language: this.currentLang(),
      parkId: rating.parkId,
      parkName: rating.parkName,
      itemId: rating.targetId,
      itemName: rating.targetName
    });
  }

  private parkRoute(parkId: string, parkName: string): string[] | null {
    return buildPublicParkRouteCommands({ language: this.currentLang(), parkId, parkName });
  }

  private averageRating(ratings: UserRatingListItem[]): number {
    return ratings.length > 0
      ? ratings.reduce((sum: number, rating: UserRatingListItem): number => sum + rating.value, 0) / ratings.length
      : 0;
  }
}
