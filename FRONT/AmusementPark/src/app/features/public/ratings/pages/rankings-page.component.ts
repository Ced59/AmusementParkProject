import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { RatingRankingItem, RatingTargetType } from '@app/models/ratings/rating.models';
import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { buildPublicParkItemRouteCommands, buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent } from '@ui/primitives';
import { RankingsStateFacade } from '../state/rankings-state.facade';

interface RankingFilter {
  key: string;
  labelKey: string;
  targetType: RatingTargetType | null;
  category: string | null;
}

@Component({
  selector: 'app-rankings-page',
  templateUrl: './rankings-page.component.html',
  styleUrls: ['./rankings-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [RankingsStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiSectionHeaderComponent
  ]
})
export class RankingsPageComponent implements OnInit {
  protected readonly filters: readonly RankingFilter[] = [
    { key: 'all', labelKey: 'ratings.rankings.filters.all', targetType: null, category: null },
    { key: 'parks', labelKey: 'ratings.rankings.filters.parks', targetType: 'Park', category: null },
    { key: 'attractions', labelKey: 'ratings.rankings.filters.attractions', targetType: 'ParkItem', category: 'Attraction' },
    { key: 'restaurants', labelKey: 'ratings.rankings.filters.restaurants', targetType: 'ParkItem', category: 'Restaurant' },
    { key: 'hotels', labelKey: 'ratings.rankings.filters.hotels', targetType: 'ParkItem', category: 'Hotel' },
    { key: 'services', labelKey: 'ratings.rankings.filters.services', targetType: 'ParkItem', category: 'Service' }
  ];
  protected readonly currentFilter = signal<RankingFilter>(this.filters[0]);
  protected readonly currentLang = signal<string>('en');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly items: Signal<RatingRankingItem[]> = this.stateFacade.items;

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
    this.stateFacade.load(filter.targetType, filter.category);
  }

  protected formatRating(value: number): string {
    return value > 0 ? value.toFixed(1).replace('.', ',') : '-';
  }

  protected targetRoute(item: RatingRankingItem): string[] | null {
    if (item.targetType === 'Park') {
      return buildPublicParkRouteCommands({
        language: this.currentLang(),
        parkId: item.parkId,
        parkName: item.targetName
      });
    }

    return buildPublicParkItemRouteCommands({
      language: this.currentLang(),
      parkId: item.parkId,
      parkName: item.parkName,
      itemId: item.targetId,
      itemName: item.targetName
    });
  }
}
