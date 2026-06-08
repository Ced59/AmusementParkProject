import { MapMarker } from '@app/models/map/map-marker';

import { buildMarkerClusters, MarkerCluster, MarkerClusterPoint } from './leaflet-marker-cluster.builder';

describe('buildMarkerClusters', () => {
  it('clusters markers that share the same screen grid cell', () => {
    const clusters: MarkerCluster[] = buildMarkerClusters([
      createPoint('park-1', 10, 10),
      createPoint('park-2', 32, 30),
      createPoint('park-3', 140, 10)
    ], {
      radiusPx: 64,
      selectedMarkerId: null
    });

    expect(clusters.map((cluster: MarkerCluster) => cluster.count)).toEqual([2, 1]);
    expect(clusters[0].markers.map((marker: MapMarker) => marker.id)).toEqual(['park-1', 'park-2']);
  });

  it('keeps the selected marker isolated from its cluster', () => {
    const clusters: MarkerCluster[] = buildMarkerClusters([
      createPoint('park-1', 10, 10),
      createPoint('park-2', 32, 30),
      createPoint('park-3', 140, 10)
    ], {
      radiusPx: 64,
      selectedMarkerId: 'park-2'
    });

    expect(clusters.map((cluster: MarkerCluster) => cluster.count)).toEqual([1, 1, 1]);
    expect(clusters.find((cluster: MarkerCluster) => cluster.id === 'selected:park-2')?.markers[0].id).toBe('park-2');
  });

  it('uses the average marker coordinates for cluster placement', () => {
    const clusters: MarkerCluster[] = buildMarkerClusters([
      createPoint('park-1', 10, 10, 40, 2),
      createPoint('park-2', 32, 30, 42, 4)
    ], {
      radiusPx: 64,
      selectedMarkerId: null
    });

    expect(clusters[0].latitude).toBe(41);
    expect(clusters[0].longitude).toBe(3);
  });
});

function createPoint(id: string, x: number, y: number, lat: number = 50, lng: number = 3): MarkerClusterPoint {
  return {
    marker: {
      id,
      lat,
      lng,
      title: id,
      iconKind: 'park'
    },
    x,
    y
  };
}
