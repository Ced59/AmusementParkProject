import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeafletMapComponent } from './leaflet-map.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

type LeafletMapComponentInternals = {
  focusSelectedMarker: () => boolean;
  openPendingSelectedMarkerPopup: () => void;
  map: { getZoom: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } | null;
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

  it('keeps the selected marker popup pending while focusing an already rendered marker', () => {
    const internals: LeafletMapComponentInternals = component as unknown as LeafletMapComponentInternals;
    const marker: { getLatLng: jasmine.Spy; openPopup: jasmine.Spy } = jasmine.createSpyObj('marker', ['getLatLng', 'openPopup']);
    const map: { getZoom: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['getZoom', 'setView', 'remove']);

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
    const map: { getZoom: jasmine.Spy; setView: jasmine.Spy; remove: jasmine.Spy } = jasmine.createSpyObj('map', ['getZoom', 'setView', 'remove']);

    component.selectedMarkerId = null;
    internals.pendingPopupMarkerId = 'entrance';
    internals.map = map;

    expect(internals.focusSelectedMarker()).toBeFalse();
    expect(internals.pendingPopupMarkerId).toBeNull();
  });
});
