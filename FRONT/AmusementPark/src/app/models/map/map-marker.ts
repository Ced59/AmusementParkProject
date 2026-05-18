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
}
