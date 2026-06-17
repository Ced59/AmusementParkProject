import { LocalizedItemDto } from '../shared/localized-item-dto';
import { VideoOwnerType } from './video-owner-type';
import { VideoType } from './video-type';

export interface VideoWriteRequest {
  originalUrl: string;
  ownerType: VideoOwnerType;
  ownerId: string;
  type: VideoType;
  title?: string | null;
  description?: string | null;
  creatorName?: string | null;
  creatorUrl?: string | null;
  thumbnailUrl?: string | null;
  durationSeconds?: number | null;
  publishedAtUtc?: string | null;
  languageCodes: string[];
  titles: LocalizedItemDto<string>[];
  descriptions: LocalizedItemDto<string>[];
  tagIds: string[];
  isPublished: boolean;
}
