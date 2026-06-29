import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import {
  buildPublicParkItemsRouteCommands,
  buildPublicParkMapRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { ParkMapStateFacade } from '../state/park-map-state.facade';
import { ParkMapViewComponent } from '../ui/park-map-view.component';
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';

@Component({
  selector: 'app-park-map-page',
  templateUrl: './park-map-page.component.html',
  styleUrls: ['./park-map-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkMapStateFacade],
  imports: [ParkMapViewComponent]
})
export class ParkMapPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly park = this.stateFacade.park;
  protected readonly parkImageId = this.stateFacade.parkImageId;
  protected readonly map = this.stateFacade.map;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly detailLink = signal<string[] | null>(null);
  protected readonly itemsLink = signal<string[] | null>(null);
  protected readonly selectedClosedFilter = signal<ClosedEntityFilter>(DEFAULT_CLOSED_ENTITY_FILTER);
  protected readonly closedFilterOptions = signal([
    { labelKey: 'parkItems.filters.openOnly', value: DEFAULT_CLOSED_ENTITY_FILTER },
    { labelKey: 'parkItems.filters.withClosed', value: 'all' },
    { labelKey: 'parkItems.filters.closedOnly', value: 'closedOnly' }
  ]);

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private currentParkId: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly stateFacade: ParkMapStateFacade
  ) {
    effect((): void => {
      const currentPark = this.park();
      if (!currentPark) {
        return;
      }

      const routeTarget = {
        language: this.currentLanguage(),
        parkId: currentPark.id,
        parkName: currentPark.name
      };

      this.detailLink.set(buildPublicParkRouteCommands(routeTarget));
      this.itemsLink.set(buildPublicParkItemsRouteCommands(routeTarget));
      this.seoService.applyParkMapSeo(
        currentPark,
        this.currentLanguage(),
        this.router.url,
        this.parkImageId(),
        buildPublicRoutePath(buildPublicParkMapRouteCommands(routeTarget))
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
      const parkId: string | null = params.get('id');
      if (!parkId || parkId === this.currentParkId) {
        return;
      }

      this.currentParkId = parkId;
      this.stateFacade.loadParkMap(parkId, this.selectedClosedFilter());
    });
  }

  onClosedFilterChanged(value: string | null): void {
    const closedFilter: ClosedEntityFilter = normalizeClosedFilter(value);

    this.selectedClosedFilter.set(closedFilter);

    if (this.currentParkId) {
      this.stateFacade.loadParkMap(this.currentParkId, closedFilter);
    }
  }
}

function normalizeClosedFilter(value: string | null): ClosedEntityFilter {
  if (value === 'all' || value === 'closedOnly') {
    return value;
  }

  return DEFAULT_CLOSED_ENTITY_FILTER;
}
