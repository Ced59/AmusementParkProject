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

  it('updates and deletes a tracked publication', () => {
    service.update('publication 1', { message: 'Updated' }).subscribe();

    const updateRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/social-publications/publication%201`
    );
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual({ message: 'Updated' });
    updateRequest.flush(createPublication('Published'));

    service.delete('publication 1').subscribe();
    const deleteRequest = httpTestingController.expectOne(
      `${environment.apiBaseUrl}admin/social-publications/publication%201`
    );
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(createPublication('Deleted'));
  });

  it('synchronizes recent tracked publications', () => {
    service.synchronize(10).subscribe();

    const request = httpTestingController.expectOne(
      (candidate) => candidate.url === `${environment.apiBaseUrl}admin/social-publications/synchronize`
    );
    expect(request.request.method).toBe('POST');
    expect(request.request.params.get('limit')).toBe('10');
    request.flush({ checkedCount: 1, updatedCount: 0, deletedCount: 1, failureCount: 0 });
  });
});

function createPublication(status: 'Published' | 'Failed' | 'Deleted') {
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
    deletedAtUtc: status === 'Deleted' ? '2026-08-05T10:10:00Z' : null,
    lastSynchronizedAtUtc: status === 'Deleted' ? '2026-08-05T10:10:00Z' : null,
    externalPostId: status !== 'Failed' ? '123_456' : null,
    externalPostUrl: status !== 'Failed' ? 'https://www.facebook.com/123_456' : null,
    failureCode: status === 'Failed' ? 'error' : null,
    failureMessage: status === 'Failed' ? 'Failure' : null
  };
}
