import { VideoHostingProvider } from './video-hosting-provider';
import { VideoOwnerType } from './video-owner-type';
import { VideoType } from './video-type';

export interface VideoSearchQuery {
  page?: number;
  size?: number;
  search?: string | null;
  hostingProvider?: VideoHostingProvider | null;
  ownerType?: VideoOwnerType | null;
  ownerId?: string | null;
  type?: VideoType | null;
  tagId?: string | null;
  creatorName?: string | null;
  languageCode?: string | null;
  isPublished?: boolean | null;
  sortBy?: string | null;
  sortDirection?: 'asc' | 'desc' | string | null;
}
