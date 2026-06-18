export interface UiPhotoCarouselImage {
  id: string;
  imageId: string;
  alt: string;
  categoryKey: string;
  categoryLabelKey: string;
  description: string | null;
  isCurrent?: boolean;
  caption?: string | null;
  credit?: string | null;
  takenOn?: string | null;
  uploadedAt?: string | null;
  updatedAt?: string | null;
  year: string;
  yearLabel: string;
  yearLabelKey?: string | null;
  tagKeys: string[];
  tagLabels: UiPhotoCarouselTagLabel[];
  latitude?: number | null;
  longitude?: number | null;
  width?: number | null;
  height?: number | null;
  sizeInBytes?: number | null;
  originalFileName?: string | null;
  contentType?: string | null;
  cameraMaker?: string | null;
  cameraModel?: string | null;
  focalLength?: number | null;
  aperture?: number | null;
  exposureTime?: number | null;
  iso?: number | null;
  orientation?: string | null;
  sourceTitle?: string | null;
  sourceSubtitle?: string | null;
  sourceIconClass?: string | null;
  sourceRouterLink?: string[] | null;
  sourceLinkLabelKey?: string | null;
  externalSourceUrl?: string | null;
}

export interface UiPhotoCarouselCategoryOption {
  key: string;
  labelKey: string;
  count: number;
}

export interface UiPhotoCarouselTagLabel {
  key: string;
  label: string;
  labelKey?: string | null;
}

export interface UiPhotoCarouselAxisOption {
  key: string;
  label: string;
  labelKey?: string | null;
  count: number;
}

export interface UiPhotoCarouselMetadataRow {
  key: string;
  labelKey: string;
  value: string;
  iconClass: string;
}
