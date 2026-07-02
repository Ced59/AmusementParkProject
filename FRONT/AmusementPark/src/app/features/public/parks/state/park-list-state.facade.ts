import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
import { ClosedEntityFilter, DEFAULT_CLOSED_ENTITY_FILTER } from '@app/models/shared/closed-entity-filter';
import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';

import {
  PARK_LIST_STATE_PARKS_API_SERVICE_PORT,
  ParkListStateParksApiServicePort
} from './park-list-state-data.ports';
interface ParkListSourceData {
  parks: Park[];
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
  private readonly selectedClosedFilterSignal = signal<ClosedEntityFilter>(DEFAULT_CLOSED_ENTITY_FILTER);
  private readonly selectedAudienceClassificationFilterSignal = signal<ParkAudienceClassificationFilter | null>(null);

  public readonly state = this.screenStateStore.state;
  public readonly mapState = this.mapStateStore.state;
  public readonly parks: Signal<ParkCardModel[]> = computed(() => {
    return mapArray(this.screenStateStore.data()?.parks, (park: Park) =>
      mapParkToCardModel(park, this.currentLanguageSignal(), this.countryDisplayService, this.textTruncator));
  });
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
  public readonly selectedClosedFilter = this.selectedClosedFilterSignal.asReadonly();
  public readonly selectedAudienceClassificationFilter = this.selectedAudienceClassificationFilterSignal.asReadonly();

  constructor(
    @Inject(PARK_LIST_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: ParkListStateParksApiServicePort,
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

  setClosedFilter(closedFilter: ClosedEntityFilter): void {
    this.selectedClosedFilterSignal.set(closedFilter);
  }

  setAudienceClassificationFilter(audienceClassificationFilter: ParkAudienceClassificationFilter | null): void {
    this.selectedAudienceClassificationFilterSignal.set(audienceClassificationFilter);
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

    if (currentMapPoint) {
      this.selectedParkCardSignal.set(this.mapPointToCardModel(currentMapPoint));
    }

    this.parksApiService.getParkById(normalizedParkId, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (park: Park) => {
          const selectedPark: ParkCardModel = mapParkToCardModel(park, this.currentLanguageSignal(), this.countryDisplayService, this.textTruncator);
          this.selectedParkCardSignal.set(selectedPark);
        },
        error: (error: unknown) => {
          console.error('Error fetching selected park:', error);
        }
      });
  }

  loadParks(page: number, size: number, term: string, region: ParkRegionFilter | null): void {
    const normalizedTerm: string = term.trim();
    const previousData: ParkListSourceData | undefined = this.screenStateStore.data();
    const filters: ParkAdminListFilters | null = this.buildAudienceClassificationFilters();

    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    const request$ = normalizedTerm
      ? this.parksApiService.searchParks(normalizedTerm, page, size, true, region, filters, { ...anonymousHttpOptions(), closedFilter: this.selectedClosedFilterSignal() })
      : this.parksApiService.getParksPaginated(page, size, true, region, filters, { ...anonymousHttpOptions(), closedFilter: this.selectedClosedFilterSignal() });

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: ParksApiResponse) => {
        const pagedResult = mapCollectionResponse(response, (park: Park) => park);
        const sourceData: ParkListSourceData = {
          parks: pagedResult.items,
          pagination: pagedResult.pagination,
        };

        if (pagedResult.items.length === 0) {
          this.screenStateStore.setEmpty(sourceData);
          return;
        }

        this.screenStateStore.setReady(sourceData);
      },
      error: (error: unknown) => {
        console.error('Error fetching parks:', error);
        this.screenStateStore.setError('parks.errorMessage', previousData);
      }
    });
  }

  loadVisibleMapPoints(term: string = '', region: ParkRegionFilter | null = null): void {
    const previousData: ParkMapPointViewModel[] | undefined = this.mapStateStore.data();
    this.mapStateStore.setLoading(previousData);

    this.parksApiService.getVisibleParkMapPoints(term, region, {
      ...anonymousHttpOptions(),
      closedFilter: this.selectedClosedFilterSignal(),
      audienceClassificationFilter: this.selectedAudienceClassificationFilterSignal()
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (points: ParkMapPoint[]) => {
          const viewModels: ParkMapPointViewModel[] = points
            .map((point: ParkMapPoint) => mapParkMapPointToViewModel(point, this.currentLanguageSignal(), this.countryDisplayService))
            .filter((point: ParkMapPointViewModel | null): point is ParkMapPointViewModel => point !== null);

          if (viewModels.length === 0) {
            this.mapStateStore.setEmpty([]);
            return;
          }

          this.mapStateStore.setReady(viewModels);
        },
        error: (error: unknown) => {
          console.error('Error fetching visible park map points:', error);
          this.mapStateStore.setError('parks.map.errorMessage', previousData);
        }
      });
  }

  private mapPointToCardModel(point: ParkMapPointViewModel): ParkCardModel {
    return {
      id: point.id,
      name: point.name,
      countryCode: point.countryCode,
      city: point.city,
      status: null,
      latitude: point.latitude,
      longitude: point.longitude,
      logoImageId: point.logoImageId,
      websiteUrl: null,
      locationLine: point.locationLine,
      addressLine: point.addressLine,
      coordinatesLine: point.coordinatesLine,
      shortDescription: null,
      isClosedDefinitively: false
    };
  }

  private buildAudienceClassificationFilters(): ParkAdminListFilters | null {
    const audienceClassification: ParkAudienceClassificationFilter | null = this.selectedAudienceClassificationFilterSignal();
    return audienceClassification
      ? { audienceClassification }
      : null;
  }
}
