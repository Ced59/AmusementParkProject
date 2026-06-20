import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SimpleChange, ViewEncapsulation } from '@angular/core';

import { LeafletMapComponent } from './leaflet-map.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

type LeafletTestMap = {
  fitBounds: jasmine.Spy;
  getZoom: jasmine.Spy;
  invalidateSize: jasmine.Spy;
  setView: jasmine.Spy;
  setZoom: jasmine.Spy;
  remove: jasmine.Spy;
};

type LeafletMapComponentInternals = {
  focusSelectedMarker: () => boolean;
  openPendingSelectedMarkerPopup: () => void;
  buildTileLayerOptions: () => Record<string, unknown>;
  scheduleViewportUpdate: () => void;
  scheduleMapSizeStabilization: () => void;
  applyDefaultViewport: () => void;
  fitMapToMarkersIfNeeded: () => boolean;
  ensureFitBoundsMinimumZoom: () => void;
  refreshMarkers: () => void;
  L: { latLngBounds: jasmine.Spy } | null;
  map: LeafletTestMap | null;
  tileLayer: { redraw: jasmine.Spy } | null;
  markerLayer: { clearLayers: jasmine.Spy } | null;
  leafletMarkers: Map<string, { getLatLng: jasmine.Spy; openPopup: jasmine.Spy }>;
  pendingPopupMarkerId: string | null;
};

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
    expect((LeafletMapComponent as unknown as { ɵcmp: { encapsulation: ViewEncapsulation } }).ɵcmp.encapsulation)
      .toBe(ViewEncapsulation.None);
  });

  it('keeps the selected marker popup pending while focusing an already rendered marker', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const marker: { getLatLng: jasmine.Spy; openPopup: jasmine.Spy } = jasmine.createSpyObj('marker', ['getLatLng', 'openPopup']);
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; setZoom: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);

    marker.getLatLng.and.returnValue({ lat: 49.804, lng: 6.878 });
    map.getZoom.and.returnValue(12);

    component.selectedMarkerId = 'entrance';
    internals.map = map;
    internals.leafletMarkers = new Map<string, { getLatLng: jasmine.Spy; openPopup: jasmine.Spy }>([
      ['entrance', marker]
    ]);

    expect(internals.focusSelectedMarker()).toBeTrue();
    expect(internals.pendingPopupMarkerId).toBe('entrance');
    expect(map.setView).toHaveBeenCalledWith({ lat: 49.804, lng: 6.878 }, 14, { animate: true });
    expect(marker.openPopup).toHaveBeenCalled();
  });

  it('reopens the pending selected marker popup after marker refresh', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const marker: { getLatLng: jasmine.Spy; openPopup: jasmine.Spy } = jasmine.createSpyObj('marker', ['getLatLng', 'openPopup']);

    internals.pendingPopupMarkerId = 'entrance';
    internals.leafletMarkers = new Map<string, { getLatLng: jasmine.Spy; openPopup: jasmine.Spy }>([
      ['entrance', marker]
    ]);

    internals.openPendingSelectedMarkerPopup();

    expect(marker.openPopup).toHaveBeenCalled();
    expect(internals.pendingPopupMarkerId).toBeNull();
  });

  it('clears pending marker popup when there is no selected marker', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; setZoom: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);

    component.selectedMarkerId = null;
    internals.pendingPopupMarkerId = 'entrance';
    internals.map = map;

    expect(internals.focusSelectedMarker()).toBeFalse();
    expect(internals.pendingPopupMarkerId).toBeNull();
  });

  it('uses reduced OpenStreetMap tile requests on mobile viewports', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(390);

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options).toEqual(jasmine.objectContaining({
      maxZoom: 19,
      detectRetina: false,
      keepBuffer: 0,
      updateWhenIdle: true,
      updateWhenZooming: false,
      tileSize: 512,
      zoomOffset: -1
    }));
  });

  it('keeps native tiles and a larger buffer for stabilized dynamic marker maps on mobile viewports', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(390);

    component.stabilizeDynamicMarkerViewport = true;

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options['tileSize']).toBeUndefined();
    expect(options['zoomOffset']).toBeUndefined();
    expect(options['keepBuffer']).toBe(2);
  });

  it('keeps native tile detail on wider viewports', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(1024);

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options['tileSize']).toBeUndefined();
    expect(options['zoomOffset']).toBeUndefined();
    expect(options['keepBuffer']).toBe(0);
  });

  it('clears stale markers and defers marker refresh until viewport update for stabilized fit-bounds maps', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);
    const markerLayer: { clearLayers: jasmine.Spy } = jasmine.createSpyObj('markerLayer', ['clearLayers']);
    const marker: { getLatLng: jasmine.Spy; openPopup: jasmine.Spy } = jasmine.createSpyObj('marker', ['getLatLng', 'openPopup']);

    component.fitBounds = true;
    component.stabilizeDynamicMarkerViewport = true;
    component.markers = [{ id: 'new', lat: 48.85, lng: 2.35 }];
    internals.L = { latLngBounds: jasmine.createSpy('latLngBounds') };
    internals.map = map;
    internals.markerLayer = markerLayer;
    internals.leafletMarkers = new Map<string, { getLatLng: jasmine.Spy; openPopup: jasmine.Spy }>([
      ['old', marker]
    ]);

    const refreshMarkersSpy: jasmine.Spy = spyOn(internals, 'refreshMarkers');
    spyOn(internals, 'scheduleViewportUpdate');

    component.ngOnChanges({
      markers: new SimpleChange([], component.markers, false)
    });

    expect(markerLayer.clearLayers).toHaveBeenCalled();
    expect(internals.leafletMarkers.size).toBe(0);
    expect(refreshMarkersSpy).not.toHaveBeenCalled();
    expect(internals.scheduleViewportUpdate).toHaveBeenCalled();
  });

  it('redraws reduced mobile tiles while stabilizing the initial map size', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; setZoom: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);
    const tileLayer: { redraw: jasmine.Spy } = jasmine.createSpyObj('tileLayer', ['redraw']);

    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(390);
    map.getZoom.and.returnValue(5);
    internals.map = map;
    internals.tileLayer = tileLayer;

    jasmine.clock().install();
    try {
      internals.scheduleMapSizeStabilization();

      jasmine.clock().tick(1500);

      expect(map.invalidateSize).toHaveBeenCalledWith({ pan: false, debounceMoveend: true });
      expect(tileLayer.redraw.calls.count()).toBe(5);
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('refreshes stabilized marker rendering and tiles after viewport updates', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);
    const tileLayer: { redraw: jasmine.Spy } = jasmine.createSpyObj('tileLayer', ['redraw']);

    component.center = [46.8, 2.2];
    component.zoom = 6;
    component.stabilizeDynamicMarkerViewport = true;
    map.getZoom.and.returnValue(5);
    internals.map = map;
    internals.tileLayer = tileLayer;

    const refreshMarkersSpy: jasmine.Spy = spyOn(internals, 'refreshMarkers');

    jasmine.clock().install();
    try {
      internals.scheduleViewportUpdate();

      jasmine.clock().tick(1);

      expect(map.invalidateSize).toHaveBeenCalled();
      expect(map.setView).toHaveBeenCalledWith([46.8, 2.2], 6);
      expect(refreshMarkersSpy.calls.count()).toBe(1);
      expect(tileLayer.redraw.calls.count()).toBe(1);

      jasmine.clock().tick(120);

      expect(refreshMarkersSpy.calls.count()).toBe(2);
      expect(tileLayer.redraw.calls.count()).toBe(2);
    } finally {
      jasmine.clock().uninstall();
    }
  });

  it('uses larger fit bounds padding and ignores invalid coordinates for stabilized dynamic marker maps', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const bounds: object = {};
    const map: LeafletTestMap = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);

    component.fitBounds = true;
    component.stabilizeDynamicMarkerViewport = true;
    component.markers = [
      { id: 'valid-1', lat: 48.85, lng: 2.35 },
      { id: 'invalid', lat: 120, lng: 2.35 },
      { id: 'valid-2', lat: 41.89, lng: 12.49 }
    ];
    internals.L = {
      latLngBounds: jasmine.createSpy('latLngBounds').and.returnValue(bounds)
    };
    internals.map = map;

    expect(internals.fitMapToMarkersIfNeeded()).toBeTrue();
    expect(internals.L.latLngBounds).toHaveBeenCalledWith([
      [48.85, 2.35],
      [41.89, 12.49]
    ]);
    expect(map.fitBounds).toHaveBeenCalledWith(bounds, { padding: [72, 72], maxZoom: 8 });
  });

  it('falls back to the default viewport when fit-bounds markers have no usable coordinates', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: LeafletTestMap = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);

    component.center = [46.8, 2.2];
    component.zoom = 6;
    component.fitBounds = true;
    component.markers = [
      { id: 'invalid', lat: 120, lng: 2.35 }
    ];
    internals.map = map;

    spyOn(internals, 'scheduleViewportUpdate');

    internals.applyDefaultViewport();

    expect(internals.scheduleViewportUpdate).not.toHaveBeenCalled();
    expect(map.setView).toHaveBeenCalledWith([46.8, 2.2], 6);
  });

  it('keeps fitted bounds at the configured minimum zoom', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; setZoom: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'setZoom', 'remove']);

    component.fitBoundsMinZoom = 3;
    map.getZoom.and.returnValue(2);
    internals.map = map;

    internals.ensureFitBoundsMinimumZoom();

    expect(map.setZoom).toHaveBeenCalledWith(3);
  });
});
