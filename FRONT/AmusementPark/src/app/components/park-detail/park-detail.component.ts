import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { Park } from '@app/models/parks/park';
import { TranslationService } from '@app/services/translation.service';
import { buildParkSlug } from '@app/commons/park-presentation.utils';
import { ParkDetailStateFacade } from '@features/public/parks/state/park-detail-state.facade';
import { ParkDetailViewComponent } from './park-detail-view.component';

@Component({
  selector: 'app-park-detail',
  templateUrl: './park-detail.component.html',
  styleUrls: ['./park-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkDetailStateFacade],
  imports: [ParkDetailViewComponent]
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
