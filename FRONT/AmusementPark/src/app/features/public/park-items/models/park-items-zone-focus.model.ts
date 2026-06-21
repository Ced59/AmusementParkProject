import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemsCountTagViewModel } from './park-items-page-view.model';

export interface ParkItemsMapViewModel {
  center: [number, number];
  markers: MapMarker[];
  hasMarkers: boolean;
}

export interface ParkItemsZoneFocusViewModel {
  parkName: string;
  zoneId: string | null;
  zoneName: string;
  zoneDescription: string | null;
  heroImageId: string | null;
  totalItems: number;
  displayedItems: number;
  hasActiveZone: boolean;
  topTypeHighlights: ParkItemsCountTagViewModel[];
  map: ParkItemsMapViewModel;
  unlocatedItems: ParkItemsUnlocatedItemViewModel[];
}

export interface ParkItemsUnlocatedItemViewModel {
  id: string | null;
  name: string;
  categoryLabelKey: string;
  typeLabelKey: string;
  detailLink: string[] | null;
}
