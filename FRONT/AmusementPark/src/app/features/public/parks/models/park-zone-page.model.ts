import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemCardViewModel } from '../../park-items/models/park-item-card.model';
import { ParkItemsCountTagViewModel } from '../../park-items/models/park-items-page-view.model';

export interface ParkZoneOverviewCardViewModel {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  totalItems: number;
  typeHighlights: ParkItemsCountTagViewModel[];
  zoneLink: string[] | null;
  itemsLink: string[] | null;
  itemsQueryParams: Record<string, string> | null;
}

export interface ParkZonesPageViewModel {
  parkName: string;
  parkLink: string[] | null;
  itemsLink: string[] | null;
  zoneCount: number;
  totalItems: number;
  zones: ParkZoneOverviewCardViewModel[];
}

export interface ParkZoneMapViewModel {
  center: [number, number];
  markers: MapMarker[];
  hasMarkers: boolean;
}

export interface ParkZonePageViewModel {
  parkName: string;
  parkLink: string[] | null;
  zonesLink: string[] | null;
  allItemsLink: string[] | null;
  allItemsQueryParams: Record<string, string> | null;
  zoneName: string;
  zoneDescription: string | null;
  totalItems: number;
  typeHighlights: ParkItemsCountTagViewModel[];
  map: ParkZoneMapViewModel;
  items: ParkItemCardViewModel[];
}
