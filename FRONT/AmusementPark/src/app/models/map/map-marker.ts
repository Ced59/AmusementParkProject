export type MapMarkerIconKind =
  | 'park'
  | 'site'
  | 'entrance'
  | 'exit'
  | 'fastPassEntrance'
  | 'accessibleEntrance'
  | 'attraction'
  | 'rollerCoaster'
  | 'waterRide'
  | 'darkRide'
  | 'flatRide'
  | 'familyRide'
  | 'thrillRide'
  | 'transportRide'
  | 'walkThrough'
  | 'playground'
  | 'interactiveExperience'
  | 'observationRide'
  | 'animal'
  | 'show'
  | 'restaurant'
  | 'snack'
  | 'shop'
  | 'hotel'
  | 'game'
  | 'meetAndGreet'
  | 'transport'
  | 'station'
  | 'toilets'
  | 'firstAid'
  | 'information'
  | 'locker'
  | 'parking'
  | 'service'
  | 'other';

export interface MapMarker {
  id: string;
  lat: number;
  lng: number;
  draggable?: boolean;
  title?: string | null;
  subtitle?: string | null;
  details?: string[];
  actionLabel?: string | null;
  actionUrl?: string | null;
  directionsActionEnabled?: boolean | null;
  detailActionLabel?: string | null;
  detailActionUrl?: string | null;
  detailActionRouteCommands?: string[] | null;
  iconKind?: MapMarkerIconKind | null;
}
