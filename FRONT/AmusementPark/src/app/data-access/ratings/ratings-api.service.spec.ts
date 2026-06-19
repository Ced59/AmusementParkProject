import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ParkRatingRanking, RatingSummary, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { RatingsApiService } from './ratings-api.service';

describe('RatingsApiService', () => {
  let service: RatingsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(RatingsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads a public rating summary for a target', () => {
    const summaries: RatingSummary[] = [];

    service.getSummary('Park', 'park 1').subscribe((result: RatingSummary): void => {
      summaries.push(result);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}ratings/Park/park%201/summary`);
    expect(request.request.method).toBe('GET');
    request.flush({
      targetType: 'Park',
      targetId: 'park 1',
      ratingCount: 2,
      averageRating: 4.5,
      bayesianScore: 3.67
    });

    expect(summaries[0]?.averageRating).toBe(4.5);
  });

  it('upserts the authenticated user rating through a JSON PUT request', () => {
    const body: UserRatingUpsertRequest = {
      targetType: 'ParkItem',
      targetId: 'item-1',
      value: 4.5
    };
    const ratings: UserRating[] = [];

    service.upsertRating(body).subscribe((result: UserRating): void => {
      ratings.push(result);
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}ratings`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.headers.get('Content-Type')).toBe('application/json');
    expect(request.request.body).toEqual(body);
    request.flush(createUserRating());

    expect(ratings[0]?.value).toBe(4.5);
  });

  it('loads filtered rankings and unwraps paged responses', () => {
    let items: ParkRatingRanking[] = [];

    service.getRankings(2, 5, 'Attraction', 'Demo').subscribe((page): void => {
      items = page.items;
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}ratings/rankings?page=2&size=5&category=Attraction&search=Demo`);
    expect(request.request.method).toBe('GET');
    request.flush({
      data: [
        {
          rank: 1,
          parkId: 'park-1',
          parkName: 'Demo Park',
          ratingCount: 8,
          score: 4.21,
          parkRatingCount: 3,
          parkAverageRating: 4.5,
          itemsRatingCount: 5,
          itemsAverageRating: 4,
          categories: []
        }
      ],
      pagination: { page: 2, pageSize: 5, totalItems: 1, totalPages: 1 }
    });

    expect(items.length).toBe(1);
    expect(items[0]?.parkName).toBe('Demo Park');
  });

  function createUserRating(): UserRating {
    return {
      id: 'rating-1',
      targetType: 'ParkItem',
      targetId: 'item-1',
      parkId: 'park-1',
      parkItemCategory: 'Attraction',
      parkItemType: 'RollerCoaster',
      value: 4.5,
      createdAtUtc: '2026-06-19T10:00:00Z',
      updatedAtUtc: '2026-06-19T10:00:00Z',
      summary: {
        targetType: 'ParkItem',
        targetId: 'item-1',
        ratingCount: 2,
        averageRating: 4.5,
        bayesianScore: 3.67
      }
    };
  }
});
