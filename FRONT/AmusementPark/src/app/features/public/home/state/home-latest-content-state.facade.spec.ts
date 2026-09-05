import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { HomeLatestArticleModel } from '@app/models/home/home-latest-article.model';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { HOME_LATEST_CONTENT_DATA_PORT, HomeLatestContentDataPort } from './home-latest-content-data.ports';
import { HomeLatestContentStateFacade } from './home-latest-content-state.facade';

describe('HomeLatestContentStateFacade', () => {
  let facade: HomeLatestContentStateFacade;
  let dataPort: HomeLatestContentDataPort;
  let parkLimits: number[];
  let articleLimits: number[];

  beforeEach(() => {
    parkLimits = [];
    articleLimits = [];
    dataPort = {
      getLatestParks: (limit: number): Observable<HomeFeaturedParkModel[]> => {
        parkLimits.push(limit);
        return of([createPark()]);
      },
      getLatestArticles: (limit: number): Observable<HomeLatestArticleModel[]> => {
        articleLimits.push(limit);
        return of([createArticle()]);
      }
    };
    TestBed.configureTestingModule({
      providers: [
        HomeLatestContentStateFacade,
        NaturalTextTruncatorService,
        CountryDisplayService,
        { provide: HOME_LATEST_CONTENT_DATA_PORT, useValue: dataPort }
      ]
    });
    facade = TestBed.inject(HomeLatestContentStateFacade);
  });

  it('loads three parks and articles and maps them for the active language', () => {
    facade.setCurrentLanguage('fr');
    facade.loadLatestContent();

    expect(parkLimits).toEqual([3]);
    expect(articleLimits).toEqual([3]);
    expect(facade.latestParksState().kind).toBe('ready');
    expect(facade.latestArticlesState().kind).toBe('ready');
    expect(facade.latestParks()[0].description).toBe('Description française');
    expect(facade.latestArticles()[0].title).toBe('Article français');
  });

  it('remaps loaded content without another request when the language changes', () => {
    facade.setCurrentLanguage('fr');
    facade.loadLatestContent();
    facade.setCurrentLanguage('en');

    expect(facade.latestParks()[0].description).toBe('English description');
    expect(facade.latestArticles()[0].title).toBe('English article');
    expect(parkLimits).toEqual([3]);
    expect(articleLimits).toEqual([3]);
  });
});

function createPark(): HomeFeaturedParkModel {
  return {
    id: 'park-1',
    name: 'Park',
    countryCode: 'FR',
    type: 'ThemePark',
    latitude: 48,
    longitude: 2,
    descriptions: [
      { languageCode: 'fr', value: '<p>Description française</p>' },
      { languageCode: 'en', value: '<p>English description</p>' }
    ],
    city: 'Paris',
    currentLogoImageId: 'logo-1',
    isManualFeatured: false,
    isSponsoredFeatured: false,
    countsByCategory: []
  };
}

function createArticle(): HomeLatestArticleModel {
  return {
    eventId: 'event-1',
    entityType: 'Park',
    parkId: 'park-1',
    parkName: 'Park',
    parkItemId: null,
    parkItemName: null,
    slug: 'article',
    titles: [
      { languageCode: 'fr', value: 'Article français' },
      { languageCode: 'en', value: 'English article' }
    ],
    summaries: [{ languageCode: 'fr', value: 'Résumé' }],
    mainImageId: 'image-1'
  };
}
