import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminSocialPublicationsApiService } from '@data-access/social-publishing/admin-social-publications-api.service';
import {
  PublishSocialLinkRequest,
  SocialPublication,
  SocialPublicationSynchronizationResult,
  SocialPublishingOverview,
  UpdateSocialPublicationRequest
} from '@app/models/social-publishing/social-publishing.models';

export interface AdminSocialPublishingDataPort {
  getOverview(limit?: number): Observable<SocialPublishingOverview>;
  publish(request: PublishSocialLinkRequest): Observable<SocialPublication>;
  retry(publicationId: string): Observable<SocialPublication>;
  update(publicationId: string, request: UpdateSocialPublicationRequest): Observable<SocialPublication>;
  delete(publicationId: string): Observable<SocialPublication>;
  synchronize(limit?: number): Observable<SocialPublicationSynchronizationResult>;
}

export const ADMIN_SOCIAL_PUBLISHING_DATA_PORT = new InjectionToken<AdminSocialPublishingDataPort>(
  'ADMIN_SOCIAL_PUBLISHING_DATA_PORT',
  {
    providedIn: 'root',
    factory: () => inject(AdminSocialPublicationsApiService)
  }
);
