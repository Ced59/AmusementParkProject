import { describe, expect, it, vi } from 'vitest';
import type { MapMarkerIconKind } from '@app/models/map/map-marker';
import { createLeafletMarkerClusterIcon, createLeafletMarkerIcon } from './leaflet-marker-icon.factory';

describe('Playground Leaflet marker presentation', () => {
  const kinds: MapMarkerIconKind[] = [
    'park', 'site', 'entrance', 'exit', 'fastPassEntrance', 'accessibleEntrance',
    'attraction', 'rollerCoaster', 'waterRide', 'darkRide', 'flatRide', 'familyRide',
    'thrillRide', 'transportRide', 'walkThrough', 'playground', 'interactiveExperience',
    'observationRide', 'animal', 'show', 'restaurant', 'snack', 'shop', 'hotel',
    'game', 'meetAndGreet', 'transport', 'station', 'photo', 'toilets', 'firstAid',
    'information', 'locker', 'parking', 'service', 'other'
  ];
  const stubLeaflet = () => {
    const divIcon = vi.fn((options: import('leaflet').DivIconOptions) => options as unknown as import('leaflet').DivIcon);
    return { divIcon, namespace: { divIcon } as unknown as typeof import('leaflet') };
  };

  for (const kind of kinds) {
    it(`keeps ${kind} identity, symbols and original hitbox/anchors with theme-linked color`, () => {
      const leaflet = stubLeaflet();
      createLeafletMarkerIcon(leaflet.namespace, kind);
      expect(leaflet.divIcon).toHaveBeenCalledOnce();
      const options = leaflet.divIcon.mock.calls[0][0];
      const location = kinds.indexOf(kind) < 6;
      expect(options.iconSize).toEqual(location ? [46, 58] : [42, 52]);
      expect(options.iconAnchor).toEqual(location ? [23, 58] : [21, 52]);
      expect(options.popupAnchor).toEqual(location ? [0, -54] : [0, -48]);
      expect(options.className).toContain(`app-leaflet-marker--${kind}`);
      expect(options.html).toContain(`data-marker-kind="${kind}"`);
      expect(options.html).toMatch(/--marker-color: var\(--(?:c-|app-)/);
      expect(options.html).toContain('aria-label=');
      expect(options.html).not.toMatch(/#[0-9a-f]{6}|<img/i);
    });
  }

  it('retains the default park identity without requesting the previous separate pictogram', () => {
    const leaflet = stubLeaflet();
    createLeafletMarkerIcon(leaflet.namespace, null);
    expect(leaflet.divIcon.mock.calls[0][0].html).toContain('🎢');
    expect(leaflet.divIcon.mock.calls[0][0].className).toContain('app-leaflet-marker--park');
  });

  it('keeps cluster counts, size thresholds and center anchor unchanged', () => {
    const leaflet = stubLeaflet();
    for (const [count, displayed, size] of [[1, 2, 'small'], [20, 20, 'medium'], [100, 100, 'large']] as const) {
      createLeafletMarkerClusterIcon(leaflet.namespace, count);
      const options = leaflet.divIcon.mock.calls.at(-1)![0];
      expect(options.iconSize).toEqual([52, 52]);
      expect(options.iconAnchor).toEqual([26, 26]);
      expect(options.className).toContain(`cluster--${size}`);
      expect(options.html).toContain(`>${displayed}</span>`);
    }
  });
});
