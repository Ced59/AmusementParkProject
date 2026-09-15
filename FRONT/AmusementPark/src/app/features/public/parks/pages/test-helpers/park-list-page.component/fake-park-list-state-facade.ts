import { signal, Signal } from '@angular/core';

import { ScreenState } from '@shared/models/contracts/screen-state.model';

import { ParkMapPointViewModel } from '../../../models/park-map-point-view.model';

import { ParkCardModel } from '@shared/models/parks/park-card.model';

import { PaginationContract } from '@shared/models/contracts';

import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';

import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';

import { ParkStatus } from '@app/models/parks/park-status';

import { PublicPlaceDiscoveryScope } from '@shared/models/search/public-search-category-option.model';

import { SearchResultItem } from '@app/models/search/search-result-item';

export class FakeParkListStateFacade {
  readonly resolvedPage = signal<{ language: string; page: number } | null>(null);

  rejectInvalidPage(): void {}
  readonly state: Signal<ScreenState<unknown, string>> = signal<
    ScreenState<unknown, string>
  >({ kind: 'ready', data: { parks: [], pagination: null } }).asReadonly();
  readonly mapState: Signal<ScreenState<ParkMapPointViewModel[], string>> =
    signal<ScreenState<ParkMapPointViewModel[], string>>({
      kind: 'ready',
      data: [],
    }).asReadonly();
  readonly parks: Signal<ParkCardModel[]> = signal<ParkCardModel[]>(
    [],
  ).asReadonly();
  readonly displayedParks: Signal<ParkCardModel[]> = signal<ParkCardModel[]>(
    [],
  ).asReadonly();
  readonly searchResults: Signal<SearchResultItem[]> = signal<SearchResultItem[]>([]).asReadonly();
  readonly pagination: Signal<PaginationContract | null> =
    signal<PaginationContract | null>(null).asReadonly();
  readonly visibleMapPoints: Signal<ParkMapPointViewModel[]> = signal<
    ParkMapPointViewModel[]
  >([]).asReadonly();
  readonly visibleCountryCount: Signal<number> = signal(0).asReadonly();
  readonly selectedParkId: Signal<string | null> = signal<string | null>(
    null,
  ).asReadonly();
  readonly selectedParkCard: Signal<ParkCardModel | null> =
    signal<ParkCardModel | null>(null).asReadonly();
  readonly selectedRegion: Signal<ParkRegionFilter | null> =
    signal<ParkRegionFilter | null>(null).asReadonly();
  readonly selectedStatus: Signal<ParkStatus | null> = signal<ParkStatus | null>('Operating').asReadonly();
  readonly selectedAudienceClassificationFilter: Signal<ParkAudienceClassificationFilter | null> = signal<ParkAudienceClassificationFilter | null>(null).asReadonly();
  readonly discoveryScopeSignal = signal<PublicPlaceDiscoveryScope>('parks');
  readonly discoveryScope: Signal<PublicPlaceDiscoveryScope> = this.discoveryScopeSignal.asReadonly();
  readonly currentPage: Signal<number> = signal(1).asReadonly();
  readonly pageSize: Signal<number> = signal(9).asReadonly();
  readonly mapLoads: Array<{
    term: string;
    region: ParkRegionFilter | null;
    scope: PublicPlaceDiscoveryScope;
  }> = [];
  readonly parkLoads: Array<{
    page: number;
    size: number;
    term: string;
    region: ParkRegionFilter | null;
  }> = [];
  readonly languages: string[] = [];
  readonly parkMapSelections: Array<string | null> = [];
  readonly discoveryMapSelections: Array<string | null> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadVisibleMapPoints(
    term: string = '',
    region: ParkRegionFilter | null = null,
    scope: PublicPlaceDiscoveryScope = 'parks',
  ): void {
    this.mapLoads.push({ term, region, scope });
  }

  loadParks(
    page: number,
    size: number,
    term: string,
    region: ParkRegionFilter | null,
  ): void {
    this.parkLoads.push({ page, size, term, region });
  }

  clearSelectedPark(): void {}

  setSelectedRegion(): void {}

  setStatus(): void {}

  setAudienceClassificationFilter(): void {}

  setDiscoveryScope(): void {}

  loadDiscoveryResults(): void {}

  selectParkFromMap(parkId: string | null): void {
    this.parkMapSelections.push(parkId);
  }

  selectDiscoveryPointFromMap(pointId: string | null): void {
    this.discoveryMapSelections.push(pointId);
  }

  selectParkFromCard(): void {}
}
