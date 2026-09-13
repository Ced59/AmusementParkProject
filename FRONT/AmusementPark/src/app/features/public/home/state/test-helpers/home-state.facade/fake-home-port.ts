import { Observable, of } from 'rxjs';

import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';

import { HomeStatsModel } from '@app/models/home/home-stats.model';

import { HomeStateHomeApiServicePort } from '../../home-state-data.ports';

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
    countsByCategory: [],
  };
}

export class FakeHomePort implements HomeStateHomeApiServicePort {
  public statsResponse$: Observable<HomeStatsModel> = of({
    parksCount: 10,
    attractionsCount: 40,
    countriesCount: 3,
  });
  public featuredResponse$: Observable<HomeFeaturedParkModel[]> = of([
    createFeaturedPark('park-2'),
  ]);
  public readonly statsCalls: number[] = [];
  public readonly featuredCalls: {
    excludedParkIds: readonly string[];
    limit: number;
  }[] = [];

  getHomeStats(): Observable<HomeStatsModel> {
    this.statsCalls.push(1);
    return this.statsResponse$;
  }

  getFeaturedParks(
    excludedParkIds: readonly string[],
    limit: number,
  ): Observable<HomeFeaturedParkModel[]> {
    this.featuredCalls.push({ excludedParkIds, limit });
    return this.featuredResponse$;
  }
}
