export interface ParkItemSiblingNavigationItem {
  id: string;
  name: string;
}

export interface ParkItemSiblingNavigation {
  parkId: string;
  currentItemId: string;
  currentPosition: number;
  totalItems: number;
  remainingItems: number;
  previous: ParkItemSiblingNavigationItem | null;
  next: ParkItemSiblingNavigationItem | null;
}
