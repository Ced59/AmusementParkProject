import { MapMarker } from '@app/models/map/map-marker';

export interface ParkItemsMapFilterOptionViewModel {
  key: string | null;
  labelKey?: string | null;
  labelText?: string | null;
  iconClass?: string | null;
  count: number;
}

export interface ParkItemsMapMarkerViewModel extends MapMarker {
  itemId: string | null;
  itemName: string;
  category: string;
  zoneId: string | null;
}

export interface ParkItemsMapViewModel {
  parkId: string | null;
  parkName: string | null;
  language: string;
  center: [number, number];
  markers: ParkItemsMapMarkerViewModel[];
  unlocatedItems: ParkItemsMapUnlocatedItemViewModel[];
  categoryFilters: ParkItemsMapFilterOptionViewModel[];
  zoneFilters: ParkItemsMapFilterOptionViewModel[];
  hasItemMarkers: boolean;
}

export interface ParkItemsMapUnlocatedItemViewModel {
  id: string | null;
  name: string;
  categoryLabelKey: string;
  typeLabelKey: string;
  detailLink: string[] | null;
}
