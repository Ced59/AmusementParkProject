import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SimpleChange, ViewEncapsulation } from '@angular/core';

import { LeafletMapComponent } from './leaflet-map.component';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies,
} from '@app/testing/common-test-providers';
import { MapMarker } from '@app/models/map/map-marker';

type LeafletTestMap = {
  fitBounds: Mock;
  getZoom: Mock;
  invalidateSize: Mock;
  setView: Mock;
  setZoom: Mock;
  remove: Mock;
};

type LeafletMapComponentInternals = {
  addSingleMarker: (marker: MapMarker) => void;
  handleMapClick: (event: {
    latlng: {
      lat: number;
      lng: number;
    };
  }) => void;
  handleMapViewportChange: () => void;
  focusSelectedMarker: () => boolean;
  openPendingSelectedMarkerPopup: () => void;
  buildTileLayerOptions: () => Record<string, unknown>;
  scheduleMarkerRefresh: (delayMs?: number) => void;
  scheduleViewportUpdate: () => void;
  scheduleMapSizeStabilization: () => void;
  applyDefaultViewport: () => void;
  fitMapToMarkersIfNeeded: () => boolean;
  ensureFitBoundsMinimumZoom: () => void;
  refreshMarkers: () => void;
  L: {
    latLngBounds: Mock;
  } | null;
  map: LeafletTestMap | null;
  tileLayer: {
    redraw: Mock;
  } | null;
  markerLayer: {
    clearLayers: Mock;
  } | null;
  leafletMarkers: Map<
    string,
    {
      getLatLng: Mock;
      openPopup: Mock;
    }
  >;
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
  internals: LeafletMapComponentInternals,
  marker: LeafletMarkerTestDouble,
): void {
  internals.L = {
    latLngBounds: vi.fn(),
    marker: vi.fn().mockReturnValue(marker),
    divIcon: vi.fn().mockReturnValue({}),
  } as unknown as LeafletMapComponentInternals['L'];
}

describe('LeafletMapComponent', () => {
  let component: LeafletMapComponent;
  let fixture: ComponentFixture<LeafletMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, LeafletMapComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(LeafletMapComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('uses unscoped component styles so Leaflet CSS can stay out of the initial global bundle', () => {
    expect(
      (
        LeafletMapComponent as unknown as {
          ɵcmp: {
            encapsulation: ViewEncapsulation;
          };
        }
      ).ɵcmp.encapsulation,
    ).toBe(ViewEncapsulation.None);
  });

  it('keeps the selected marker popup pending while focusing an already rendered marker', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const marker: {
      getLatLng: Mock;
      openPopup: Mock;
    } = {
      getLatLng: vi.fn().mockName('marker.getLatLng'),
      openPopup: vi.fn().mockName('marker.openPopup'),
    };
    const map: {
      fitBounds: Mock;
      getZoom: Mock;
      invalidateSize: Mock;
      setView: Mock;
      setZoom: Mock;
      remove: Mock;
    } = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    marker.getLatLng.mockReturnValue({ lat: 49.804, lng: 6.878 });
    map.getZoom.mockReturnValue(12);

    component.selectedMarkerId = 'entrance';
    internals.map = map;
    internals.leafletMarkers = new Map<
      string,
      {
        getLatLng: Mock;
        openPopup: Mock;
      }
    >([['entrance', marker]]);

    expect(internals.focusSelectedMarker()).toBe(true);
    expect(internals.pendingPopupMarkerId).toBe('entrance');
    expect(map.setView).toHaveBeenCalledWith({ lat: 49.804, lng: 6.878 }, 14, {
      animate: true,
    });
    expect(marker.openPopup).toHaveBeenCalled();
  });

  it('reopens the pending selected marker popup after marker refresh', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const marker: {
      getLatLng: Mock;
      openPopup: Mock;
    } = {
      getLatLng: vi.fn().mockName('marker.getLatLng'),
      openPopup: vi.fn().mockName('marker.openPopup'),
    };

    internals.pendingPopupMarkerId = 'entrance';
    internals.leafletMarkers = new Map<
      string,
      {
        getLatLng: Mock;
        openPopup: Mock;
      }
    >([['entrance', marker]]);

    internals.openPendingSelectedMarkerPopup();

    expect(marker.openPopup).toHaveBeenCalled();
    expect(internals.pendingPopupMarkerId).toBeNull();
  });

  it('binds and opens a directions-only marker popup when the marker is clicked', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const markerLayer: {
      clearLayers: Mock;
    } = {
      clearLayers: vi.fn().mockName('markerLayer.clearLayers'),
    };
    const testDouble = createLeafletMarkerTestDouble();
    const markerModel: MapMarker = {
      id: 'park-gps',
      lat: 48.85,
      lng: 2.35,
      actionUrl: 'https://maps.google.com/?daddr=48.85,2.35',
      actionLabel: 'Y aller',
    };
    const markerClickSpy: Mock = vi.fn();

    configureLeafletMarkerFactory(internals, testDouble.marker);
    internals.markerLayer = markerLayer;
    component.markerClick.subscribe(markerClickSpy);

    internals.addSingleMarker(markerModel);

    expect(testDouble.marker.bindPopup).toHaveBeenCalled();
    const popupContent: string = vi.mocked(testDouble.marker.bindPopup).mock
      .lastCall![0] as string;
    expect(popupContent).toContain('leaflet-map-popup__action--directions');
    expect(popupContent).toContain('Y aller');
    expect(popupContent).not.toContain('<strong></strong>');

    const clickHandlers: Array<(...args: unknown[]) => void> =
      testDouble.handlers.get('click') ?? [];
    expect(clickHandlers.length).toBe(1);

    clickHandlers[0]();

    expect(testDouble.marker.openPopup).toHaveBeenCalled();
    expect(markerClickSpy).toHaveBeenCalledWith(markerModel);
  });

  it('does not refresh markers after a viewport move for a simple map', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    component.markers = [{ id: 'park', lat: 48.85, lng: 2.35 }];
    component.stabilizeDynamicMarkerViewport = false;
    map.getZoom.mockReturnValue(13);
    internals.map = map;

    const scheduleMarkerRefreshSpy: Mock = vi.spyOn(
      internals,
      'scheduleMarkerRefresh',
    );

    internals.handleMapViewportChange();

    expect(scheduleMarkerRefreshSpy).not.toHaveBeenCalled();
  });

  it('refreshes markers after viewport moves for stabilized dynamic maps', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    component.markers = [{ id: 'park', lat: 48.85, lng: 2.35 }];
    component.stabilizeDynamicMarkerViewport = true;
    map.getZoom.mockReturnValue(13);
    internals.map = map;

    const scheduleMarkerRefreshSpy: Mock = vi.spyOn(
      internals,
      'scheduleMarkerRefresh',
    );

    internals.handleMapViewportChange();

    expect(scheduleMarkerRefreshSpy).toHaveBeenCalledWith(40);
  });

  it('clears pending marker popup when there is no selected marker', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: {
      fitBounds: Mock;
      getZoom: Mock;
      invalidateSize: Mock;
      setView: Mock;
      setZoom: Mock;
      remove: Mock;
    } = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    component.selectedMarkerId = null;
    internals.pendingPopupMarkerId = 'entrance';
    internals.map = map;

    expect(internals.focusSelectedMarker()).toBe(false);
    expect(internals.pendingPopupMarkerId).toBeNull();
  });

  it('uses reduced OpenStreetMap tile requests on mobile viewports', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(390);

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options).toEqual(
      expect.objectContaining({
        maxZoom: 19,
        detectRetina: false,
        keepBuffer: 0,
        updateWhenIdle: true,
        updateWhenZooming: false,
        tileSize: 512,
        zoomOffset: -1,
      }),
    );
  });

  it('keeps native tiles and a larger buffer for stabilized dynamic marker maps on mobile viewports', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(390);

    component.stabilizeDynamicMarkerViewport = true;

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options['tileSize']).toBeUndefined();
    expect(options['zoomOffset']).toBeUndefined();
    expect(options['keepBuffer']).toBe(2);
  });

  it('keeps native tile detail on wider viewports', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(1024);

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options['tileSize']).toBeUndefined();
    expect(options['zoomOffset']).toBeUndefined();
    expect(options['keepBuffer']).toBe(0);
  });

  it('clears stale markers and defers marker refresh until viewport update for stabilized fit-bounds maps', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };
    const markerLayer: {
      clearLayers: Mock;
    } = {
      clearLayers: vi.fn().mockName('markerLayer.clearLayers'),
    };
    const marker: {
      getLatLng: Mock;
      openPopup: Mock;
    } = {
      getLatLng: vi.fn().mockName('marker.getLatLng'),
      openPopup: vi.fn().mockName('marker.openPopup'),
    };

    component.fitBounds = true;
    component.stabilizeDynamicMarkerViewport = true;
    component.markers = [{ id: 'new', lat: 48.85, lng: 2.35 }];
    internals.L = { latLngBounds: vi.fn() };
    internals.map = map;
    internals.markerLayer = markerLayer;
    internals.leafletMarkers = new Map<
      string,
      {
        getLatLng: Mock;
        openPopup: Mock;
      }
    >([['old', marker]]);

    const refreshMarkersSpy: Mock = vi.spyOn(internals, 'refreshMarkers');
    vi.spyOn(internals, 'scheduleViewportUpdate');

    component.ngOnChanges({
      markers: new SimpleChange([], component.markers, false),
    });

    expect(markerLayer.clearLayers).toHaveBeenCalled();
    expect(internals.leafletMarkers.size).toBe(0);
    expect(refreshMarkersSpy).not.toHaveBeenCalled();
    expect(internals.scheduleViewportUpdate).toHaveBeenCalled();
  });

  it('redraws reduced mobile tiles while stabilizing the initial map size', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: {
      fitBounds: Mock;
      getZoom: Mock;
      invalidateSize: Mock;
      setView: Mock;
      setZoom: Mock;
      remove: Mock;
    } = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };
    const tileLayer: {
      redraw: Mock;
    } = {
      redraw: vi.fn().mockName('tileLayer.redraw'),
    };

    vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(390);
    map.getZoom.mockReturnValue(5);
    internals.map = map;
    internals.tileLayer = tileLayer;

    vi.useFakeTimers();
    try {
      internals.scheduleMapSizeStabilization();

      vi.advanceTimersByTime(1500);

      expect(map.invalidateSize).toHaveBeenCalledWith({
        pan: false,
        debounceMoveend: true,
      });
      expect(vi.mocked(tileLayer.redraw).mock.calls.length).toBe(5);
    } finally {
      vi.useRealTimers();
    }
  });

  it('refreshes stabilized marker rendering and tiles after viewport updates', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };
    const tileLayer: {
      redraw: Mock;
    } = {
      redraw: vi.fn().mockName('tileLayer.redraw'),
    };

    component.center = [46.8, 2.2];
    component.zoom = 6;
    component.stabilizeDynamicMarkerViewport = true;
    map.getZoom.mockReturnValue(5);
    internals.map = map;
    internals.tileLayer = tileLayer;

    const refreshMarkersSpy: Mock = vi.spyOn(internals, 'refreshMarkers');

    vi.useFakeTimers();
    try {
      internals.scheduleViewportUpdate();

      vi.advanceTimersByTime(1);

      expect(map.invalidateSize).toHaveBeenCalled();
      expect(map.setView).toHaveBeenCalledWith([46.8, 2.2], 6);
      expect(vi.mocked(refreshMarkersSpy).mock.calls.length).toBe(1);
      expect(vi.mocked(tileLayer.redraw).mock.calls.length).toBe(1);

      vi.advanceTimersByTime(120);

      expect(vi.mocked(refreshMarkersSpy).mock.calls.length).toBe(2);
      expect(vi.mocked(tileLayer.redraw).mock.calls.length).toBe(2);
    } finally {
      vi.useRealTimers();
    }
  });

  it('uses larger fit bounds padding and ignores invalid coordinates for stabilized dynamic marker maps', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const bounds: object = {};
    const map: LeafletTestMap = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    component.fitBounds = true;
    component.stabilizeDynamicMarkerViewport = true;
    component.markers = [
      { id: 'valid-1', lat: 48.85, lng: 2.35 },
      { id: 'invalid', lat: 120, lng: 2.35 },
      { id: 'valid-2', lat: 41.89, lng: 12.49 },
    ];
    internals.L = {
      latLngBounds: vi.fn().mockReturnValue(bounds),
    };
    internals.map = map;

    expect(internals.fitMapToMarkersIfNeeded()).toBe(true);
    expect(internals.L.latLngBounds).toHaveBeenCalledWith([
      [48.85, 2.35],
      [41.89, 12.49],
    ]);
    expect(map.fitBounds).toHaveBeenCalledWith(bounds, {
      padding: [72, 72],
      maxZoom: 8,
    });
  });

  it('falls back to the default viewport when fit-bounds markers have no usable coordinates', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    component.center = [46.8, 2.2];
    component.zoom = 6;
    component.fitBounds = true;
    component.markers = [{ id: 'invalid', lat: 120, lng: 2.35 }];
    internals.map = map;

    vi.spyOn(internals, 'scheduleViewportUpdate');

    internals.applyDefaultViewport();

    expect(internals.scheduleViewportUpdate).not.toHaveBeenCalled();
    expect(map.setView).toHaveBeenCalledWith([46.8, 2.2], 6);
  });

  it('emits editable map click positions inside Angular zone', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const ngZone = (
      component as unknown as {
        ngZone: {
          run(fn: () => void): void;
        };
      }
    ).ngZone;
    const positionChangeSpy: Mock = vi.fn();
    const zoneRunSpy: Mock = vi
      .spyOn(ngZone, 'run')
      .mockImplementation((fn: () => void): void => fn());

    component.editable = true;
    component.markers = [];
    component.positionChange.subscribe(positionChangeSpy);

    internals.handleMapClick({ latlng: { lat: 50.632, lng: 3.057 } });

    expect(zoneRunSpy).toHaveBeenCalled();
    expect(positionChangeSpy).toHaveBeenCalledWith({ lat: 50.632, lng: 3.057 });
  });

  it('keeps fitted bounds at the configured minimum zoom', () => {
    const internals: LeafletMapComponentInternals =
      component as unknown as LeafletMapComponentInternals;
    const map: {
      fitBounds: Mock;
      getZoom: Mock;
      invalidateSize: Mock;
      setView: Mock;
      setZoom: Mock;
      remove: Mock;
    } = {
      fitBounds: vi.fn().mockName('map.fitBounds'),
      getZoom: vi.fn().mockName('map.getZoom'),
      invalidateSize: vi.fn().mockName('map.invalidateSize'),
      setView: vi.fn().mockName('map.setView'),
      setZoom: vi.fn().mockName('map.setZoom'),
      remove: vi.fn().mockName('map.remove'),
    };

    component.fitBoundsMinZoom = 3;
    map.getZoom.mockReturnValue(2);
    internals.map = map;

    internals.ensureFitBoundsMinimumZoom();

    expect(map.setZoom).toHaveBeenCalledWith(3);
  });
});
