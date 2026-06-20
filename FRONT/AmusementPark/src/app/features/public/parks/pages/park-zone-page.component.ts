import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { ParkZonesPageStateFacade } from '../state/park-zones-page-state.facade';
import { ParkZoneViewComponent } from '../ui/park-zone-view.component';

@Component({
  selector: 'app-park-zone-page',
  templateUrl: './park-zone-page.component.html',
  styleUrls: ['./park-zone-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkZonesPageStateFacade],
  imports: [ParkZoneViewComponent]
})
export class ParkZonePageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly page = this.stateFacade.zonePage;
  protected readonly parkImageId = this.stateFacade.parkImageId;
  protected readonly currentLanguage = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkZonesPageStateFacade,
    private readonly seoService: SeoService
  ) {
    effect((): void => {
      const currentPage = this.page();

      if (!currentPage) {
        return;
      }

      this.seoService.applyParkZoneSeo(currentPage.parkName, currentPage.zoneName, this.currentLanguage(), this.router.url, this.parkImageId());
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
      const zoneId: string | null = params.get('zoneId');

      this.stateFacade.setSelectedZone(zoneId);

      if (!parkId || parkId === this.currentParkId) {
        return;
      }

      this.currentParkId = parkId;
      this.stateFacade.loadData(parkId, this.currentLanguage());
    });
  }

  onBackClicked(): void {
    const zonesLink: string[] | null = this.page()?.zonesLink ?? null;

    if (zonesLink) {
      this.router.navigate(zonesLink);
      return;
    }

    this.router.navigate(['/', this.currentLanguage(), 'parks']);
  }
}
