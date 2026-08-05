import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PublishSocialLinkRequest } from '@app/models/social-publishing/social-publishing.models';
import { environment } from '../../../environments/environment';
import { AdminSocialPublicationsApiService } from './admin-social-publications-api.service';

describe('AdminSocialPublicationsApiService', () => {
  let service: AdminSocialPublicationsApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideCommonTestDependencies()
    });
    service = TestBed.inject(AdminSocialPublicationsApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('loads provider configuration and recent publications', () => {
    service.getOverview(10).subscribe((overview) => {
      expect(overview.publishers[0].network).toBe('Facebook');
    });

    const request = httpTestingController.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}admin/social-publications`
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('limit')).toBe('10');
    request.flush({
      publishers: [{
        network: 'Facebook',
        displayName: 'Facebook',
        isEnabled: true,
        isConfigured: true,
        targetUrl: 'https://www.facebook.com/test',
        supportsAutomaticParkAnnouncements: true
      }],
      recentPublications: []
    });
  });

  it('publishes a site link through the admin endpoint', () => {
    const body: PublishSocialLinkRequest = {
      network: 'Facebook',
      message: 'Message',
      url: 'https://amusement-parks.fun/fr/home'
    };

    service.publish(body).subscribe((publication) => {
      expect(publication.status).toBe('Published');
    });

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/social-publications`
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(createPublication('Published'));
  });

  it('retries a failed publication with an encoded identifier', () => {
    service.retry('publication 1').subscribe();

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/social-publications/publication%201/retry`
    );
    expect(request.request.method).toBe('POST');
    request.flush(createPublication('Published'));
  });
});

function createPublication(status: 'Published' | 'Failed') {
  return {
    id: 'publication-1',
    network: 'Facebook',
    status,
    trigger: 'Manual',
    message: 'Message',
    url: 'https://amusement-parks.fun/fr/home',
    sourceEntityType: null,
    sourceEntityId: null,
    requestedAtUtc: '2026-08-05T10:00:00Z',
    attemptedAtUtc: '2026-08-05T10:00:00Z',
    publishedAtUtc: status === 'Published' ? '2026-08-05T10:00:01Z' : null,
    externalPostId: status === 'Published' ? '123_456' : null,
    externalPostUrl: status === 'Published' ? 'https://www.facebook.com/123_456' : null,
    failureCode: status === 'Failed' ? 'error' : null,
    failureMessage: status === 'Failed' ? 'Failure' : null
  };
}
