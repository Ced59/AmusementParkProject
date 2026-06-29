import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest } from 'rxjs';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import {
  buildPublicParkItemVideoRouteCommands,
  buildPublicParkItemRouteCommands,
  buildPublicParkItemVideosRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicVideoBackLink } from '@features/public/videos/ui/public-video-list-view.component';
import { PublicVideoWatchViewComponent } from '@features/public/videos/ui/public-video-watch-view.component';
import { ParkItemVideoStateFacade } from '../state/park-item-video-state.facade';

@Component({
  selector: 'app-park-item-video-page',
  templateUrl: './park-item-video-page.component.html',
  styleUrls: ['./park-item-video-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemVideoStateFacade],
  imports: [PublicVideoWatchViewComponent]
})
export class ParkItemVideoPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly item = this.stateFacade.item;
  protected readonly park = this.stateFacade.park;
  protected readonly itemImageId = this.stateFacade.itemImageId;
  protected readonly parkImageId = this.stateFacade.parkImageId;
  protected readonly video = this.stateFacade.video;
  protected readonly rawVideo = this.stateFacade.rawVideo;
  protected readonly previousVideo = this.stateFacade.previousVideo;
  protected readonly nextVideo = this.stateFacade.nextVideo;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly backLinks = signal<PublicVideoBackLink[]>([]);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentLoadKey: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: ParkItemVideoStateFacade
  ) {
    effect((): void => {
      const currentItem = this.item();
      const currentPark = this.park();
      const currentVideo = this.rawVideo();
      if (!currentItem || !currentPark || !currentVideo) {
        return;
      }

      const routeTarget = {
        language: this.currentLanguage(),
        parkId: currentPark.id,
        parkName: currentPark.name,
        itemId: currentItem.id,
        itemName: currentItem.name
      };

      this.backLinks.set([
        {
          routerLink: buildPublicParkItemVideosRouteCommands(routeTarget),
          labelKey: 'videos.watch.backToVideos',
          iconClass: 'pi pi-list',
          variant: 'ghost'
        },
        {
          routerLink: buildPublicParkItemRouteCommands(routeTarget),
          labelKey: 'parkItems.videosPage.backToItem',
          labelParams: { name: currentItem.name },
          iconClass: 'pi pi-arrow-left',
          variant: 'soft'
        },
        {
          routerLink: buildPublicParkRouteCommands(routeTarget),
          labelKey: 'parkItems.videosPage.backToPark',
          labelParams: { name: currentPark.name },
          iconClass: 'pi pi-map',
          variant: 'soft'
        }
      ]);
      this.seoService.applyParkItemVideoSeo(
        currentVideo,
        currentItem,
        currentPark,
        this.currentLanguage(),
        this.router.url,
        this.itemImageId(),
        this.parkImageId(),
        buildPublicRoutePath(buildPublicParkItemVideoRouteCommands({
          ...routeTarget,
          videoId: currentVideo.id,
          videoTitle: currentVideo.title
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

    combineLatest([this.route.paramMap, this.route.queryParamMap])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(([params]: [ParamMap, ParamMap]) => {
        const itemId: string | null = params.get('itemId');
        const videoId: string | null = params.get('videoId');
        if (!itemId || !videoId) {
          return;
        }

        const loadKey: string = `${itemId}|${videoId}`;
        if (loadKey === this.currentLoadKey) {
          return;
        }

        this.currentLoadKey = loadKey;
        this.stateFacade.loadItemVideo(itemId, videoId);
      });
  }
}
