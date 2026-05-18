import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemCardViewModel } from './park-item-card.model';

export interface ParkItemDetailRowViewModel {
  labelKey: string;
  value: string;
  valueKey?: string | null;
  iconClass?: string | null;
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

export interface ParkItemAccessConditionViewModel {
  title: string | null;
  titleKey: string;
  description: string | null;
  rows: ParkItemDetailRowViewModel[];
}

export interface ParkItemDetailViewModel {
  name: string;
  categoryLabelKey: string;
  typeLabelKey: string;
  typeIconClass: string;
  typeTone: string;
  parkName: string | null;
  homeLink: string[];
  parkLink: string[] | null;
  itemsLink: string[] | null;
  description: string | null;
  manufacturerName: string | null;
  modelName: string | null;
  status: string | null;
  zoneName: string | null;
  subtype: string | null;
  spotlightRows: ParkItemDetailRowViewModel[];
  summaryRows: ParkItemDetailRowViewModel[];
  specGroups: ParkItemDetailSpecGroupViewModel[];
  accessConditions: ParkItemAccessConditionViewModel[];
  locationPoints: ParkItemLocationPointViewModel[];
  mapMarkers: MapMarker[];
  mapCenter: [number, number];
  mapZoom: number;
  hasPreciseLocations: boolean;
  relatedItems: ParkItemCardViewModel[];
}
