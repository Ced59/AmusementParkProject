import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoType } from '@app/models/videos/video-type';
import { resolveLocalizedText, stripHtml } from '@shared/utils/localization/localized-text.helpers';
import {
  PublicVideoCardViewModel,
  PublicVideoNavigationItem,
  PublicVideoSelectOption,
  PublicVideoTagViewModel,
  PublicVideoWatchViewModel
} from '../models/public-video-view.model';
import { SafeVideoEmbedUrlService } from '../services/safe-video-embed-url.service';

export const PUBLIC_VIDEO_TYPES: readonly VideoType[] = [
  VideoType.ON_RIDE,
  VideoType.OFF_RIDE,
  VideoType.WALKTHROUGH,
  VideoType.ADVERTISEMENT,
  VideoType.DOCUMENTARY,
  VideoType.REVIEW,
  VideoType.NEWS,
  VideoType.EVENT,
  VideoType.INTERVIEW,
  VideoType.OTHER
];

export function buildPublicVideoCards(
  videos: readonly VideoDto[],
  tags: readonly VideoTagDto[],
  currentLanguage: string,
  buildDetailLink: (video: VideoDto) => string[] | null
): PublicVideoCardViewModel[] {
  const tagLookup: Map<string, PublicVideoTagViewModel> = buildVideoTagLookup(tags, currentLanguage);

  return videos.map((video: VideoDto) => buildPublicVideoCard(video, tagLookup, currentLanguage, buildDetailLink(video)));
}

export function buildPublicVideoWatchView(
  video: VideoDto,
  tags: readonly VideoTagDto[],
  currentLanguage: string,
  embedUrlService: SafeVideoEmbedUrlService,
  detailLink: string[] | null
): PublicVideoWatchViewModel {
  const tagLookup: Map<string, PublicVideoTagViewModel> = buildVideoTagLookup(tags, currentLanguage);
  const card: PublicVideoCardViewModel = buildPublicVideoCard(video, tagLookup, currentLanguage, detailLink);

  return {
    ...card,
    canonicalUrl: normalizeOptionalString(video.canonicalUrl) ?? normalizeOptionalString(video.originalUrl) ?? '',
    originalUrl: normalizeOptionalString(video.originalUrl) ?? normalizeOptionalString(video.canonicalUrl) ?? '',
    embedUrl: embedUrlService.resolve(video.embedUrl)
  };
}

export function buildVideoTagOptions(tags: readonly VideoTagDto[], currentLanguage: string): PublicVideoSelectOption[] {
  return tags
    .filter((tag: VideoTagDto) => tag.isActive !== false)
    .map((tag: VideoTagDto) => ({
      value: tag.id,
      label: resolveLocalizedText(tag.labels, currentLanguage, tag.slug || tag.id)
    }))
    .sort((left: PublicVideoSelectOption, right: PublicVideoSelectOption) => left.label.localeCompare(right.label, currentLanguage));
}

export function buildVideoTypeOptions(): PublicVideoSelectOption[] {
  return PUBLIC_VIDEO_TYPES.map((type: VideoType) => ({
    value: type,
    label: getVideoTypeLabelKey(type)
  }));
}

export function buildPublicVideoNavigation(
  videos: readonly VideoDto[],
  currentVideoId: string,
  buildDetailLink: (video: VideoDto) => string[] | null
): { previous: PublicVideoNavigationItem | null; next: PublicVideoNavigationItem | null } {
  const currentIndex: number = videos.findIndex((video: VideoDto) => video.id === currentVideoId);

  if (currentIndex < 0) {
    return { previous: null, next: null };
  }

  const previousVideo: VideoDto | undefined = currentIndex > 0 ? videos[currentIndex - 1] : undefined;
  const nextVideo: VideoDto | undefined = currentIndex < videos.length - 1 ? videos[currentIndex + 1] : undefined;

  return {
    previous: previousVideo ? toNavigationItem(previousVideo, buildDetailLink(previousVideo)) : null,
    next: nextVideo ? toNavigationItem(nextVideo, buildDetailLink(nextVideo)) : null
  };
}

function buildPublicVideoCard(
  video: VideoDto,
  tagLookup: ReadonlyMap<string, PublicVideoTagViewModel>,
  currentLanguage: string,
  detailLink: string[] | null
): PublicVideoCardViewModel {
  const title: string = resolveVideoTitle(video, currentLanguage);
  const description: string | null = resolveVideoDescription(video, currentLanguage);

  return {
    id: video.id,
    title,
    description,
    creatorName: normalizeOptionalString(video.creatorName),
    creatorUrl: normalizeOptionalString(video.creatorUrl),
    provider: video.hostingProvider ?? VideoHostingProvider.OTHER,
    providerLabelKey: getVideoProviderLabelKey(video.hostingProvider),
    type: video.type ?? VideoType.OTHER,
    typeLabelKey: getVideoTypeLabelKey(video.type),
    durationLabel: formatDuration(video.durationSeconds),
    publishedAtLabel: formatPublishedAt(video.publishedAtUtc, currentLanguage),
    thumbnailPathOrUrl: normalizeOptionalString(video.thumbnailImageId) ?? normalizeOptionalString(video.thumbnailUrl),
    detailLink,
    tags: (video.tagIds ?? [])
      .map((tagId: string) => tagLookup.get(tagId))
      .filter((tag: PublicVideoTagViewModel | undefined): tag is PublicVideoTagViewModel => tag !== undefined)
  };
}

function buildVideoTagLookup(tags: readonly VideoTagDto[], currentLanguage: string): Map<string, PublicVideoTagViewModel> {
  return new Map<string, PublicVideoTagViewModel>(
    tags
      .filter((tag: VideoTagDto) => tag.isActive !== false)
      .map((tag: VideoTagDto) => [
        tag.id,
        {
          id: tag.id,
          slug: tag.slug,
          label: resolveLocalizedText(tag.labels, currentLanguage, tag.slug || tag.id)
        }
      ])
  );
}

function resolveVideoTitle(video: VideoDto, currentLanguage: string): string {
  return normalizeOptionalString(resolveLocalizedText(video.titles, currentLanguage, video.title || 'Video'))
    ?? normalizeOptionalString(video.title)
    ?? 'Video';
}

function resolveVideoDescription(video: VideoDto, currentLanguage: string): string | null {
  const localizedDescription: string = resolveLocalizedText(video.descriptions, currentLanguage, video.description ?? '');
  const normalizedDescription: string | null = normalizeOptionalString(stripHtml(localizedDescription));

  if (normalizedDescription) {
    return normalizedDescription;
  }

  return normalizeOptionalString(stripHtml(video.description));
}

function getVideoProviderLabelKey(provider: VideoHostingProvider | null | undefined): string {
  switch (provider) {
    case VideoHostingProvider.YOUTUBE:
      return 'videos.providers.youtube';
    case VideoHostingProvider.DAILYMOTION:
      return 'videos.providers.dailymotion';
    case VideoHostingProvider.VIMEO:
      return 'videos.providers.vimeo';
    default:
      return 'videos.providers.other';
  }
}

function getVideoTypeLabelKey(type: VideoType | null | undefined): string {
  switch (type) {
    case VideoType.ON_RIDE:
      return 'videos.types.onRide';
    case VideoType.OFF_RIDE:
      return 'videos.types.offRide';
    case VideoType.WALKTHROUGH:
      return 'videos.types.walkthrough';
    case VideoType.ADVERTISEMENT:
      return 'videos.types.advertisement';
    case VideoType.DOCUMENTARY:
      return 'videos.types.documentary';
    case VideoType.REVIEW:
      return 'videos.types.review';
    case VideoType.NEWS:
      return 'videos.types.news';
    case VideoType.EVENT:
      return 'videos.types.event';
    case VideoType.INTERVIEW:
      return 'videos.types.interview';
    default:
      return 'videos.types.other';
  }
}

function formatDuration(durationSeconds: number | null | undefined): string | null {
  if (durationSeconds === null || durationSeconds === undefined || !Number.isFinite(durationSeconds) || durationSeconds <= 0) {
    return null;
  }

  const totalSeconds: number = Math.round(durationSeconds);
  const hours: number = Math.floor(totalSeconds / 3600);
  const minutes: number = Math.floor((totalSeconds % 3600) / 60);
  const seconds: number = totalSeconds % 60;

  if (hours > 0) {
    return `${hours}:${padDurationPart(minutes)}:${padDurationPart(seconds)}`;
  }

  return `${minutes}:${padDurationPart(seconds)}`;
}

function formatPublishedAt(value: string | null | undefined, currentLanguage: string): string | null {
  const normalizedValue: string | null = normalizeOptionalString(value);

  if (!normalizedValue) {
    return null;
  }

  const date: Date = new Date(normalizedValue);

  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return new Intl.DateTimeFormat(currentLanguage || 'en', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(date);
}

function toNavigationItem(video: VideoDto, routerLink: string[] | null): PublicVideoNavigationItem {
  return {
    title: normalizeOptionalString(video.title) ?? 'Video',
    routerLink
  };
}

function padDurationPart(value: number): string {
  return value.toString().padStart(2, '0');
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : null;
}
