import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapMarker } from '@app/models/map/map-marker';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { LeafletStaticMapComponent } from './leaflet-static-map.component';

type LeafletStaticMapComponentInternals = {
  addMarker: (marker: MapMarker) => void;
  focusSelectedMarker: () => boolean;
  renderMarkers: () => void;
  L: { marker: jasmine.Spy; divIcon: jasmine.Spy; latLngBounds: jasmine.Spy } | null;
  map: { getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; remove: jasmine.Spy; setView: jasmine.Spy } | null;
  markerLayer: { clearLayers: jasmine.Spy } | null;
  leafletMarkers: Map<string, LeafletMarkerTestDouble>;
  openPopupMarkerId: string | null;
  pendingPopupMarkerId: string | null;
};

type LeafletMarkerTestDouble = {
  addTo: jasmine.Spy;
  bindPopup: jasmine.Spy;
  getLatLng: jasmine.Spy;
  getPopup: jasmine.Spy;
  on: jasmine.Spy;
  openPopup: jasmine.Spy;
};

type LeafletMarkerHandlers = Map<string, Array<(...args: unknown[]) => void>>;

function createLeafletMarkerTestDouble(): { marker: LeafletMarkerTestDouble; handlers: LeafletMarkerHandlers } {
  const marker: LeafletMarkerTestDouble = jasmine.createSpyObj('leafletMarker', [
    'addTo',
    'bindPopup',
    'getLatLng',
    'getPopup',
    'on',
    'openPopup'
  ]);
  const handlers: LeafletMarkerHandlers = new Map<string, Array<(...args: unknown[]) => void>>();

  marker.addTo.and.returnValue(marker);
  marker.bindPopup.and.returnValue(marker);
  marker.getLatLng.and.returnValue({ lat: 48.85, lng: 2.35 });
  marker.getPopup.and.returnValue({});
  marker.on.and.callFake((eventName: string, handler: (...args: unknown[]) => void): LeafletMarkerTestDouble => {
    const existingHandlers: Array<(...args: unknown[]) => void> = handlers.get(eventName) ?? [];
    handlers.set(eventName, [...existingHandlers, handler]);
    return marker;
  });

  return { marker, handlers };
}

function configureLeafletMarkerFactory(internals: LeafletStaticMapComponentInternals, marker: LeafletMarkerTestDouble): void {
  internals.L = {
    marker: jasmine.createSpy('marker').and.returnValue(marker),
    divIcon: jasmine.createSpy('divIcon').and.returnValue({}),
    latLngBounds: jasmine.createSpy('latLngBounds')
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
    const internals: LeafletStaticMapComponentInternals = component as unknown as LeafletStaticMapComponentInternals;
    const markerLayer: { clearLayers: jasmine.Spy } = jasmine.createSpyObj('markerLayer', ['clearLayers']);
    const testDouble = createLeafletMarkerTestDouble();
    const markerModel: MapMarker = {
      id: 'park-location',
      lat: 48.85,
      lng: 2.35,
      actionUrl: 'https://maps.google.com/?daddr=48.85,2.35',
      actionLabel: 'Y aller'
    };
    const markerClickSpy: jasmine.Spy = jasmine.createSpy('markerClick');

    configureLeafletMarkerFactory(internals, testDouble.marker);
    internals.markerLayer = markerLayer;
    component.markerClick.subscribe(markerClickSpy);

    internals.addMarker(markerModel);

    expect(testDouble.marker.bindPopup).toHaveBeenCalled();
    const popupContent: string = testDouble.marker.bindPopup.calls.mostRecent().args[0] as string;
    expect(popupContent).toContain('leaflet-map-popup__action--directions');
    expect(popupContent).toContain('Y aller');

    const clickHandlers: Array<(...args: unknown[]) => void> = testDouble.handlers.get('click') ?? [];
    expect(clickHandlers.length).toBe(1);

    clickHandlers[0]();

    expect(testDouble.marker.openPopup).toHaveBeenCalled();
    expect(markerClickSpy).toHaveBeenCalledWith(markerModel);
  });

  it('keeps the directions popup open when marker inputs are rebuilt after a click', () => {
    const internals: LeafletStaticMapComponentInternals = component as unknown as LeafletStaticMapComponentInternals;
    const markerLayer: { clearLayers: jasmine.Spy } = jasmine.createSpyObj('markerLayer', ['clearLayers']);
    const testDouble = createLeafletMarkerTestDouble();
    const markerModel: MapMarker = {
      id: 'park-location',
      lat: 48.85,
      lng: 2.35,
      actionUrl: 'https://maps.google.com/?daddr=48.85,2.35',
      actionLabel: 'Y aller'
    };

    configureLeafletMarkerFactory(internals, testDouble.marker);
    internals.markerLayer = markerLayer;
    component.markers = [markerModel];

    internals.renderMarkers();

    const clickHandlers: Array<(...args: unknown[]) => void> = testDouble.handlers.get('click') ?? [];
    expect(clickHandlers.length).toBe(1);

    clickHandlers[0]();

    expect(testDouble.marker.openPopup.calls.count()).toBe(1);
    expect(internals.openPopupMarkerId).toBe('park-location');

    internals.renderMarkers();

    expect(markerLayer.clearLayers.calls.count()).toBe(2);
    expect(testDouble.marker.openPopup.calls.count()).toBe(2);
    expect(internals.pendingPopupMarkerId).toBeNull();
    expect(internals.openPopupMarkerId).toBe('park-location');
  });

  it('opens the selected marker popup without rebuilding markers', () => {
    const internals: LeafletStaticMapComponentInternals = component as unknown as LeafletStaticMapComponentInternals;
    const map: { getZoom: jasmine.Spy; invalidateSize: jasmine.Spy; remove: jasmine.Spy; setView: jasmine.Spy } = jasmine.createSpyObj('map', ['getZoom', 'invalidateSize', 'remove', 'setView']);
    const testDouble = createLeafletMarkerTestDouble();

    component.selectedMarkerId = 'entrance';
    map.getZoom.and.returnValue(12);
    internals.map = map;
    internals.leafletMarkers = new Map<string, LeafletMarkerTestDouble>([
      ['entrance', testDouble.marker]
    ]);

    expect(internals.focusSelectedMarker()).toBeTrue();
    expect(map.setView).toHaveBeenCalledWith({ lat: 48.85, lng: 2.35 }, 14, { animate: true });
    expect(testDouble.marker.openPopup).toHaveBeenCalled();
    expect(internals.openPopupMarkerId).toBe('entrance');
  });
});
