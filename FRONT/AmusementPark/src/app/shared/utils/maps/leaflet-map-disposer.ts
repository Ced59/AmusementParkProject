import type { Map as LeafletMap } from 'leaflet';

/** Keeps Leaflet's pane alive until the last public zoomend dispatch finishes. */
export function createLeafletMapDisposer(map: Pick<LeafletMap, 'on' | 'getPane' | 'remove'>): () => void {
  let zoomInProgress = false;
  let zoomGeneration = 0;
  let removalRequested = false;
  let removed = false;

  const removeIfReady = (): void => {
    if (!removalRequested || zoomInProgress || removed) {
      return;
    }
    removed = true;
    map.remove();
  };

  map.on('zoomstart', (): void => {
    zoomGeneration++;
    zoomInProgress = true;
  });
  map.on('zoomend', (): void => {
    const completedGeneration: number = zoomGeneration;
    queueMicrotask((): void => {
      // A listener may start another zoom during the same event dispatch.
      if (completedGeneration !== zoomGeneration) {
        return;
      }
      zoomInProgress = false;
      removeIfReady();
    });
  });

  return (): void => {
    if (removalRequested || removed) {
      return;
    }
    removalRequested = true;
    zoomInProgress = zoomInProgress || Boolean(map.getPane('mapPane')?.classList.contains('leaflet-zoom-anim'));
    removeIfReady();
  };
}
