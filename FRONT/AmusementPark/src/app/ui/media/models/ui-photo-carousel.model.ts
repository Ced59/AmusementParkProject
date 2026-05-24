export interface UiPhotoCarouselImage {
  id: string;
  imageId: string;
  alt: string;
  categoryKey: string;
  categoryLabelKey: string;
  description: string | null;
  isCurrent?: boolean;
  sourceTitle?: string | null;
  sourceSubtitle?: string | null;
  sourceIconClass?: string | null;
  sourceRouterLink?: string[] | null;
  sourceLinkLabelKey?: string | null;
}

export interface UiPhotoCarouselCategoryOption {
  key: string;
  labelKey: string;
  count: number;
}
