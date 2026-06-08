import { MapMarker } from '@app/models/map/map-marker';

export interface MarkerClusterPoint {
  readonly marker: MapMarker;
  readonly x: number;
  readonly y: number;
}

export interface MarkerClusterOptions {
  readonly radiusPx: number;
  readonly selectedMarkerId: string | null;
}

export interface MarkerCluster {
  readonly id: string;
  readonly markers: MapMarker[];
  readonly latitude: number;
  readonly longitude: number;
  readonly count: number;
}

interface MutableMarkerCluster {
  readonly id: string;
  readonly markers: MapMarker[];
  latitudeTotal: number;
  longitudeTotal: number;
}

export function buildMarkerClusters(points: MarkerClusterPoint[], options: MarkerClusterOptions): MarkerCluster[] {
  if (points.length === 0) {
    return [];
  }

  const radiusPx: number = Math.max(1, options.radiusPx);
  const buckets: Map<string, MutableMarkerCluster> = new Map<string, MutableMarkerCluster>();

  for (const point of points) {
    const bucketKey: string = point.marker.id === options.selectedMarkerId
      ? `selected:${point.marker.id}`
      : `${Math.floor(point.x / radiusPx)}:${Math.floor(point.y / radiusPx)}`;
    let bucket: MutableMarkerCluster | undefined = buckets.get(bucketKey);

    if (!bucket) {
      bucket = {
        id: bucketKey,
        markers: [],
        latitudeTotal: 0,
        longitudeTotal: 0
      };
      buckets.set(bucketKey, bucket);
    }

    bucket.markers.push(point.marker);
    bucket.latitudeTotal += point.marker.lat;
    bucket.longitudeTotal += point.marker.lng;
  }

  return Array.from(buckets.values()).map((bucket: MutableMarkerCluster): MarkerCluster => {
    const count: number = bucket.markers.length;

    return {
      id: bucket.id,
      markers: bucket.markers,
      latitude: bucket.latitudeTotal / count,
      longitude: bucket.longitudeTotal / count,
      count
    };
  });
}
