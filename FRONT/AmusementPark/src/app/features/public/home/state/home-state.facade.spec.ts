import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { Park } from '@app/models/parks/park';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { Pagination } from '@app/models/shared/pagination';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import {
  HOME_STATE_HOME_API_SERVICE_PORT,
  HOME_STATE_PARKS_API_SERVICE_PORT,
  HOME_STATE_SEARCH_API_SERVICE_PORT,
  HomeStateHomeApiServicePort,
  HomeStateParksApiServicePort,
  HomeStateSearchApiServicePort
} from './home-state-data.ports';
import { HomeStateFacade } from './home-state.facade';

class FakeSearchPort implements HomeStateSearchApiServicePort {
  public response$: Observable<SearchApiResponse> = of(createSearchResponse([createSearchResult()], createPagination(1, 10, 1)));
  public readonly calls: { term: string; categories: string[]; page: number; size: number }[] = [];

  getSearch(term: string, categories: string[], page: number, size: number): Observable<SearchApiResponse> {
    this.calls.push({ term, categories, page, size });
    return this.response$;
  }
}

class FakeParksPort implements HomeStateParksApiServicePort {
  public response$: Observable<Park[]> = of([createPark('park-1')]);
  public readonly calls: number[] = [];

  getRandomVisibleParks(limit: number): Observable<Park[]> {
    this.calls.push(limit);
    return this.response$;
  }
}

class FakeHomePort implements HomeStateHomeApiServicePort {
  public statsResponse$: Observable<HomeStatsModel> = of({ parksCount: 10, attractionsCount: 40, countriesCount: 3 });
  public featuredResponse$: Observable<HomeFeaturedParkModel[]> = of([createFeaturedPark('park-2')]);
  public readonly statsCalls: number[] = [];
  public readonly featuredCalls: { excludedParkIds: readonly string[]; limit: number }[] = [];

  getHomeStats(): Observable<HomeStatsModel> {
    this.statsCalls.push(1);
    return this.statsResponse$;
  }

  getFeaturedParks(excludedParkIds: readonly string[], limit: number): Observable<HomeFeaturedParkModel[]> {
    this.featuredCalls.push({ excludedParkIds, limit });
    return this.featuredResponse$;
  }
}

function createPark(id: string): Park {
  return {
    id,
    name: id,
    countryCode: 'FR',
    latitude: 48.8,
    longitude: 2.3,
    isVisible: true,
    descriptions: [{ languageCode: 'en', value: '<p>Park description.</p>' }]
  };
}

function createFeaturedPark(id: string): HomeFeaturedParkModel {
  return {
    id,
    name: id,
    countryCode: 'FR',
    type: 'ThemePark',
    latitude: 48.8,
    longitude: 2.3,
    descriptions: [{ languageCode: 'en', value: '<p>Featured park.</p>' }],
    city: 'Paris',
    currentLogoImageId: null,
    isManualFeatured: true,
    isSponsoredFeatured: false,
    countsByCategory: []
  };
}

function createSearchResult(): SearchResultItem {
  return {
    originalId: 'park-1',
    category: 'Park',
    title: 'Parc de test',
    description: 'Description de test'
  };
}

function createPagination(currentPage: number, itemsPerPage: number, totalItems: number): Pagination {
  return {
    currentPage,
    itemsPerPage,
    totalItems,
    totalPages: Math.ceil(totalItems / itemsPerPage)
  };
}

function createSearchResponse(data: SearchResultItem[], pagination: Pagination): SearchApiResponse {
  return { data, pagination };
}

describe('HomeStateFacade', () => {
  let facade: HomeStateFacade;
  let searchPort: FakeSearchPort;
  let parksPort: FakeParksPort;
  let homePort: FakeHomePort;

  beforeEach(() => {
    searchPort = new FakeSearchPort();
    parksPort = new FakeParksPort();
    homePort = new FakeHomePort();

    TestBed.configureTestingModule({
      providers: [
        HomeStateFacade,
        NaturalTextTruncatorService,
        CountryDisplayService,
        { provide: HOME_STATE_SEARCH_API_SERVICE_PORT, useValue: searchPort },
        { provide: HOME_STATE_PARKS_API_SERVICE_PORT, useValue: parksPort },
        { provide: HOME_STATE_HOME_API_SERVICE_PORT, useValue: homePort }
      ]
    });

    facade = TestBed.inject(HomeStateFacade);
  });

  it('loads home stats from the home port', () => {
    facade.loadHomeStats();

    expect(homePort.statsCalls).toEqual([1]);
    expect(facade.homeStats()).toEqual({ parksCount: 10, attractionsCount: 40, countriesCount: 3 });
    expect(facade.statsState().kind).toBe('ready');
  });

  it('loads hero parks and then featured parks with hero ids excluded', () => {
    facade.loadFeaturedParks('en');

    expect(parksPort.calls).toEqual([4]);
    expect(facade.heroParks().map((park) => park.id)).toEqual(['park-1']);
    expect(homePort.featuredCalls).toEqual([{ excludedParkIds: ['park-1'], limit: 3 }]);
    expect(facade.featuredParks().map((park) => park.id)).toEqual(['park-2']);
  });

  it('still loads featured parks when hero loading fails', () => {
    parksPort.response$ = throwError(() => new Error('network'));

    facade.loadFeaturedParks('en');

    expect(facade.heroParksState().kind).toBe('error');
    expect(homePort.featuredCalls).toEqual([{ excludedParkIds: [], limit: 3 }]);
  });

  it('searches with a trimmed term and selected category', () => {
    facade.search(' taron ', 'Attraction', 2, 20);

    expect(searchPort.calls).toEqual([{ term: 'taron', categories: ['Attraction'], page: 2, size: 20 }]);
    expect(facade.hasPerformedSearch()).toBeTrue();
    expect(facade.searchResults().map((result: SearchResultItem) => result.originalId)).toEqual(['park-1']);
  });

  it('passes the attractions with standalone category to the search port', () => {
    facade.search('', 'attractionsWithStandalone', 1, 10);

    expect(searchPort.calls).toEqual([{ term: '', categories: ['attractionsWithStandalone'], page: 1, size: 10 }]);
    expect(facade.hasPerformedSearch()).toBeTrue();
  });

  it('clears search without calling the search port when no criteria are provided', () => {
    facade.search('   ', '', 3, 10);

    expect(searchPort.calls).toEqual([]);
    expect(facade.hasPerformedSearch()).toBeFalse();
    expect(facade.currentPage()).toBe(1);
  });
});
