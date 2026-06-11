import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkItemDetailStateFacade } from '../state/park-item-detail-state.facade';
import { ParkItemDetailViewComponent } from '../ui/park-item-detail-view.component';
import { SeoService } from '@core/seo/seo.service';

@Component({
  selector: 'app-park-item-detail-page',
  templateUrl: './park-item-detail-page.component.html',
  styleUrls: ['./park-item-detail-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkItemDetailStateFacade],
  imports: [ParkItemDetailViewComponent]
})
export class ParkItemDetailPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly detail = this.stateFacade.detail;
  protected readonly currentLanguage = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkItemDetailStateFacade,
    private readonly seoService: SeoService
  ) {
    effect((): void => {
      const currentDetail = this.detail();

      if (!currentDetail) {
        return;
      }

      this.seoService.applyParkItemDetailSeo(currentDetail, this.currentLanguage(), this.router.url);
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
}
