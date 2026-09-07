import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { SharePublicationPreviewRequest, SharePublicationPublishRequest } from '@app/models/sharing/share-publication.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { SharePublicationsApiService } from './share-publications-api.service';

describe('SharePublicationsApiService', () => {
  let service: SharePublicationsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(SharePublicationsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('previews the selected public fields without adding private identifiers', () => {
    const requestBody: SharePublicationPreviewRequest = {
      publicationType: 'PersonalRanking',
      sourceId: null,
      datePrecision: 'Hidden',
      includedFields: ['GlobalRatings']
    };

    service.preview(requestBody).subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}me/shares/preview`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(requestBody);
    expect(request.request.body).not.toHaveProperty('userId');
    request.flush({
      publicationType: 'PersonalRanking',
      sourceVersion: 12,
      approvalToken: 'approved-preview',
      contentPolicy: { schemaVersion: 1, datePrecision: 'Hidden', includedFields: ['GlobalRatings'] },
      personalRanking: { displayName: 'User', avatarUrl: null, statistics: null, ratings: [], isTruncated: false }
    });
  });

  it('publishes only the exact server preview approved by the user', () => {
    const requestBody: SharePublicationPublishRequest = {
      publicationType: 'PersonalRanking',
      sourceId: null,
      approvedSourceVersion: 12,
      approvedPolicySchemaVersion: 1,
      approvedDatePrecision: 'Hidden',
      approvedIncludedFields: ['GlobalRatings'],
      approvalToken: 'approved-preview'
    };

    service.publish(requestBody).subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}me/shares/publish`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(requestBody);
    expect(request.request.body).not.toHaveProperty('userId');
    request.flush({
      isPublic: true,
      shareId: 'opaque-share-id',
      publishedAtUtc: '2026-09-07T08:00:00Z',
      policySchemaVersion: 1,
      datePrecision: 'Hidden',
      includedFields: ['GlobalRatings']
    });
  });
});
