export interface ParkItemsCountTagViewModel {
  value: string;
  labelKey: string;
  count: number;
}

export interface ParkItemZoneCardViewModel {
  id: string | null;
  name: string;
  totalItems: number;
  typeHighlights: ParkItemsCountTagViewModel[];
  isSelected: boolean;
}

export interface ParkItemsPageViewModel {
  parkId: string | null;
  parkName: string;
  backLink: string[] | null;
  totalItems: number;
  totalResults: number;
  zoneCount: number;
  hasZones: boolean;
  activeZoneLabel: string | null;
  topTypeHighlights: ParkItemsCountTagViewModel[];
}
