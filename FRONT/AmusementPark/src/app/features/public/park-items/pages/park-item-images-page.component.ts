import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { AdminContextualBlockAppliedEvent, AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events.service';
import {
  buildPublicParkItemImagesRouteCommands,
  buildPublicParkItemRouteCommands,
  buildPublicParkItemsRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkItemImagesStateFacade } from '../state/park-item-images-state.facade';
import { ParkItemImagesViewComponent } from '../ui/park-item-images-view.component';

@Component({
  selector: 'app-park-item-images-page',
  templateUrl: './park-item-images-page.component.html',
  styleUrls: ['./park-item-images-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemImagesStateFacade],
  imports: [ParkItemImagesViewComponent]
})
export class ParkItemImagesPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly item = this.stateFacade.item;
  protected readonly park = this.stateFacade.park;
  protected readonly photos = this.stateFacade.photos;
  protected readonly categories = this.stateFacade.categories;
  protected readonly totalImages = this.stateFacade.totalImages;
  protected readonly canLoadMore = this.stateFacade.canLoadMore;
  protected readonly loadingMore = this.stateFacade.loadingMore;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);
  protected readonly itemsLink = signal<string[] | null>(null);
  protected readonly parkLink = signal<string[] | null>(null);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentItemId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: ParkItemImagesStateFacade,
    private readonly contextualBlockRefreshEvents: AdminContextualBlockRefreshEvents
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

      this.detailLink.set(buildPublicParkItemRouteCommands(routeTarget));
      this.itemsLink.set(buildPublicParkItemsRouteCommands(routeTarget));
      this.parkLink.set(buildPublicParkRouteCommands(routeTarget));
      this.seoService.applyParkItemImagesSeo(
        currentItem,
        currentPark,
        this.currentLanguage(),
        this.router.url,
        this.totalImages(),
        this.photos()[0]?.imageId ?? null,
        buildPublicRoutePath(buildPublicParkItemImagesRouteCommands(routeTarget))
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
      const itemId: string | null = params.get('itemId');
      if (!itemId || itemId === this.currentItemId) {
        return;
      }

      this.currentItemId = itemId;
      this.stateFacade.loadItemImages(itemId);
    });

    this.contextualBlockRefreshEvents.appliedBlock$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event: AdminContextualBlockAppliedEvent) => {
      if (event.blockType === 'parkItem.images' && event.entityId === this.currentItemId && this.currentItemId) {
        this.stateFacade.loadItemImages(this.currentItemId);
      }
    });
  }

  onLoadMoreClicked(): void {
    this.stateFacade.loadNextPage();
  }
}
