import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Park } from '@app/models/parks/park';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkOpeningHoursCalendar, ParkOpeningHoursSchedule } from '@app/models/parks/park-opening-hours';
import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParksApiService } from './parks-api.service';

describe('ParksApiService', () => {
  let service: ParksApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ParksApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  function createPark(overrides: Partial<Park> = {}): Park {
    return {
      id: 'park-1',
      name: 'Park',
      countryCode: 'BE',
      latitude: 50,
      longitude: 3,
      descriptions: [],
      ...overrides
    };
  }

  it('gets paginated parks and trims visible map search queries before endpoint generation', () => {
    service.getParksPaginated(2, 20, true, 'europe').subscribe();
    service.getVisibleParkMapPoints('  parc  ', null).subscribe();

    const pageRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks?page=2&size=20&visibleOnly=true&region=europe`);
    expect(pageRequest.request.method).toBe('GET');
    pageRequest.flush({ data: [] });

    const mapRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/map-visible?query=parc`);
    expect(mapRequest.request.method).toBe('GET');
    mapRequest.flush([]);
  });

  it('unwraps geolocation search responses whether API returns arrays or collections', () => {
    let result: Park[] = [];
    service.getParksByLocation(50, 3, 1000).subscribe((parks: Park[]) => {
      result = parks;
    });

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/geo-search?latitude=50&longitude=3&radiusMeters=1000`);
    request.flush({ data: [createPark()] });

    expect(result.length).toBe(1);
  });

  it('shares in-flight park detail summary requests without persisting stale data', () => {
    const firstSummary: ParkDetailSummary = createParkDetailSummary('Park');
    const secondSummary: ParkDetailSummary = createParkDetailSummary('Updated Park');
    const firstResults: ParkDetailSummary[] = [];
    const secondResults: ParkDetailSummary[] = [];
    const thirdResults: ParkDetailSummary[] = [];

    service.getParkDetailSummary('park-1').subscribe((summary: ParkDetailSummary): void => {
      firstResults.push(summary);
    });
    service.getParkDetailSummary('park-1').subscribe((summary: ParkDetailSummary): void => {
      secondResults.push(summary);
    });

    const sharedRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1/detail-summary`);
    expect(sharedRequest.request.method).toBe('GET');
    sharedRequest.flush(firstSummary);

    expect(firstResults[0]?.park.name).toBe('Park');
    expect(secondResults[0]?.park.name).toBe('Park');

    service.getParkDetailSummary('park-1').subscribe((summary: ParkDetailSummary): void => {
      thirdResults.push(summary);
    });

    const refreshedRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1/detail-summary`);
    refreshedRequest.flush(secondSummary);

    expect(thirdResults[0]?.park.name).toBe('Updated Park');
  });

  it('shares in-flight nearest park requests without persisting stale data', () => {
    const firstResponse: ParkDistanceResponse = createNearestResponse('near-1');
    const secondResponse: ParkDistanceResponse = createNearestResponse('near-2');
    const firstResults: ParkDistanceResponse[] = [];
    const secondResults: ParkDistanceResponse[] = [];
    const thirdResults: ParkDistanceResponse[] = [];

    service.getNearestParks('park-1', 4, null).subscribe((response: ParkDistanceResponse): void => {
      firstResults.push(response);
    });
    service.getNearestParks('park-1', 4, null).subscribe((response: ParkDistanceResponse): void => {
      secondResults.push(response);
    });

    const sharedRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1/nearby?limit=4`);
    expect(sharedRequest.request.method).toBe('GET');
    sharedRequest.flush(firstResponse);

    expect(firstResults[0]?.targets[0]?.park.id).toBe('near-1');
    expect(secondResults[0]?.targets[0]?.park.id).toBe('near-1');

    service.getNearestParks('park-1', 4, null).subscribe((response: ParkDistanceResponse): void => {
      thirdResults.push(response);
    });

    const refreshedRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1/nearby?limit=4`);
    refreshedRequest.flush(secondResponse);

    expect(thirdResults[0]?.targets[0]?.park.id).toBe('near-2');
  });

  it('normalizes park write requests for create and update', () => {
    const park: Park = createPark({
      isVisible: undefined,
      adminReviewStatus: undefined,
      isFeaturedOnHome: true,
      isFeaturedOnHomeSponsored: true,
      featuredHomeOrder: 0,
      audienceClassification: 'Regional',
      openingDate: '1987-05-20',
      closingDate: '1991-10-20',
      openingDateText: '1987-05-20',
      closingDateText: '1991-10-20',
      webSiteUrl: 'https://park.test'
    });

    service.createPark(park).subscribe();
    service.updatePark('park-1', { ...park, featuredHomeOrder: 4, isFeaturedOnHome: false, isFeaturedOnHomeSponsored: true }).subscribe();

    const createRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks`);
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.headers.get('Content-Type')).toBe('application/json');
    expect(createRequest.request.body.featuredHomeOrder).toBeNull();
    expect(createRequest.request.body.isVisible).toBeTrue();
    expect(createRequest.request.body.adminReviewStatus).toBe('Validated');
    expect(createRequest.request.body.audienceClassification).toBe('Regional');
    expect(createRequest.request.body.isFeaturedOnHomeSponsored).toBeTrue();
    expect(createRequest.request.body.openingDate).toBe('1987-05-20');
    expect(createRequest.request.body.closingDate).toBe('1991-10-20');
    expect(createRequest.request.body.openingDateText).toBe('1987-05-20');
    expect(createRequest.request.body.closingDateText).toBe('1991-10-20');
    createRequest.flush(park);

    const updateRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1`);
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body.featuredHomeOrder).toBe(4);
    expect(updateRequest.request.body.isFeaturedOnHomeSponsored).toBeFalse();
    updateRequest.flush(park);
  });

  it('updates visibility and bulk administration through PATCH requests', () => {
    service.updateParkVisibility('park-1', false).subscribe();
    service.updateParksBulkAdministration({ ids: ['park-1'], adminReviewStatus: 'Validated' }).subscribe();

    const visibilityRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1/visibility`);
    expect(visibilityRequest.request.method).toBe('PATCH');
    expect(visibilityRequest.request.body).toEqual({ isVisible: false });
    visibilityRequest.flush(createPark());

    const bulkRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/bulk-administration`);
    expect(bulkRequest.request.method).toBe('PATCH');
    bulkRequest.flush({ requestedCount: 1, updatedCount: 1 });
  });

  it('reads and upserts park opening hours through JSON endpoints', () => {
    const calendar: ParkOpeningHoursCalendar = {
      parkId: 'park-1',
      timeZoneId: 'Europe/Paris',
      updatedAtUtc: '2026-06-29T10:00:00Z',
      fromDate: '2026-07-01',
      toDate: '2026-07-01',
      days: []
    };
    const schedule: ParkOpeningHoursSchedule = {
      parkId: 'park-1',
      timeZoneId: 'Europe/Paris',
      regularRules: [],
      dateOverrides: []
    };

    service.getParkOpeningHours('park-1', '2026-07-01', '2026-07-31').subscribe((result: ParkOpeningHoursCalendar): void => {
      expect(result).toEqual(calendar);
    });
    service.getAdminParkOpeningHours('park-1').subscribe((result: ParkOpeningHoursSchedule): void => {
      expect(result).toEqual(schedule);
    });
    service.upsertAdminParkOpeningHours('park-1', schedule).subscribe((result: ParkOpeningHoursSchedule): void => {
      expect(result).toEqual(schedule);
    });

    const publicRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}parks/park-1/opening-hours?from=2026-07-01&to=2026-07-31`);
    expect(publicRequest.request.method).toBe('GET');
    publicRequest.flush(calendar);

    const adminGetRequest = httpTestingController.expectOne((request) => {
      return request.urlWithParams === `${environment.apiBaseUrl}admin/parks/park-1/opening-hours` && request.method === 'GET';
    });
    expect(adminGetRequest.request.method).toBe('GET');
    adminGetRequest.flush(schedule);

    const adminPutRequest = httpTestingController.expectOne((request) => {
      return request.urlWithParams === `${environment.apiBaseUrl}admin/parks/park-1/opening-hours` && request.method === 'PUT';
    });
    expect(adminPutRequest.request.method).toBe('PUT');
    expect(adminPutRequest.request.headers.get('Content-Type')).toBe('application/json');
    expect(adminPutRequest.request.body).toEqual(schedule);
    adminPutRequest.flush(schedule);
  });

  function createParkDetailSummary(name: string): ParkDetailSummary {
    return {
      park: createPark({ name }),
      mainImage: null,
      references: {},
      stats: {
        totalItems: 0,
        zoneCount: 0,
        attractionCount: 0,
        restaurantCount: 0,
        showCount: 0,
        shopCount: 0,
        hotelCount: 0,
        countsByCategory: {}
      }
    };
  }

  function createNearestResponse(targetParkId: string): ParkDistanceResponse {
    return {
      source: {
        id: 'park-1',
        name: 'Park',
        countryCode: 'BE',
        latitude: 50,
        longitude: 3
      },
      distanceUnit: 'km',
      calculationKind: 'nearest',
      targets: [
        {
          proximityRank: 1,
          distanceKilometers: 12,
          distanceMeters: 12000,
          distanceUnit: 'km',
          estimatedTravelDurationMinutes: 18,
          park: createPark({ id: targetParkId })
        }
      ],
      missingTargetParkIds: [],
      unavailableTargetParkIds: []
    };
  }
});
