import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ProfileComparisonsApiService } from './profile-comparisons-api.service';

describe('ProfileComparisonsApiService', () => {
  let service: ProfileComparisonsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies(),
    });
    service = TestBed.inject(ProfileComparisonsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads a shared comparison with an encoded opaque token', () => {
    service.getShared('opaque/token').subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}passport/shared/comparisons/opaque%2Ftoken`,
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      createdAtUtc: '2026-09-13T12:00:00Z',
      creatorDisplayName: 'Camille',
      acceptorDisplayName: 'Alex',
      categories: ['VisitedParks'],
      parks: [],
      ratings: [],
      years: [],
      missedItems: [],
      commonRatingCount: 0,
      minimumRatingsForCorrelation: 5,
      ratingCorrelation: null,
      hasIncompleteCatalog: false,
      calculationVersion: 'profile-comparison-v1',
    });
  });

  it('revokes by opaque share token without sending a technical body', () => {
    service.revoke('opaque-share').subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/profile-comparisons/opaque-share`,
    );
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toBeNull();
    request.flush({ revokedAtUtc: '2026-09-13T12:05:00Z' });
  });
});
