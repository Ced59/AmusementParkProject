import { MapMarker } from '@app/models/map/map-marker';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { resolveLocationMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { ParkItemLocationPointViewModel } from '../models/park-item-detail-view.model';
import { formatCoordinates, isValidCoordinatePair } from './park-item-detail-formatters';

const DEFAULT_MAP_CENTER: [number, number] = [48.8566, 2.3522];

export function buildLocationPoints(item: ParkItem, currentLanguage: string): ParkItemLocationPointViewModel[] {
  const precisePoints: ParkItemLocationPointViewModel[] = buildPreciseLocationPoints(item.attractionLocations, currentLanguage);

  if (precisePoints.length > 0) {
    return precisePoints;
  }

  if (!isValidCoordinatePair(item.latitude, item.longitude)) {
    return [];
  }

  return [{
    id: 'general',
    labelKey: 'parkItems.locations.general',
    iconClass: 'pi pi-map-marker',
    latitude: item.latitude,
    longitude: item.longitude,
    coordinatesLabel: formatCoordinates(item.latitude, item.longitude, currentLanguage),
    isGeneralFallback: true
  }];
}

function buildPreciseLocationPoints(locations: AttractionLocations | null | undefined, currentLanguage: string): ParkItemLocationPointViewModel[] {
  const points: ParkItemLocationPointViewModel[] = [];

  pushLocationPoint(points, 'entrance', 'parkItems.locations.entrance', 'pi pi-sign-in', locations?.entrance, currentLanguage);
  pushLocationPoint(points, 'exit', 'parkItems.locations.exit', 'pi pi-sign-out', locations?.exit, currentLanguage);
  pushLocationPoint(points, 'fastPassEntrance', 'parkItems.locations.fastPassEntrance', 'pi pi-ticket', locations?.fastPassEntrance, currentLanguage);
  pushLocationPoint(points, 'reducedMobilityEntrance', 'parkItems.locations.reducedMobilityEntrance', 'pi pi-heart', locations?.reducedMobilityEntrance, currentLanguage);

  return points;
}

export function buildMapMarkers(points: ParkItemLocationPointViewModel[], itemName: string): MapMarker[] {
  return points.map((point: ParkItemLocationPointViewModel) => ({
    id: point.id,
    lat: point.latitude,
    lng: point.longitude,
    title: itemName,
    subtitle: point.coordinatesLabel,
    iconKind: resolveLocationMarkerIconKind(point.id),
    details: []
  }));
}

export function resolveMapCenter(points: ParkItemLocationPointViewModel[], item: ParkItem, park: Park | null): [number, number] {
  const firstPoint: ParkItemLocationPointViewModel | undefined = points[0];

  if (firstPoint) {
    return [firstPoint.latitude, firstPoint.longitude];
  }

  if (isValidCoordinatePair(item.latitude, item.longitude)) {
    return [item.latitude, item.longitude];
  }

  if (park && isValidCoordinatePair(park.latitude, park.longitude)) {
    return [park.latitude, park.longitude];
  }

  return DEFAULT_MAP_CENTER;
}

function pushLocationPoint(
  points: ParkItemLocationPointViewModel[],
  id: string,
  labelKey: string,
  iconClass: string,
  point: AttractionLocationPoint | null | undefined,
  currentLanguage: string
): void {
  if (!point || !isValidCoordinatePair(point.latitude, point.longitude)) {
    return;
  }

  points.push({
    id,
    labelKey,
    iconClass,
    latitude: point.latitude!,
    longitude: point.longitude!,
    coordinatesLabel: formatCoordinates(point.latitude!, point.longitude!, currentLanguage),
    isGeneralFallback: false
  });
}
