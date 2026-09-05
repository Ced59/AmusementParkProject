import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { HomeLatestArticleModel } from '@app/models/home/home-latest-article.model';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { HOME_LATEST_CONTENT_DATA_PORT, HomeLatestContentDataPort } from '../state/home-latest-content-data.ports';
import { HomeLatestContentComponent } from './home-latest-content.component';

const homeLatestContentDataPort: HomeLatestContentDataPort = {
  getLatestParks: (): Observable<HomeFeaturedParkModel[]> => of([createPark()]),
  getLatestArticles: (): Observable<HomeLatestArticleModel[]> => of([createArticle()])
};

describe('HomeLatestContentComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, HomeLatestContentComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: HOME_LATEST_CONTENT_DATA_PORT, useValue: homeLatestContentDataPort }
      ]
    }).compileComponents();
  });

  it('renders the latest park and article with detailed cards', () => {
    const fixture: ComponentFixture<HomeLatestContentComponent> = TestBed.createComponent(HomeLatestContentComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('app-ui-featured-park-card').length).toBe(1);
    expect(fixture.nativeElement.querySelectorAll('app-ui-article-card').length).toBe(1);
  });

  it('uses horizontal snap rails on mobile without compacting the cards', () => {
    const styles: string = (
      HomeLatestContentComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toMatch(/@media \(max-width: 680px\)[\s\S]*\.home-latest__grid[\s\S]*display: flex/);
    expect(styles).toMatch(/\.home-latest__grid[\s\S]*overflow-x: auto[\s\S]*scroll-snap-type: x mandatory/);
    expect(styles).toContain('flex: 0 0 min(84vw, 21rem)');
    expect(styles).toMatch(/\.home-latest-content[^{]*,\s*\.home-latest[^{]*\{[^}]*min-width: 0/);
    expect(styles).toMatch(/\.home-latest[^{]*>\s*app-page-state[^{]*\{[^}]*min-width: 0;[^}]*width: 100%/);
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
    descriptions: [{ languageCode: 'en', value: 'Description' }],
    city: 'Paris',
    currentLogoImageId: null,
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
    titles: [{ languageCode: 'en', value: 'Latest article' }],
    summaries: [{ languageCode: 'en', value: 'Article summary' }],
    mainImageId: null
  };
}
