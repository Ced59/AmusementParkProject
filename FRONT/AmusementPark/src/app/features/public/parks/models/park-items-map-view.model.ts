import { MapMarker } from '@app/models/map/map-marker';

export interface ParkItemsMapFilterOptionViewModel {
  key: string | null;
  labelKey?: string | null;
  labelText?: string | null;
  iconClass?: string | null;
  count: number;
}

export interface ParkItemsMapMarkerViewModel extends MapMarker {
  category: string;
  zoneId: string | null;
}

export interface ParkItemsMapViewModel {
  center: [number, number];
  markers: ParkItemsMapMarkerViewModel[];
  categoryFilters: ParkItemsMapFilterOptionViewModel[];
  zoneFilters: ParkItemsMapFilterOptionViewModel[];
  hasItemMarkers: boolean;
}
