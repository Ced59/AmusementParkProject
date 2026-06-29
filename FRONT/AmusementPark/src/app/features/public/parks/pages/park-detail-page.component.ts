import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, PLATFORM_ID, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { buildPublicParkRouteCommands, buildPublicRoutePath } from '@shared/utils/routing/public-detail-route.helpers';
import { ParkDetailStateFacade } from '../state/park-detail-state.facade';
import { ParkDetailViewComponent } from '../ui/park-detail-view.component';
import { SeoService } from '@core/seo/seo.service';
import { LcpImagePreloadService } from '@core/performance/lcp-image-preload.service';
import { AdminContextualBlockAppliedEvent, AdminContextualBlockRefreshEvents } from '@features/admin/contextual-editing/state/admin-contextual-block-refresh-events.service';

@Component({
  selector: 'app-park-detail-page',
  templateUrl: './park-detail-page.component.html',
  styleUrls: ['./park-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkDetailStateFacade],
  imports: [ParkDetailViewComponent]
})
export class ParkDetailPageComponent implements OnInit {
  protected readonly heroImageResponsiveWidths: readonly number[] = [320, 480, 640, 800, 960, 1280];
  protected readonly heroImageSizes: string = '(max-width: 900px) 100vw, 900px';
  protected readonly heroImageSrcWidth: number = 960;
  protected readonly state = this.stateFacade.state;
  protected readonly nearbyState = this.stateFacade.nearbyState;
  protected readonly weatherState = this.stateFacade.weatherState;
  protected readonly park = this.stateFacade.park;
  protected readonly weather = this.stateFacade.weather;
  protected readonly nearbyParks = this.stateFacade.nearbyParks;
  protected readonly summary = this.stateFacade.summary;
  protected readonly currentLang = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private readonly isBrowser: boolean = isPlatformBrowser(inject(PLATFORM_ID));

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkDetailStateFacade,
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
      const currentPark = this.park();

      if (!currentPark) {
        this.lcpImagePreloadService.clearPreload();
        return;
      }

      this.preloadHeroImage(currentPark.heroImageId);
      this.seoService.applyParkDetailSeo(
        currentPark,
        this.currentLang(),
        this.router.url,
        buildPublicRoutePath(buildPublicParkRouteCommands({
          language: this.currentLang(),
          parkId: currentPark.id,
          parkName: currentPark.name
        }))
      );
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    this.currentLang.set(initialLanguage);
    this.stateFacade.setCurrentLanguage(initialLanguage);

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const id: string | null = params.get('id');

      if (!id) {
        return;
      }

      this.stateFacade.loadPark(id);
    });

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.currentLang.set(language);
      this.stateFacade.setCurrentLanguage(language);
    });

    this.contextualBlockRefreshEvents.appliedBlock$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((event: AdminContextualBlockAppliedEvent) => {
      if (event.entityType !== 'Park') {
        return;
      }

      const currentParkId: string | null = this.park()?.id ?? this.route.snapshot.paramMap.get('id');
      if (currentParkId !== event.entityId) {
        return;
      }

      this.stateFacade.loadPark(event.entityId);
    });
  }

  goBack(): void {
    this.router.navigate([`/${this.currentLang()}/parks`]);
  }

  goToExplore(): void {
    const exploreLink: string[] | null = this.park()?.exploreLink ?? null;

    if (!exploreLink) {
      return;
    }

    this.router.navigate(exploreLink);
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
