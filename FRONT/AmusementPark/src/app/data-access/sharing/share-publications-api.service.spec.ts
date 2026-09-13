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

  it('loads the share settings for one owned visit', () => {
    service.getVisitSettings('visit/with spaces').subscribe();

    const readRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fwith%20spaces/share`
    );
    expect(readRequest.request.method).toBe('GET');
    readRequest.flush({ isPublic: false, includedFields: [] });

  });

  it('rotates and revokes every publication through the central lifecycle', () => {
    service.rotate('publication/with spaces').subscribe();
    const rotateRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/shares/publication%2Fwith%20spaces/rotate-link`
    );
    expect(rotateRequest.request.method).toBe('POST');
    expect(rotateRequest.request.body).toEqual({});
    rotateRequest.flush({ isPublic: true, shareId: 'new-token', includedFields: [] });

    service.revoke('publication/with spaces').subscribe();
    const revokeRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/shares/publication%2Fwith%20spaces`
    );
    expect(revokeRequest.request.method).toBe('DELETE');
    revokeRequest.flush({ isPublic: false, shareId: null, includedFields: [] });
  });

  it('loads a bounded candidate list before previewing a visit recap', () => {
    service.getVisitCandidates('visit/with spaces', true).subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/passport/visits/visit%2Fwith%20spaces/share/candidates?includeMissedItems=true`
    );
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], totalEligibleItemCount: 0, isTruncated: false });
  });

  it('loads an anonymous visit recap only from its opaque share link', () => {
    service.getSharedVisit('opaque/token').subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}passport/shared/visits/opaque%2Ftoken`
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      publishedAtUtc: '2026-09-07T08:00:00Z',
      visitRecap: {
        parkId: 'park-1',
        parkName: 'Denain Évasion',
        categories: [],
        items: [],
        hasHiddenDate: true,
        hasIncompleteRatings: false,
        hasIncompleteItems: false
      }
    });
  });

  it('loads and publicly resolves the selected passport profile', () => {
    service.getPassportProfileSelection().subscribe();
    const selection = httpTestingController.expectOne(`${environment.apiBaseUrl}me/passport/share/selection`);
    expect(selection.request.method).toBe('GET');
    selection.flush({ years: [], parks: [], ratings: [], savedVisibility: 'Unlisted', savedAllowsComparisons: false, hasSavedSnapshot: false });

    service.getPassportProfileSettings().subscribe();
    const settings = httpTestingController.expectOne(`${environment.apiBaseUrl}me/passport/share`);
    expect(settings.request.method).toBe('GET');
    settings.flush({ isPublic: false, includedFields: [] });

    service.getSharedPassportProfile('opaque/token').subscribe();
    const publicProfile = httpTestingController.expectOne(
      `${environment.apiBaseUrl}passport/shared/profiles/opaque%2Ftoken`
    );
    expect(publicProfile.request.method).toBe('GET');
    publicProfile.flush({ publishedAtUtc: '2026-09-12T00:00:00Z', passportProfile: {} });
  });
});
