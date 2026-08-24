import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, signal } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { of, Subject, timer } from 'rxjs';
import { debounce, skip } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TranslationService } from '@app/services/translation.service';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { ParkListStateFacade } from '../state/park-list-state.facade';
import { ParkListViewComponent } from '../ui/park-list-view.component';
import { SeoService } from '@core/seo/seo.service';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkStatus } from '@app/models/parks/park-status';
import { PUBLIC_PLACE_DISCOVERY_SCOPE_OPTIONS, PublicPlaceDiscoveryScope, PublicSearchCategoryOption } from '@shared/models/search/public-search-category-option.model';

@Component({
  selector: 'app-park-list-page',
  templateUrl: './park-list-page.component.html',
  styleUrls: ['./park-list-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkListStateFacade],
  imports: [ParkListViewComponent]
})
export class ParkListPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly mapState = this.stateFacade.mapState;
  protected readonly parks = this.stateFacade.parks;
  protected readonly searchResults = this.stateFacade.searchResults;
  protected readonly displayedParks = this.stateFacade.displayedParks;
  protected readonly pagination = this.stateFacade.pagination;
  protected readonly visibleMapPoints = this.stateFacade.visibleMapPoints;
  protected readonly visibleCountryCount = this.stateFacade.visibleCountryCount;
  protected readonly selectedMapParkId = this.stateFacade.selectedParkId;
  protected readonly selectedParkCard = this.stateFacade.selectedParkCard;
  protected readonly selectedRegion = this.stateFacade.selectedRegion;
  protected readonly selectedStatus = this.stateFacade.selectedStatus;
  protected readonly selectedAudienceClassificationFilter = this.stateFacade.selectedAudienceClassificationFilter;
  protected readonly discoveryScope = this.stateFacade.discoveryScope;
  protected readonly currentLang = signal<string>('en');
  protected readonly searchTerm = signal<string>('');
  protected readonly discoveryScopeFilterOptions = signal(PUBLIC_PLACE_DISCOVERY_SCOPE_OPTIONS.map((option: PublicSearchCategoryOption) => ({
    labelKey: option.labelKey,
    value: option.value
  })));
  protected readonly statusFilterOptions = signal([
    { labelKey: 'parks.statusFilters.all', value: null },
    { labelKey: 'parks.statuses.operating', value: 'Operating' },
    { labelKey: 'parks.statuses.planned', value: 'Planned' },
    { labelKey: 'parks.statuses.underConstruction', value: 'UnderConstruction' },
    { labelKey: 'parks.statuses.temporarilyClosed', value: 'TemporarilyClosed' },
    { labelKey: 'parks.statuses.closedDefinitively', value: 'ClosedDefinitively' },
    { labelKey: 'parks.statuses.cancelled', value: 'Cancelled' }
  ]);
  protected readonly audienceClassificationFilterOptions = signal([
    { labelKey: 'parks.audienceFilters.all', value: null },
    { labelKey: 'parks.audienceFilters.international', value: 'International' },
    { labelKey: 'parks.audienceFilters.national', value: 'National' },
    { labelKey: 'parks.audienceFilters.regional', value: 'Regional' },
    { labelKey: 'parks.audienceFilters.local', value: 'Local' },
    { labelKey: 'parks.audienceFilters.notSpecified', value: 'Unspecified' }
  ]);

  private readonly searchSubject: Subject<ParkSearchTrigger> = new Subject<ParkSearchTrigger>();
  private activeLanguage: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: ParkListStateFacade,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');

    this.applyLanguage(initialLanguage, false);
    this.watchRouteLanguageChanges();

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string) => {
      this.applyLanguage(language, true);
    });

    this.searchSubject.pipe(
      debounce((trigger: ParkSearchTrigger) => trigger.immediate ? of(0) : timer(300)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((trigger: ParkSearchTrigger) => {
      this.stateFacade.clearSelectedPark();
      this.reloadResults(1, this.stateFacade.pageSize(), trigger.term);
    });

    this.reloadResults(1, this.stateFacade.pageSize(), this.searchTerm());
  }

  private watchRouteLanguageChanges(): void {
    const languageRoute: ActivatedRoute | null = findNearestLanguageActivatedRoute(this.route);

    languageRoute?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((params: ParamMap) => {
      const language: string = resolveLanguageFromParamMap(params, this.currentLang());
      this.applyLanguage(language, true);
    });
  }

  private applyLanguage(language: string, reloadVisibleMapPoints: boolean): void {
    if (this.activeLanguage === language) {
      return;
    }

    this.activeLanguage = language;
    this.currentLang.set(language);
    this.stateFacade.setCurrentLanguage(language);
    this.seoService.applyParkListSeo(language, this.router.url);

    if (reloadVisibleMapPoints) {
      this.reloadResults(this.stateFacade.currentPage(), this.stateFacade.pageSize(), this.searchTerm());
    }
  }

  onSearchInput(value: string): void {
    const normalizedValue: string = value.trim();
    this.searchTerm.set(normalizedValue);
    this.searchSubject.next({ term: normalizedValue, immediate: false });
  }

  onSearchSubmit(): void {
    this.searchSubject.next({ term: this.searchTerm(), immediate: true });
  }

  clearSearch(): void {
    this.searchTerm.set('');
    this.searchSubject.next({ term: '', immediate: false });
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.stateFacade.pageSize();
    this.loadListResults(page, rows, this.searchTerm());
  }

  onRegionFilterChanged(region: ParkRegionFilter | null): void {
    this.stateFacade.setSelectedRegion(region);
    this.stateFacade.clearSelectedPark();
    this.reloadResults(1, this.stateFacade.pageSize(), this.searchTerm());
  }

  onStatusFilterChanged(value: string | null): void {
    const status: ParkStatus | null = normalizeParkStatus(value);

    this.stateFacade.setStatus(status);
    this.stateFacade.clearSelectedPark();
    this.reloadResults(1, this.stateFacade.pageSize(), this.searchTerm());
  }

  onAudienceClassificationFilterChanged(value: string | null): void {
    const audienceClassificationFilter: ParkAudienceClassificationFilter | null = normalizeAudienceClassificationFilter(value);

    this.stateFacade.setAudienceClassificationFilter(audienceClassificationFilter);
    this.stateFacade.clearSelectedPark();
    this.reloadResults(1, this.stateFacade.pageSize(), this.searchTerm());
  }

  onDiscoveryScopeChanged(value: string | null): void {
    const scope: PublicPlaceDiscoveryScope = normalizeDiscoveryScope(value);
    this.stateFacade.setDiscoveryScope(scope);
    this.stateFacade.clearSelectedPark();
    this.reloadResults(1, this.stateFacade.pageSize(), this.searchTerm());
  }

  onMapParkSelected(parkId: string | null): void {
    this.stateFacade.selectParkFromMap(parkId);
  }

  onResultParkFocused(park: ParkCardModel): void {
    this.stateFacade.selectParkFromCard(park);
  }

  clearSelectedPark(): void {
    this.stateFacade.clearSelectedPark();
  }

  private reloadResults(page: number, size: number, term: string): void {
    this.stateFacade.loadVisibleMapPoints(term, this.selectedRegion(), this.discoveryScope());
    this.loadListResults(page, size, term);
  }

  private loadListResults(page: number, size: number, term: string): void {
    const scope: PublicPlaceDiscoveryScope = this.discoveryScope();
    if (scope === 'parks') {
      this.stateFacade.loadParks(page, size, term, this.selectedRegion());
      return;
    }

    this.stateFacade.loadDiscoveryResults(scope, page, size, term, this.selectedRegion());
  }
}

interface ParkSearchTrigger {
  term: string;
  immediate: boolean;
}

function normalizeParkStatus(value: string | null): ParkStatus | null {
  switch (value) {
    case 'Operating':
    case 'Planned':
    case 'UnderConstruction':
    case 'TemporarilyClosed':
    case 'ClosedDefinitively':
    case 'Cancelled':
      return value;
    default:
      return null;
  }
}

function normalizeAudienceClassificationFilter(value: string | null): ParkAudienceClassificationFilter | null {
  switch (value) {
    case 'International':
    case 'National':
    case 'Regional':
    case 'Local':
    case 'Unspecified':
      return value;
    default:
      return null;
  }
}

function normalizeDiscoveryScope(value: string | null): PublicPlaceDiscoveryScope {
  switch (value) {
    case 'parksAndStandaloneAttractions':
    case 'standaloneAttractions':
      return value;
    default:
      return 'parks';
  }
}
