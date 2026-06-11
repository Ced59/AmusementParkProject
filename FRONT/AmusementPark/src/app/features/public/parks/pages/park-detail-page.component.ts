import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkDetailStateFacade } from '../state/park-detail-state.facade';
import { ParkDetailViewComponent } from '../ui/park-detail-view.component';
import { SeoService } from '@core/seo/seo.service';

@Component({
  selector: 'app-park-detail-page',
  templateUrl: './park-detail-page.component.html',
  styleUrls: ['./park-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkDetailStateFacade],
  imports: [ParkDetailViewComponent]
})
export class ParkDetailPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
  protected readonly summary = this.stateFacade.summary;
  protected readonly currentLang = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkDetailStateFacade,
    private readonly seoService: SeoService
  ) {
    effect((): void => {
      const currentPark = this.park();

      if (!currentPark) {
        return;
      }

      this.seoService.applyParkDetailSeo(currentPark, this.currentLang(), this.router.url);
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
}
