import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest } from 'rxjs';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import {
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicVideoFilterState } from '@features/public/videos/models/public-video-view.model';
import { PublicVideoBackLink, PublicVideoListViewComponent } from '@features/public/videos/ui/public-video-list-view.component';
import {
  buildPublicVideoFilterKey,
  buildPublicVideoFilterQueryParams,
  parsePublicVideoFilters
} from '@features/public/videos/utils/public-video-filter-query.helpers';
import { ParkVideosStateFacade } from '../state/park-videos-state.facade';

@Component({
  selector: 'app-park-videos-page',
  templateUrl: './park-videos-page.component.html',
  styleUrls: ['./park-videos-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkVideosStateFacade],
  imports: [PublicVideoListViewComponent]
})
export class ParkVideosPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
  protected readonly videos = this.stateFacade.videoCards;
  protected readonly totalVideos = this.stateFacade.totalVideos;
  protected readonly canLoadMore = this.stateFacade.canLoadMore;
  protected readonly loadingMore = this.stateFacade.loadingMore;
  protected readonly filters = this.stateFacade.filters;
  protected readonly typeOptions = this.stateFacade.typeOptions;
  protected readonly tagOptions = this.stateFacade.tagOptions;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly titleParams = signal<Record<string, string | number | null | undefined>>({});
  protected readonly backLinks = signal<PublicVideoBackLink[]>([]);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentLoadKey: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: ParkVideosStateFacade
  ) {
    effect((): void => {
      const currentPark = this.park();
      if (!currentPark) {
        return;
      }

      this.titleParams.set({
        name: currentPark.name,
        count: this.totalVideos()
      });
      this.backLinks.set([
        {
          routerLink: buildPublicParkRouteCommands({
            language: this.currentLanguage(),
            parkId: currentPark.id,
            parkName: currentPark.name
          }),
          labelKey: 'parks.videosPage.backToPark',
          labelParams: { name: currentPark.name },
          iconClass: 'pi pi-arrow-left',
          variant: 'ghost'
        },
        {
          routerLink: buildPublicParkItemsRouteCommands({
            language: this.currentLanguage(),
            parkId: currentPark.id,
            parkName: currentPark.name
          }),
          labelKey: 'parkVisitor.summary.viewAllItems',
          iconClass: 'pi pi-sitemap',
          variant: 'primary'
        }
      ]);
      this.seoService.applyParkVideosSeo(currentPark, this.currentLanguage(), this.router.url, this.totalVideos());
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    this.currentLanguage.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLanguage.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });

    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([params, queryParams]: [ParamMap, ParamMap]) => {
        const parkId: string | null = params.get('id');
        if (!parkId) {
          return;
        }

        const filters: PublicVideoFilterState = parsePublicVideoFilters(queryParams);
        const loadKey: string = `${parkId}|${buildPublicVideoFilterKey(filters)}`;
        if (loadKey === this.currentLoadKey) {
          return;
        }

        this.currentLoadKey = loadKey;
        this.stateFacade.loadParkVideos(parkId, filters);
      });
  }

  onFiltersChanged(filters: PublicVideoFilterState): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: buildPublicVideoFilterQueryParams(filters)
    });
  }

  onLoadMoreClicked(): void {
    this.stateFacade.loadNextPage();
  }
}
