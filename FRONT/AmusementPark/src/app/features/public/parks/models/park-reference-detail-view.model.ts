import { UiPhotoCarouselCategoryOption, UiPhotoCarouselImage } from '@ui/media';

export type ParkReferenceKind = 'founder' | 'operator' | 'manufacturer';

export interface ParkReferenceFactViewModel {
  labelKey: string;
  value: string;
  iconClass: string;
  href?: string | null;
  isExternal?: boolean;
  isMonospace?: boolean;
}

export interface ParkReferenceAttractionViewModel {
  id: string;
  name: string;
  parkName: string;
  category: string;
  type: string;
  routerLink: string[] | null;
}

export interface ParkReferenceDetailViewModel {
  id: string | null;
  kind: ParkReferenceKind;
  name: string;
  legalName: string | null;
  richDescription: string | null;
  badgeKey: string;
  titleKey: string;
  descriptionTitleKey: string;
  emptyDescriptionKey: string;
  heroIconClass: string;
  facts: ParkReferenceFactViewModel[];
  photos: UiPhotoCarouselImage[];
  photoCategories: UiPhotoCarouselCategoryOption[];
  attractions: ParkReferenceAttractionViewModel[];
}
