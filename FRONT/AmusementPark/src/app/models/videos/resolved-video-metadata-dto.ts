import { VideoHostingProvider } from './video-hosting-provider';

export interface ResolvedVideoMetadataDto {
  hostingProvider: VideoHostingProvider;
  originalUrl: string;
  canonicalUrl: string;
  embedUrl?: string | null;
  externalId?: string | null;
  title?: string | null;
  description?: string | null;
  creatorName?: string | null;
  creatorUrl?: string | null;
  thumbnailUrl?: string | null;
  durationSeconds?: number | null;
  publishedAtUtc?: string | null;
  detectedLanguageCode?: string | null;
  metadataSource?: string | null;
  fetchedAtUtc?: string | null;
  providerChannelId?: string | null;
  providerChannelUrl?: string | null;
}
