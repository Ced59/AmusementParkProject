import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest, startWith } from 'rxjs';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import {
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicParkVideosRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicVideoFilterState } from '@features/public/videos/models/public-video-view.model';
import { PublicVideoBackLink, PublicVideoListTab, PublicVideoListViewComponent } from '@features/public/videos/ui/public-video-list-view.component';
import {
  buildPublicVideoFilterKey,
  buildPublicVideoFilterQueryParams,
  parsePublicVideoFilters
} from '@features/public/videos/utils/public-video-filter-query.helpers';
import { ParkVideosStateFacade } from '../state/park-videos-state.facade';
import { ParkVideosGalleryTab } from '../models/park-videos-view.model';

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
  protected readonly parkImageId = this.stateFacade.parkImageId;
  protected readonly videos = this.stateFacade.videoCards;
  protected readonly totalVideos = this.stateFacade.totalVideos;
  protected readonly parkTabVideoCount = this.stateFacade.parkTabVideoCount;
  protected readonly itemTabVideoCount = this.stateFacade.itemTabVideoCount;
  protected readonly showItemTab = this.stateFacade.showItemTab;
  protected readonly activeTab = this.stateFacade.activeTab;
  protected readonly canLoadMore = this.stateFacade.canLoadMore;
  protected readonly loadingMore = this.stateFacade.loadingMore;
  protected readonly itemVideosLoading = this.stateFacade.itemVideosLoading;
  protected readonly filters = this.stateFacade.filters;
  protected readonly typeOptions = this.stateFacade.typeOptions;
  protected readonly tagOptions = this.stateFacade.tagOptions;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly titleParams = signal<Record<string, string | number | null | undefined>>({});
  protected readonly backLinks = signal<PublicVideoBackLink[]>([]);
  protected readonly tabs = signal<PublicVideoListTab[]>([]);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentLoadKey: string | null = null;
  private currentParkId: string | null = null;

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

      const routeTarget = {
        language: this.currentLanguage(),
        parkId: currentPark.id,
        parkName: currentPark.name
      };

      this.titleParams.set({
        name: currentPark.name,
        count: this.totalVideos()
      });
      this.tabs.set(this.showItemTab()
        ? [
          { id: 'park', labelKey: 'parks.videosPage.tabs.park', count: this.parkTabVideoCount() },
          { id: 'items', labelKey: 'parks.videosPage.tabs.items', count: this.itemTabVideoCount() }
        ]
        : []);
      this.backLinks.set([
        {
          routerLink: buildPublicParkRouteCommands(routeTarget),
          labelKey: 'parks.videosPage.backToPark',
          labelParams: { name: currentPark.name },
          iconClass: 'pi pi-arrow-left',
          variant: 'ghost'
        },
        {
          routerLink: buildPublicParkItemsRouteCommands(routeTarget),
          labelKey: 'parkVisitor.summary.viewAllItems',
          iconClass: 'pi pi-sitemap',
          variant: 'primary'
        }
      ]);
      this.seoService.applyParkVideosSeo(
        currentPark,
        this.currentLanguage(),
        this.router.url,
        this.totalVideos(),
        this.videos()[0]?.thumbnailPathOrUrl ?? null,
        this.parkImageId(),
        buildPublicRoutePath(buildPublicParkVideosRouteCommands(routeTarget))
      );
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    combineLatest([
      this.route.paramMap,
      this.route.queryParamMap,
      this.translationService.languageChanged.pipe(startWith(initialLanguage))
    ])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([params, queryParams, language]: [ParamMap, ParamMap, string]) => {
        this.currentLanguage.set(language);
        this.stateFacade.setCurrentLanguage(language);

        const parkId: string | null = params.get('id');
        if (!parkId) {
          return;
        }

        if (parkId !== this.currentParkId) {
          this.currentParkId = parkId;
          this.stateFacade.selectTab('park');
        }

        const filters: PublicVideoFilterState = parsePublicVideoFilters(queryParams);
        const loadKey: string = `${language}|${parkId}|${buildPublicVideoFilterKey(filters)}`;
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

  onTabSelected(tabId: string): void {
    if (tabId === 'park' || tabId === 'items') {
      this.stateFacade.selectTab(tabId as ParkVideosGalleryTab);
    }
  }
}
