import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { combineLatest } from 'rxjs';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import {
  buildPublicParkRouteCommands,
  buildPublicParkVideosRouteCommands
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicVideoBackLink } from '@features/public/videos/ui/public-video-list-view.component';
import { PublicVideoWatchViewComponent } from '@features/public/videos/ui/public-video-watch-view.component';
import { ParkVideoStateFacade } from '../state/park-video-state.facade';

@Component({
  selector: 'app-park-video-page',
  templateUrl: './park-video-page.component.html',
  styleUrls: ['./park-video-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkVideoStateFacade],
  imports: [PublicVideoWatchViewComponent]
})
export class ParkVideoPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
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
    private readonly stateFacade: ParkVideoStateFacade
  ) {
    effect((): void => {
      const currentPark = this.park();
      const currentVideo = this.rawVideo();
      if (!currentPark || !currentVideo) {
        return;
      }

      this.backLinks.set([
        {
          routerLink: buildPublicParkVideosRouteCommands({
            language: this.currentLanguage(),
            parkId: currentPark.id,
            parkName: currentPark.name
          }),
          labelKey: 'videos.watch.backToVideos',
          iconClass: 'pi pi-list',
          variant: 'ghost'
        },
        {
          routerLink: buildPublicParkRouteCommands({
            language: this.currentLanguage(),
            parkId: currentPark.id,
            parkName: currentPark.name
          }),
          labelKey: 'parks.videosPage.backToPark',
          labelParams: { name: currentPark.name },
          iconClass: 'pi pi-arrow-left',
          variant: 'soft'
        }
      ]);
      this.seoService.applyParkVideoSeo(currentVideo, currentPark, this.currentLanguage(), this.router.url, this.parkImageId());
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
        const parkId: string | null = params.get('id');
        const videoId: string | null = params.get('videoId');
        if (!parkId || !videoId) {
          return;
        }

        const loadKey: string = `${parkId}|${videoId}`;
        if (loadKey === this.currentLoadKey) {
          return;
        }

        this.currentLoadKey = loadKey;
        this.stateFacade.loadParkVideo(parkId, videoId);
      });
  }
}
