import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { AdminContextualBlockAppliedEvent, AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import {
  buildPublicParkImagesRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { ParkImagesStateFacade } from '../state/park-images-state.facade';
import { ParkImagesViewComponent } from '../ui/park-images-view.component';

@Component({
  selector: 'app-park-images-page',
  templateUrl: './park-images-page.component.html',
  styleUrls: ['./park-images-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkImagesStateFacade],
  imports: [ParkImagesViewComponent]
})
export class ParkImagesPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
  protected readonly photos = this.stateFacade.photos;
  protected readonly categories = this.stateFacade.categories;
  protected readonly activeTab = this.stateFacade.activeTab;
  protected readonly parkTabImageCount = this.stateFacade.parkTabImageCount;
  protected readonly itemTabImageCount = this.stateFacade.itemTabImageCount;
  protected readonly showItemTab = this.stateFacade.showItemTab;
  protected readonly totalImages = this.stateFacade.totalImages;
  protected readonly canLoadMore = this.stateFacade.canLoadMore;
  protected readonly loadingMore = this.stateFacade.loadingMore;
  protected readonly itemImagesLoading = this.stateFacade.itemImagesLoading;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);
  protected readonly itemsLink = signal<string[] | null>(null);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: ParkImagesStateFacade,
    private readonly contextualBlockRefreshEvents: AdminContextualBlockRefreshEvents
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

      this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
      this.itemsLink.set(buildPublicParkItemsRouteCommands(routeTarget));
      this.seoService.applyParkImagesSeo(
        currentPark,
        this.currentLanguage(),
        this.router.url,
        this.totalImages(),
        this.stateFacade.socialImageId(),
        buildPublicRoutePath(buildPublicParkImagesRouteCommands(routeTarget))
      );
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

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const parkId: string | null = params.get('id');
      if (!parkId || parkId === this.currentParkId) {
        return;
      }

      this.currentParkId = parkId;
      this.stateFacade.loadParkImages(parkId);
    });

    this.contextualBlockRefreshEvents.appliedBlock$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event: AdminContextualBlockAppliedEvent) => {
      if (event.blockType === 'park.images' && event.entityId === this.currentParkId && this.currentParkId) {
        this.stateFacade.loadParkImages(this.currentParkId);
      }
    });
  }

  onLoadMoreClicked(): void {
    this.stateFacade.loadNextPage();
  }

  onTabSelected(tab: 'park' | 'items'): void {
    this.stateFacade.selectTab(tab);
  }
}
