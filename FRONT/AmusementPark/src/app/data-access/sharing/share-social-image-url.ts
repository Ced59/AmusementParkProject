import { environment } from '../../../environments/environment';
import { SHARE_PUBLICATIONS_API_ENDPOINTS } from './share-publications-api-endpoints';

export type ShareSocialImagePublicationType = 'visit' | 'year' | 'passport';

export function buildShareSocialImageUrl(
  publicationType: ShareSocialImagePublicationType,
  shareId: string,
  publicationVersion: number,
  language: string
): string {
  const endpoint: string = SHARE_PUBLICATIONS_API_ENDPOINTS.socialImage(
    publicationType,
    shareId,
    publicationVersion,
    language
  );
  return `${environment.apiBaseUrl}${endpoint}`;
}
