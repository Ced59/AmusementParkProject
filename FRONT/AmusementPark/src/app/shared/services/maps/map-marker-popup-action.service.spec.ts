import { MapMarker } from '@app/models/map/map-marker';

import { MapDirectionsUrlService } from './map-directions-url.service';
import { MapMarkerDetailLinkService } from './map-marker-detail-link.service';
import { MapMarkerPopupActionService } from './map-marker-popup-action.service';

describe('MapMarkerPopupActionService', () => {
  let directionsService: jasmine.SpyObj<MapDirectionsUrlService>;
  let detailLinkService: jasmine.SpyObj<MapMarkerDetailLinkService>;
  let service: MapMarkerPopupActionService;

  beforeEach(() => {
    directionsService = jasmine.createSpyObj<MapDirectionsUrlService>('MapDirectionsUrlService', ['buildDirectionsUrl']);
    detailLinkService = jasmine.createSpyObj<MapMarkerDetailLinkService>('MapMarkerDetailLinkService', ['buildParkDetailRouteCommands', 'buildParkItemDetailRouteCommands']);
    service = new MapMarkerPopupActionService(directionsService, detailLinkService);
  });

  function createMarker(): MapMarker {
    return {
      id: 'm1',
      label: 'Marker',
      lat: 50,
      lng: 3,
      iconKind: 'park'
    } as MapMarker;
  }

  it('adds directions url and labels without mutating the original marker', () => {
    const marker: MapMarker = createMarker();
    directionsService.buildDirectionsUrl.and.returnValue('https://maps.test');

    const result: MapMarker = service.enrich(marker, {
      directions: { latitude: 50, longitude: 3 },
      directionsLabel: ' Go '
    });

    expect(result).not.toBe(marker);
    expect(result.actionUrl).toBe('https://maps.test');
    expect(result.actionLabel).toBe('Go');
    expect(result.directionsActionEnabled).toBeTrue();
    expect(marker.actionUrl).toBeUndefined();
  });

  it('prefers park item detail route commands over park route commands', () => {
    const marker: MapMarker = createMarker();
    detailLinkService.buildParkItemDetailRouteCommands.and.returnValue(['/', 'fr', 'item']);
    detailLinkService.buildParkDetailRouteCommands.and.returnValue(['/', 'fr', 'park']);

    const result: MapMarker = service.enrich(marker, {
      parkDetail: { language: 'fr', parkId: 'p1', parkName: 'Park' },
      parkItemDetail: { language: 'fr', parkId: 'p1', parkName: 'Park', itemId: 'i1', itemName: 'Item' },
      detailLabel: 'Details'
    });

    expect(result.detailActionRouteCommands).toEqual(['/', 'fr', 'item']);
    expect(result.detailActionLabel).toBe('Details');
    expect(detailLinkService.buildParkDetailRouteCommands).not.toHaveBeenCalled();
  });

  it('keeps existing action values when no replacement is provided', () => {
    const marker: MapMarker = {
      ...createMarker(),
      actionUrl: 'https://existing.test',
      actionLabel: 'Existing',
      detailActionRouteCommands: ['/', 'en', 'park'],
      detailActionLabel: 'Existing detail'
    } as MapMarker;

    const result: MapMarker = service.enrich(marker, {});

    expect(result.actionUrl).toBe('https://existing.test');
    expect(result.actionLabel).toBe('Existing');
    expect(result.detailActionRouteCommands).toEqual(['/', 'en', 'park']);
    expect(result.detailActionLabel).toBe('Existing detail');
  });

  it('normalizes blank labels to null', () => {
    const result: MapMarker = service.enrich(createMarker(), {
      directionsLabel: ' ',
      detailLabel: ' '
    });

    expect(result.actionLabel).toBeNull();
    expect(result.detailActionLabel).toBeNull();
  });
});
