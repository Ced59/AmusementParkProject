import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapMarker } from '@app/models/map/map-marker';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { LeafletStaticMapComponent } from './leaflet-static-map.component';

type LeafletStaticMapComponentInternals = {
  addMarker: (marker: MapMarker) => void;
  focusSelectedMarker: () => boolean;
  renderMarkers: () => void;
  L: {
    marker: Mock;
    divIcon: Mock;
    latLngBounds: Mock;
  } | null;
  map: {
    getZoom: Mock;
    invalidateSize: Mock;
    remove: Mock;
    setView: Mock;
  } | null;
  markerLayer: {
    clearLayers: Mock;
  } | null;
  leafletMarkers: Map<string, LeafletMarkerTestDouble>;
  openPopupMarkerId: string | null;
  pendingPopupMarkerId: string | null;
};

type LeafletMarkerTestDouble = {
  addTo: Mock;
  bindPopup: Mock;
  getLatLng: Mock;
  getPopup: Mock;
  on: Mock;
  openPopup: Mock;
};

type LeafletMarkerHandlers = Map<string, Array<(...args: unknown[]) => void>>;

function createLeafletMarkerTestDouble(): {
  marker: LeafletMarkerTestDouble;
  handlers: LeafletMarkerHandlers;
} {
  const marker: LeafletMarkerTestDouble = {
    addTo: vi.fn().mockName('leafletMarker.addTo'),
    bindPopup: vi.fn().mockName('leafletMarker.bindPopup'),
    getLatLng: vi.fn().mockName('leafletMarker.getLatLng'),
    getPopup: vi.fn().mockName('leafletMarker.getPopup'),
    on: vi.fn().mockName('leafletMarker.on'),
    openPopup: vi.fn().mockName('leafletMarker.openPopup'),
  };
  const handlers: LeafletMarkerHandlers = new Map<
    string,
    Array<(...args: unknown[]) => void>
  >();

  marker.addTo.mockReturnValue(marker);
  marker.bindPopup.mockReturnValue(marker);
  marker.getLatLng.mockReturnValue({ lat: 48.85, lng: 2.35 });
  marker.getPopup.mockReturnValue({});
  marker.on.mockImplementation(
    (
      eventName: string,
      handler: (...args: unknown[]) => void,
    ): LeafletMarkerTestDouble => {
      const existingHandlers: Array<(...args: unknown[]) => void> =
        handlers.get(eventName) ?? [];
      handlers.set(eventName, [...existingHandlers, handler]);
      return marker;
    },
  );

  return { marker, handlers };
}

function configureLeafletMarkerFactory(
  internals: LeafletStaticMapComponentInternals,
  marker: LeafletMarkerTestDouble,
): void {
  internals.L = {
    marker: vi.fn().mockReturnValue(marker),
    divIcon: vi.fn().mockReturnValue({}),
    latLngBounds: vi.fn(),
  };
}

describe('LeafletStaticMapComponent', () => {
  let component: LeafletStaticMapComponent;
  let fixture: ComponentFixture<LeafletStaticMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, LeafletStaticMapComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(LeafletStaticMapComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('binds and opens a directions popup when the marker is clicked', () => {
    const internals: LeafletStaticMapComponentInternals =
      component as unknown as LeafletStaticMapComponentInternals;
    const markerLayer: {
      clearLayers: Mock;
    } = {
      clearLayers: vi.fn().mockName('markerLayer.clearLayers'),
    };
    const testDouble = createLeafletMarkerTestDouble();
    const markerModel: MapMarker = {
      id: 'park-location',
      lat: 48.85,
      lng: 2.35,
      actionUrl: 'https://maps.google.com/?daddr=48.85,2.35',
      actionLabel: 'Y aller',
    };
    const markerClickSpy: Mock = vi.fn();

    configureLeafletMarkerFactory(internals, testDouble.marker);
    internals.markerLayer = markerLayer;
    component.markerClick.subscribe(markerClickSpy);

    internals.addMarker(markerModel);

    expect(testDouble.marker.bindPopup).toHaveBeenCalled();
    const popupContent: string = vi.mocked(testDouble.marker.bindPopup).mock
      .lastCall![0] as string;
    expect(popupContent).toContain('leaflet-map-popup__action--directions');
    expect(popupContent).toContain('Y aller');

    const clickHandlers: Array<(...args: unknown[]) => void> =
      testDouble.handlers.get('click') ?? [];
    expect(clickHandlers.length).toBe(1);

    clickHandlers[0]();

    expect(testDouble.marker.openPopup).toHaveBeenCalled();
    expect(markerClickSpy).toHaveBeenCalledWith(markerModel);
  });

  it('keeps the directions popup open when marker inputs are rebuilt after a click', () => {
    const internals: LeafletStaticMapComponentInternals =
      component as unknown as LeafletStaticMapComponentInternals;
    const markerLayer: {
      clearLayers: Mock;
    } = {
      clearLayers: vi.fn().mockName('markerLayer.clearLayers'),
    };
    const testDouble = createLeafletMarkerTestDouble();
    const markerModel: MapMarker = {
      id: 'park-location',
      lat: 48.85,
      lng: 2.35,
      actionUrl: 'https://maps.google.com/?daddr=48.85,2.35',
      actionLabel: 'Y aller',
    };

    configureLeafletMarkerFactory(internals, testDouble.marker);
    internals.markerLayer = markerLayer;
    component.markers = [markerModel];

    internals.renderMarkers();

    const clickHandlers: Array<(...args: unknown[]) => void> =
      testDouble.handlers.get('click') ?? [];
    expect(clickHandlers.length).toBe(1);

    clickHandlers[0]();

    expect(vi.mocked(testDouble.marker.openPopup).mock.calls.length).toBe(1);
    expect(internals.openPopupMarkerId).toBe('park-location');

    internals.renderMarkers();

    expect(vi.mocked(markerLayer.clearLayers).mock.calls.length).toBe(2);
    expect(vi.mocked(testDouble.marker.openPopup).mock.calls.length).toBe(2);
    expect(internals.pendingPopupMarkerId).toBeNull();
    expect(internals.openPopupMarkerId).toBe('park-location');
  });

  it('opens the selected marker popup without rebuilding markers', () => {
    const internals: LeafletStaticMapComponentInternals =
      component as unknown as LeafletStaticMapComponentInternals;
    const map: {
      getZoom: Mock;
      invalidateSize: Mock;
      remove: Mock;
      setView: Mock;
    } = {
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      remove: vi.fn().mockName('map.remove'),
      setView: vi.fn().mockName('map.setView'),
    };
    const testDouble = createLeafletMarkerTestDouble();

    component.selectedMarkerId = 'entrance';
    map.getZoom.mockReturnValue(12);
    internals.map = map;
    internals.leafletMarkers = new Map<string, LeafletMarkerTestDouble>([
      ['entrance', testDouble.marker],
    ]);

    expect(internals.focusSelectedMarker()).toBe(true);
    expect(map.setView).toHaveBeenCalledWith({ lat: 48.85, lng: 2.35 }, 14, {
      animate: true,
    });
    expect(testDouble.marker.openPopup).toHaveBeenCalled();
    expect(internals.openPopupMarkerId).toBe('entrance');
  });
});
