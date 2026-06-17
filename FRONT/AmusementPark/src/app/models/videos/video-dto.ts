import { LocalizedItemDto } from '../shared/localized-item-dto';
import { VideoHostingProvider } from './video-hosting-provider';
import { VideoOwnerType } from './video-owner-type';
import { VideoType } from './video-type';

export interface VideoDto {
  id: string;
  hostingProvider: VideoHostingProvider;
  ownerType: VideoOwnerType;
  ownerId?: string | null;
  type: VideoType;
  originalUrl: string;
  canonicalUrl: string;
  embedUrl?: string | null;
  externalId?: string | null;
  title: string;
  description?: string | null;
  creatorName?: string | null;
  creatorUrl?: string | null;
  thumbnailUrl?: string | null;
  thumbnailImageId?: string | null;
  durationSeconds?: number | null;
  publishedAtUtc?: string | null;
  languageCodes: string[];
  titles: LocalizedItemDto<string>[];
  descriptions: LocalizedItemDto<string>[];
  tagIds: string[];
  externalMetadata: VideoExternalMetadataDto;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface VideoExternalMetadataDto {
  source?: string | null;
  fetchedAtUtc?: string | null;
  providerTitle?: string | null;
  providerDescription?: string | null;
  providerChannelId?: string | null;
  providerChannelUrl?: string | null;
}
