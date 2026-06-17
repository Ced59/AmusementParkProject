import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest } from 'rxjs';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import {
  buildPublicParkItemRouteCommands,
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
import { ParkItemVideosStateFacade } from '../state/park-item-videos-state.facade';

@Component({
  selector: 'app-park-item-videos-page',
  templateUrl: './park-item-videos-page.component.html',
  styleUrls: ['./park-item-videos-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemVideosStateFacade],
  imports: [PublicVideoListViewComponent]
})
export class ParkItemVideosPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly item = this.stateFacade.item;
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
    private readonly stateFacade: ParkItemVideosStateFacade
  ) {
    effect((): void => {
      const currentItem = this.item();
      const currentPark = this.park();
      if (!currentItem || !currentPark) {
        return;
      }

      const routeTarget = {
        language: this.currentLanguage(),
        parkId: currentPark.id,
        parkName: currentPark.name,
        itemId: currentItem.id,
        itemName: currentItem.name
      };

      this.titleParams.set({
        name: currentItem.name,
        park: currentPark.name,
        count: this.totalVideos()
      });
      this.backLinks.set([
        {
          routerLink: buildPublicParkItemRouteCommands(routeTarget),
          labelKey: 'parkItems.videosPage.backToItem',
          labelParams: { name: currentItem.name },
          iconClass: 'pi pi-arrow-left',
          variant: 'ghost'
        },
        {
          routerLink: buildPublicParkItemsRouteCommands(routeTarget),
          labelKey: 'parkItems.videosPage.backToItems',
          labelParams: { name: currentPark.name },
          iconClass: 'pi pi-sitemap',
          variant: 'primary'
        },
        {
          routerLink: buildPublicParkRouteCommands(routeTarget),
          labelKey: 'parkItems.videosPage.backToPark',
          labelParams: { name: currentPark.name },
          iconClass: 'pi pi-map',
          variant: 'soft'
        }
      ]);
      this.seoService.applyParkItemVideosSeo(currentItem, currentPark, this.currentLanguage(), this.router.url, this.totalVideos());
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
        const itemId: string | null = params.get('itemId');
        if (!itemId) {
          return;
        }

        const filters: PublicVideoFilterState = parsePublicVideoFilters(queryParams);
        const loadKey: string = `${itemId}|${buildPublicVideoFilterKey(filters)}`;
        if (loadKey === this.currentLoadKey) {
          return;
        }

        this.currentLoadKey = loadKey;
        this.stateFacade.loadItemVideos(itemId, filters);
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
