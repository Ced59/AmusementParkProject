import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublishingOverview
} from '@app/models/social-publishing/social-publishing.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import {
  ADMIN_SOCIAL_PUBLISHING_DATA_PORT,
  AdminSocialPublishingDataPort
} from './admin-social-publishing-data.ports';
import { AdminSocialPublishingFacade } from './admin-social-publishing.facade';

describe('AdminSocialPublishingFacade', () => {
  let facade: AdminSocialPublishingFacade;
  let port: MockedObject<AdminSocialPublishingDataPort>;
  let toastMessageService: MockedObject<ToastMessageService>;

  const overview: SocialPublishingOverview = {
    publishers: [{
      network: 'Facebook',
      displayName: 'Facebook',
      isEnabled: true,
      isConfigured: true,
      targetUrl: 'https://www.facebook.com/test',
      supportsAutomaticParkAnnouncements: true
    }],
    recentPublications: []
  };

  beforeEach(() => {
    port = {
      getOverview: vi.fn(),
      publish: vi.fn(),
      retry: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
      synchronize: vi.fn()
    } as unknown as MockedObject<AdminSocialPublishingDataPort>;
    toastMessageService = {
      add: vi.fn()
    } as unknown as MockedObject<ToastMessageService>;

    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminSocialPublishingFacade,
        { provide: ADMIN_SOCIAL_PUBLISHING_DATA_PORT, useValue: port },
        { provide: ToastMessageService, useValue: toastMessageService }
      ]
    });
    facade = TestBed.inject(AdminSocialPublishingFacade);
  });

  it('loads Facebook configuration and exposes it through signals', () => {
    port.getOverview.mockReturnValue(of(overview));

    facade.load();

    expect(facade.state().kind).toBe('ready');
    expect(facade.facebookPublisher()?.isConfigured).toBe(true);
    expect(facade.recentPublications()).toEqual([]);
  });

  it('publishes a link and prepends the result to history', () => {
    const request: PublishSocialLinkRequest = {
      network: 'Facebook',
      message: 'Message',
      url: 'https://amusement-parks.fun/fr/home'
    };
    const publication: SocialPublication = createPublication('Published');
    port.getOverview.mockReturnValue(of(overview));
    port.publish.mockReturnValue(of(publication));
    facade.load();

    facade.publish(request);

    expect(port.publish).toHaveBeenCalledWith(request);
    expect(facade.recentPublications()).toEqual([publication]);
    expect(facade.publishing()).toBe(false);
    expect(toastMessageService.add).toHaveBeenCalledWith(
      'success',
      expect.any(String),
      expect.any(String)
    );
  });

  it('retries a failed publication and replaces its history entry', () => {
    const failedPublication: SocialPublication = createPublication('Failed');
    const publishedPublication: SocialPublication = createPublication('Published');
    port.getOverview.mockReturnValue(of({ ...overview, recentPublications: [failedPublication] }));
    port.retry.mockReturnValue(of(publishedPublication));
    facade.load();

    facade.retry(failedPublication.id);

    expect(port.retry).toHaveBeenCalledWith(failedPublication.id);
    expect(facade.recentPublications()).toEqual([publishedPublication]);
    expect(facade.retryingPublicationId()).toBeNull();
  });

  it('updates and deletes a published publication in history', () => {
    const publication: SocialPublication = createPublication('Published');
    const updatedPublication: SocialPublication = { ...publication, message: 'Updated' };
    const deletedPublication: SocialPublication = {
      ...updatedPublication,
      status: 'Deleted',
      deletedAtUtc: '2026-08-05T10:10:00Z'
    };
    port.getOverview.mockReturnValue(of({ ...overview, recentPublications: [publication] }));
    port.update.mockReturnValue(of(updatedPublication));
    port.delete.mockReturnValue(of(deletedPublication));
    facade.load();

    facade.update(publication.id, 'Updated');
    facade.delete(publication.id);

    expect(port.update).toHaveBeenCalledWith(publication.id, { message: 'Updated' });
    expect(port.delete).toHaveBeenCalledWith(publication.id);
    expect(facade.recentPublications()[0].status).toBe('Deleted');
  });

  it('synchronizes then reloads the overview', () => {
    port.getOverview.mockReturnValue(of(overview));
    port.synchronize.mockReturnValue(of({ checkedCount: 1, updatedCount: 0, deletedCount: 1, failureCount: 0 }));
    facade.load();

    facade.synchronize();

    expect(port.synchronize).toHaveBeenCalled();
    expect(port.getOverview).toHaveBeenCalledTimes(2);
    expect(facade.synchronizing()).toBe(false);
  });
});

function createPublication(status: 'Published' | 'Failed' | 'Deleted'): SocialPublication {
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
