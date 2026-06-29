import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, PLATFORM_ID, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { buildPublicParkItemRouteCommands, buildPublicRoutePath } from '@shared/utils/routing/public-detail-route.helpers';
import { ParkItemDetailStateFacade } from '../state/park-item-detail-state.facade';
import { ParkItemDetailViewComponent } from '../ui/park-item-detail-view.component';
import { SeoService } from '@core/seo/seo.service';
import { LcpImagePreloadService } from '@core/performance/lcp-image-preload.service';
import { AdminContextualBlockAppliedEvent, AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events.service';

@Component({
  selector: 'app-park-item-detail-page',
  templateUrl: './park-item-detail-page.component.html',
  styleUrls: ['./park-item-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemDetailStateFacade],
  imports: [ParkItemDetailViewComponent]
})
export class ParkItemDetailPageComponent implements OnInit {
  protected readonly heroImageResponsiveWidths: readonly number[] = [320, 480, 640, 800, 960, 1280];
  protected readonly heroImageSizes: string = '(max-width: 900px) 100vw, 900px';
  protected readonly heroImageSrcWidth: number = 960;
  protected readonly state = this.stateFacade.state;
  protected readonly detail = this.stateFacade.detail;
  protected readonly currentLanguage = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly isBrowser: boolean = isPlatformBrowser(inject(PLATFORM_ID));

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkItemDetailStateFacade,
    private readonly seoService: SeoService,
    private readonly lcpImagePreloadService: LcpImagePreloadService,
    private readonly contextualBlockRefreshEvents: AdminContextualBlockRefreshEvents
  ) {
    this.destroyRef.onDestroy((): void => {
      if (this.isBrowser) {
        this.lcpImagePreloadService.clearPreload();
      }
    });

    effect((): void => {
      const currentDetail = this.detail();

      if (!currentDetail) {
        this.lcpImagePreloadService.clearPreload();
        return;
      }

      this.preloadHeroImage(currentDetail.heroPhoto?.imageId ?? null);
      this.seoService.applyParkItemDetailSeo(
        currentDetail,
        this.currentLanguage(),
        this.router.url,
        buildPublicRoutePath(buildPublicParkItemRouteCommands({
          language: this.currentLanguage(),
          parkId: currentDetail.parkId,
          parkName: currentDetail.parkName,
          itemId: currentDetail.id,
          itemName: currentDetail.name
        }))
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

      if (!itemId) {
        return;
      }

      this.stateFacade.loadItem(itemId);
    });

    this.contextualBlockRefreshEvents.appliedBlock$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event: AdminContextualBlockAppliedEvent) => {
      if (event.entityType !== 'ParkItem') {
        return;
      }

      const currentItemId: string | null = this.detail()?.id ?? this.route.snapshot.paramMap.get('itemId');
      if (currentItemId !== event.entityId) {
        return;
      }

      this.stateFacade.loadItem(event.entityId);
    });
  }

  goBackToItems(): void {
    const itemsLink: string[] | null = this.detail()?.itemsLink ?? null;
    const parkLink: string[] | null = this.detail()?.parkLink ?? null;

    if (itemsLink) {
      this.router.navigate(itemsLink);
      return;
    }

    if (parkLink) {
      this.router.navigate(parkLink);
      return;
    }

    this.router.navigate(['/', this.currentLanguage(), 'parks']);
  }

  private preloadHeroImage(heroImageId: string | null): void {
    this.lcpImagePreloadService.preloadImage({
      imageId: heroImageId,
      fallbackWidth: this.heroImageSrcWidth,
      responsiveWidths: this.heroImageResponsiveWidths,
      sizes: this.heroImageSizes
    });
  }
}
