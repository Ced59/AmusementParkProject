import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { HomeApiService } from './home-api.service';

describe('HomeApiService', () => {
  let service: HomeApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(HomeApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('requests home stats', () => {
    service.getHomeStats().subscribe((stats) => {
      expect(stats.parksCount).toBe(10);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}public-stats/home`);
    expect(request.request.method).toBe('GET');
    request.flush({ parksCount: 10, attractionsCount: 20, countriesCount: 3 });
  });

  it('requests featured parks with trimmed excluded ids and encoded limit', () => {
    service.getFeaturedParks([' park 1 ', ' ', 'park/2'], 5).subscribe((parks) => {
      expect(parks).toEqual([]);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/home-featured?limit=5&excludeIds=park%201&excludeIds=park%2F2`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('requests the latest home content with the requested limits', () => {
    service.getLatestParks(3).subscribe((parks) => {
      expect(parks).toEqual([]);
    });
    service.getLatestArticles(3).subscribe((articles) => {
      expect(articles).toEqual([]);
    });

    const parksRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/home-latest?limit=3`);
    const articlesRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}history/articles/latest?limit=3`);
    expect(parksRequest.request.method).toBe('GET');
    expect(articlesRequest.request.method).toBe('GET');
    parksRequest.flush([]);
    articlesRequest.flush([]);
  });
});
