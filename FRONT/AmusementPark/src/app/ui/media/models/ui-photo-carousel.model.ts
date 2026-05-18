export interface UiPhotoCarouselImage {
  id: string;
  imageId: string;
  alt: string;
  categoryKey: string;
  categoryLabelKey: string;
  description: string | null;
  isCurrent?: boolean;
}

export interface UiPhotoCarouselCategoryOption {
  key: string;
  labelKey: string;
  count: number;
}
