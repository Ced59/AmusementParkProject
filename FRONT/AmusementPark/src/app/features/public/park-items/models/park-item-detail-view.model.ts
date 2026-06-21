import { MapMarker } from '@app/models/map/map-marker';
import { ImageCategory } from '@app/models/images/image-category';
import { RatingSummary } from '@app/models/ratings/rating.models';
import { UiPhotoCarouselImage } from '@ui/media';
import { ParkItemCardViewModel } from './park-item-card.model';

export interface ParkItemDetailRowViewModel {
  labelKey: string;
  value: string;
  valueKey?: string | null;
  iconClass?: string | null;
  isTextualValue?: boolean;
  routerLink?: string[] | null;
  queryParams?: Record<string, string> | null;
}

export interface ParkItemDetailNavigationLinkViewModel {
  routerLink: string[];
  queryParams?: Record<string, string> | null;
}

export interface ParkItemDetailSiblingNavigationItemViewModel {
  name: string;
  routerLink: string[];
}

export interface ParkItemDetailSiblingNavigationViewModel {
  currentPosition: number;
  totalItems: number;
  remainingItems: number;
  previous: ParkItemDetailSiblingNavigationItemViewModel | null;
  next: ParkItemDetailSiblingNavigationItemViewModel | null;
}

export interface ParkItemDetailSpecGroupViewModel {
  titleKey: string;
  iconClass: string;
  rows: ParkItemDetailRowViewModel[];
}

export interface ParkItemLocationPointViewModel {
  id: string;
  labelKey: string;
  iconClass: string;
  latitude: number;
  longitude: number;
  coordinatesLabel: string;
  isGeneralFallback: boolean;
}

export interface ParkItemPhotoCategoryOptionViewModel {
  key: string;
  labelKey: string;
  count: number;
}

export interface ParkItemPhotoViewModel extends UiPhotoCarouselImage {
  category: ImageCategory;
}

export interface ParkItemAccessConditionMetricViewModel {
  labelKey: string;
  value: string;
  helperKey: string | null;
  iconClass: string;
}

export interface ParkItemAccessConditionViewModel {
  title: string | null;
  titleKey: string;
  description: string | null;
  rows: ParkItemDetailRowViewModel[];
  metrics: ParkItemAccessConditionMetricViewModel[];
  kind: 'height' | 'restriction' | 'default';
  iconClass: string;
  tone: string;
}

export interface ParkItemDetailViewModel {
  id: string | null;
  parkId: string | null;
  name: string;
  categoryLabelKey: string;
  typeLabelKey: string;
  typeIconClass: string;
  typeTone: string;
  parkName: string | null;
  homeLink: string[];
  parkLink: string[] | null;
  itemsLink: string[] | null;
  imagesLink: string[] | null;
  videosLink: string[] | null;
  siblingNavigation: ParkItemDetailSiblingNavigationViewModel | null;
  categoryNavigation: ParkItemDetailNavigationLinkViewModel | null;
  typeNavigation: ParkItemDetailNavigationLinkViewModel | null;
  subtypeNavigation: ParkItemDetailNavigationLinkViewModel | null;
  zoneNavigation: ParkItemDetailNavigationLinkViewModel | null;
  description: string | null;
  rating: RatingSummary | null;
  manufacturerName: string | null;
  modelName: string | null;
  status: string | null;
  zoneName: string | null;
  subtype: string | null;
  spotlightRows: ParkItemDetailRowViewModel[];
  summaryRows: ParkItemDetailRowViewModel[];
  specGroups: ParkItemDetailSpecGroupViewModel[];
  heroPhoto: ParkItemPhotoViewModel | null;
  accessConditions: ParkItemAccessConditionViewModel[];
  locationPoints: ParkItemLocationPointViewModel[];
  mapMarkers: MapMarker[];
  mapCenter: [number, number];
  mapZoom: number;
  hasPreciseLocations: boolean;
  relatedItems: ParkItemCardViewModel[];
}
