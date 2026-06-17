import { SafeResourceUrl } from '@angular/platform-browser';

import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoType } from '@app/models/videos/video-type';

export interface PublicVideoTagViewModel {
  id: string;
  slug: string;
  label: string;
}

export interface PublicVideoFilterState {
  type: VideoType | null;
  tagId: string | null;
  creatorName: string;
}

export interface PublicVideoSelectOption {
  value: string;
  label: string;
}

export interface PublicVideoCardViewModel {
  id: string;
  title: string;
  description: string | null;
  creatorName: string | null;
  creatorUrl: string | null;
  provider: VideoHostingProvider;
  providerLabelKey: string;
  type: VideoType;
  typeLabelKey: string;
  durationLabel: string | null;
  publishedAtLabel: string | null;
  thumbnailPathOrUrl: string | null;
  detailLink: string[] | null;
  tags: PublicVideoTagViewModel[];
}

export interface PublicVideoWatchViewModel extends PublicVideoCardViewModel {
  canonicalUrl: string;
  originalUrl: string;
  embedUrl: SafeResourceUrl | null;
}

export interface PublicVideoNavigationItem {
  title: string;
  routerLink: string[] | null;
}
