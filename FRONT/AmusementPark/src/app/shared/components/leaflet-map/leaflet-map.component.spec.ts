import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ViewEncapsulation } from '@angular/core';

import { LeafletMapComponent } from './leaflet-map.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

type LeafletMapComponentInternals = {
  focusSelectedMarker: () => boolean;
  openPendingSelectedMarkerPopup: () => void;
  buildTileLayerOptions: () => Record<string, unknown>;
  scheduleMapSizeStabilization: () => void;
  map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } | null;
  tileLayer: { redraw: jasmine.Spy } | null;
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
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'remove']);

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
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'remove']);

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

  it('keeps native tile detail on wider viewports', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    spyOnProperty(window, 'innerWidth', 'get').and.returnValue(1024);

    const options: Record<string, unknown> = internals.buildTileLayerOptions();

    expect(options['tileSize']).toBeUndefined();
    expect(options['zoomOffset']).toBeUndefined();
    expect(options['keepBuffer']).toBe(0);
  });

  it('redraws reduced mobile tiles while stabilizing the initial map size', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const map: { fitBounds: jasmine.Spy; getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['fitBounds', 'getZoom', 'invalidateSize', 'setView', 'remove']);
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
});
