import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Park } from '../../models/parks/park';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { NgIf } from '@angular/common';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { ParkHeroSectionComponent } from '../public/park-hero-section/park-hero-section.component';
import { ParkPracticalInfoSectionComponent } from '../public/park-practical-info-section/park-practical-info-section.component';
import { ParkLocationSectionComponent } from '../public/park-location-section/park-location-section.component';
import { ParkNearbySectionComponent } from '../public/park-nearby-section/park-nearby-section.component';
import { TranslateModule } from '@ngx-translate/core';
import { ParkContentSummaryComponent } from '../public/park-content-summary/park-content-summary.component';
import { TranslationService } from '../../services/translation.service';
import { buildParkSlug } from '../../commons/park-presentation.utils';
import { ParkDetailStateFacade } from '@features/public/parks/state/park-detail-state.facade';

@Component({
    selector: 'app-park-detail',
    templateUrl: './park-detail.component.html',
    styleUrls: ['./park-detail.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ParkDetailStateFacade],
    imports: [PageStateComponent, NgIf, Bind, ButtonDirective, ParkHeroSectionComponent, ParkPracticalInfoSectionComponent, ParkLocationSectionComponent, ParkNearbySectionComponent, ParkContentSummaryComponent, TranslateModule]
})
export class ParkDetailComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
  protected readonly explorer = this.stateFacade.explorer;
  protected readonly nearbyParks = this.stateFacade.nearbyParks;
  protected readonly nearbyState = this.stateFacade.nearbyState;
  protected readonly currentLang = signal<string>('en');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly stateFacade: ParkDetailStateFacade
  ) {
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      const id: string | null = params.get('id');

      if (!id) {
        return;
      }

      this.stateFacade.loadPark(id);
    });

    if (this.route.parent) {
      this.route.parent.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
        this.currentLang.set(params.get('lang') ?? 'en');
      });
    }

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string) => {
      this.currentLang.set(lang);
    });
  }

  goBack(): void {
    this.router.navigate([`/${this.currentLang()}/parks`]);
  }

  goToExplore(): void {
    const currentPark: Park | null = this.park();

    if (!currentPark?.id || !currentPark?.name) {
      return;
    }

    this.router.navigate(['/', this.currentLang(), 'park', currentPark.id, buildParkSlug(currentPark.name), 'items']);
  }

  hasPracticalInfo(park: Park | null): boolean {
    return !!park?.countryCode || !!park?.city || !!park?.street || !!park?.postalCode || !!park?.webSiteUrl;
  }

  hasLocationInfo(park: Park | null): boolean {
    return !!park && Number.isFinite(park.latitude) && Number.isFinite(park.longitude);
  }
}
