import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { PaginationContract } from '@shared/models/contracts';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { mapArray, mapCollectionResponse, mapParkToCardModel } from '@shared/utils/mapping';
import { Park } from '@app/models/parks/park';
import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { mapParkMapPointToViewModel } from '../mappers/park-map-point-view.mapper';
import { ParkStatus } from '@app/models/parks/park-status';
import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { StandaloneAttractionMapPoint } from '@app/models/standalone-attractions/standalone-attraction-map-point';
import { PublicPlaceDiscoveryScope } from '@shared/models/search/public-search-category-option.model';
import { mapStandaloneAttractionMapPointToViewModel } from '../mappers/standalone-attraction-map-point-view.mapper';

import {
  PARK_LIST_STATE_PARKS_API_SERVICE_PORT,
  ParkListStateParksApiServicePort,
  PARK_LIST_STATE_SEARCH_API_SERVICE_PORT,
  ParkListStateSearchApiServicePort,
  PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT,
  ParkListStateStandaloneAttractionsApiServicePort
} from './park-list-state-data.ports';
interface ParkListSourceData {
  parks: Park[];
  searchResults: SearchResultItem[];
  pagination: PaginationContract | null;
}

@Injectable()
export class ParkListStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkListSourceData>();
  private readonly mapStateStore = new SignalScreenStateStore<ParkMapPointViewModel[]>();
  private readonly currentLanguageSignal = signal('en');
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(9);
  private readonly selectedParkIdSignal = signal<string | null>(null);
  private readonly selectedParkCardSignal = signal<ParkCardModel | null>(null);
  private readonly selectedRegionSignal = signal<ParkRegionFilter | null>(null);
  private readonly selectedStatusSignal = signal<ParkStatus | null>('Operating');
  private readonly selectedAudienceClassificationFilterSignal = signal<ParkAudienceClassificationFilter | null>(null);
  private readonly discoveryScopeSignal = signal<PublicPlaceDiscoveryScope>('parks');
  private screenRequestGeneration = 0;
  private mapRequestGeneration = 0;

  public readonly state = this.screenStateStore.state;
  public readonly mapState = this.mapStateStore.state;
  public readonly parks: Signal<ParkCardModel[]> = computed(() => {
    return mapArray(this.screenStateStore.data()?.parks, (park: Park) =>
      mapParkToCardModel(park, this.currentLanguageSignal(), this.countryDisplayService, this.textTruncator));
  });
  public readonly searchResults: Signal<SearchResultItem[]> = computed(() => this.screenStateStore.data()?.searchResults ?? []);
  public readonly displayedParks: Signal<ParkCardModel[]> = computed(() => {
    const selectedPark: ParkCardModel | null = this.selectedParkCardSignal();

    if (selectedPark) {
      return [selectedPark];
    }

    return this.parks();
  });
  public readonly pagination: Signal<PaginationContract | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly visibleMapPoints: Signal<ParkMapPointViewModel[]> = computed(() => this.mapStateStore.data() ?? []);
  public readonly visibleCountryCount: Signal<number> = computed(() => {
    const countryCodes: Set<string> = new Set<string>();

    for (const point of this.visibleMapPoints()) {
      if (point.countryCode) {
        countryCodes.add(point.countryCode);
      }
    }

    return countryCodes.size;
  });
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly selectedParkId = this.selectedParkIdSignal.asReadonly();
  public readonly selectedParkCard = this.selectedParkCardSignal.asReadonly();
  public readonly selectedRegion = this.selectedRegionSignal.asReadonly();
  public readonly selectedStatus = this.selectedStatusSignal.asReadonly();
  public readonly selectedAudienceClassificationFilter = this.selectedAudienceClassificationFilterSignal.asReadonly();
  public readonly discoveryScope = this.discoveryScopeSignal.asReadonly();

  constructor(
    @Inject(PARK_LIST_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: ParkListStateParksApiServicePort,
    @Inject(PARK_LIST_STATE_SEARCH_API_SERVICE_PORT) private readonly searchApiService: ParkListStateSearchApiServicePort,
    @Inject(PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT) private readonly standaloneAttractionsApiService: ParkListStateStandaloneAttractionsApiServicePort,
    private readonly countryDisplayService: CountryDisplayService,
    private readonly textTruncator: NaturalTextTruncatorService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  setSelectedRegion(region: ParkRegionFilter | null): void {
    this.selectedRegionSignal.set(region);
  }

  setStatus(status: ParkStatus | null): void {
    this.selectedStatusSignal.set(status);
  }

  setAudienceClassificationFilter(audienceClassificationFilter: ParkAudienceClassificationFilter | null): void {
    this.selectedAudienceClassificationFilterSignal.set(audienceClassificationFilter);
  }

  setDiscoveryScope(scope: PublicPlaceDiscoveryScope): void {
    this.discoveryScopeSignal.set(scope);
  }

  clearSelectedPark(): void {
    this.selectedParkIdSignal.set(null);
    this.selectedParkCardSignal.set(null);
  }

  selectParkFromCard(park: ParkCardModel): void {
    const parkId: string | null = park.id?.trim() || null;

    if (!parkId) {
      this.clearSelectedPark();
      return;
    }

    this.selectedParkIdSignal.set(parkId);
    this.selectedParkCardSignal.set(park);
  }

  selectParkFromMap(parkId: string | null): void {
    const normalizedParkId: string | null = parkId?.trim() || null;

    if (!normalizedParkId) {
      this.clearSelectedPark();
      return;
    }

    this.selectedParkIdSignal.set(normalizedParkId);

    const alreadyLoadedPark: ParkCardModel | undefined = this.parks().find((park: ParkCardModel) => park.id === normalizedParkId);

    if (alreadyLoadedPark) {
      this.selectedParkCardSignal.set(alreadyLoadedPark);
      return;
    }

    const currentMapPoint: ParkMapPointViewModel | undefined = this.visibleMapPoints().find((point: ParkMapPointViewModel) => point.id === normalizedParkId);

    if (currentMapPoint?.kind === 'standaloneAttraction') {
      this.selectedParkCardSignal.set(null);
      return;
    }

    if (currentMapPoint) {
      this.selectedParkCardSignal.set(this.mapPointToCardModel(currentMapPoint));
    }

    this.parksApiService.getParkById(normalizedParkId, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (park: Park) => {
          if (this.selectedParkIdSignal() !== normalizedParkId) {
            return;
          }

          const selectedPark: ParkCardModel = mapParkToCardModel(park, this.currentLanguageSignal(), this.countryDisplayService, this.textTruncator);
          this.selectedParkCardSignal.set(selectedPark);
        },
        error: (error: unknown) => {
          if (this.selectedParkIdSignal() !== normalizedParkId) {
            return;
          }

          console.error('Error fetching selected park:', error);
        }
      });
  }

  loadParks(page: number, size: number, term: string, region: ParkRegionFilter | null): void {
    const normalizedTerm: string = term.trim();
    const previousData: ParkListSourceData | undefined = this.screenStateStore.data();
    const filters: ParkAdminListFilters | null = this.buildAudienceClassificationFilters();
    const requestGeneration: number = ++this.screenRequestGeneration;

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const request$ = normalizedTerm
      ? this.parksApiService.searchParks(normalizedTerm, page, size, true, region, filters, this.buildPublicOptions())
      : this.parksApiService.getParksPaginated(page, size, true, region, filters, this.buildPublicOptions());

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: ParksApiResponse) => {
        if (requestGeneration !== this.screenRequestGeneration) {
          return;
        }

        const pagedResult = mapCollectionResponse(response, (park: Park) => park);
        const sourceData: ParkListSourceData = {
          parks: pagedResult.items,
          searchResults: [],
          pagination: pagedResult.pagination,
        };

        if (pagedResult.items.length === 0) {
          this.screenStateStore.setEmpty(sourceData);
          return;
        }

        this.screenStateStore.setReady(sourceData);
      },
      error: (error: unknown) => {
        if (requestGeneration !== this.screenRequestGeneration) {
          return;
        }

        console.error('Error fetching parks:', error);
        this.screenStateStore.setError('parks.errorMessage', previousData);
      }
    });
  }

  loadDiscoveryResults(
    scope: Exclude<PublicPlaceDiscoveryScope, 'parks'>,
    page: number,
    size: number,
    term: string,
    region: ParkRegionFilter | null
  ): void {
    const previousData: ParkListSourceData | undefined = this.screenStateStore.data();
    const categories: string[] = scope === 'standaloneAttractions'
      ? ['standaloneAttractions']
      : ['park', 'standaloneAttractions'];
    const requestGeneration: number = ++this.screenRequestGeneration;

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    this.searchApiService.getSearch(term.trim(), categories, page, size, anonymousHttpOptions(), region)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: SearchApiResponse) => {
          if (requestGeneration !== this.screenRequestGeneration) {
            return;
          }

          const sourceData: ParkListSourceData = {
            parks: [],
            searchResults: response.data ?? [],
            pagination: response.pagination ?? null
          };

          if (sourceData.searchResults.length === 0) {
            this.screenStateStore.setEmpty(sourceData);
            return;
          }

          this.screenStateStore.setReady(sourceData);
        },
        error: (error: unknown) => {
          if (requestGeneration !== this.screenRequestGeneration) {
            return;
          }

          console.error('Error fetching public places:', error);
          this.screenStateStore.setError('parks.errorMessage', previousData);
        }
      });
  }

  loadVisibleMapPoints(
    term: string = '',
    region: ParkRegionFilter | null = null,
    scope: PublicPlaceDiscoveryScope = this.discoveryScopeSignal()
  ): void {
    const previousData: ParkMapPointViewModel[] | undefined = this.mapStateStore.data();
    const requestGeneration: number = ++this.mapRequestGeneration;
    this.mapStateStore.setLoading(previousData);

    this.buildMapPointsRequest(term, region, scope)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (viewModels: ParkMapPointViewModel[]) => {
          if (requestGeneration !== this.mapRequestGeneration) {
            return;
          }

          if (viewModels.length === 0) {
            this.mapStateStore.setEmpty([]);
            return;
          }

          this.mapStateStore.setReady(viewModels);
        },
        error: (error: unknown) => {
          if (requestGeneration !== this.mapRequestGeneration) {
            return;
          }

          console.error('Error fetching visible park map points:', error);
          this.mapStateStore.setError('parks.map.errorMessage', previousData);
        }
      });
  }

  private buildMapPointsRequest(
    term: string,
    region: ParkRegionFilter | null,
    scope: PublicPlaceDiscoveryScope
  ): Observable<ParkMapPointViewModel[]> {
    const parkRequest: Observable<ParkMapPointViewModel[]> = this.parksApiService.getVisibleParkMapPoints(term, region, {
      ...anonymousHttpOptions(),
      closedFilter: scope === 'parks' && this.selectedStatusSignal() !== null ? 'openOnly' : 'all',
      status: scope === 'parks' ? this.selectedStatusSignal() ?? undefined : undefined,
      audienceClassificationFilter: scope === 'parks' ? this.selectedAudienceClassificationFilterSignal() : null
    }).pipe(map((points: ParkMapPoint[]) => points
      .map((point: ParkMapPoint) => mapParkMapPointToViewModel(point, this.currentLanguageSignal(), this.countryDisplayService))
      .filter((point: ParkMapPointViewModel | null): point is ParkMapPointViewModel => point !== null)));

    const standaloneRequest: Observable<ParkMapPointViewModel[]> = this.standaloneAttractionsApiService.getVisibleMapPoints(term, region, anonymousHttpOptions())
      .pipe(map((points: StandaloneAttractionMapPoint[]) => points
        .map((point: StandaloneAttractionMapPoint) => mapStandaloneAttractionMapPointToViewModel(point, this.currentLanguageSignal(), this.countryDisplayService))
        .filter((point: ParkMapPointViewModel | null): point is ParkMapPointViewModel => point !== null)));

    if (scope === 'parks') {
      return parkRequest;
    }

    if (scope === 'standaloneAttractions') {
      return standaloneRequest;
    }

    return forkJoin([parkRequest, standaloneRequest]).pipe(
      map(([parkPoints, standalonePoints]: [ParkMapPointViewModel[], ParkMapPointViewModel[]]) => [...parkPoints, ...standalonePoints])
    );
  }

  private mapPointToCardModel(point: ParkMapPointViewModel): ParkCardModel {
    return mapParkToCardModel({
      id: point.id,
      name: point.name,
      countryCode: point.countryCode ?? undefined,
      city: point.city ?? undefined,
      street: point.street ?? undefined,
      postalCode: point.postalCode ?? undefined,
      status: (point.status as ParkStatus | null) ?? undefined,
      latitude: point.latitude,
      longitude: point.longitude,
      currentLogoImageId: point.logoImageId
    }, this.currentLanguageSignal(), this.countryDisplayService, this.textTruncator);
  }

  private buildPublicOptions(): ReturnType<typeof anonymousHttpOptions> & { closedFilter: 'openOnly' | 'all'; status: ParkStatus | null } {
    const status: ParkStatus | null = this.selectedStatusSignal();
    return {
      ...anonymousHttpOptions(),
      closedFilter: status === null ? 'all' : 'openOnly',
      status,
    };
  }

  private buildAudienceClassificationFilters(): ParkAdminListFilters | null {
    const audienceClassification: ParkAudienceClassificationFilter | null = this.selectedAudienceClassificationFilterSignal();
    return audienceClassification
      ? { audienceClassification }
      : null;
  }
}
