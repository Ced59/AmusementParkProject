import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { NearbyDestination } from '@app/models/parks/nearby-destination';
import { MeasurementPreferenceService } from '@app/services/measurements/measurement-preference.service';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NearbyDestinationsFacade } from './nearby-destinations.facade';
import { mixedNearbyDestinations } from './nearby-destinations.fixture';
import { NEARBY_DESTINATIONS_PORT } from './nearby-destinations.ports';

describe('NearbyDestinationsFacade', () => {
  const preference = signal<MeasurementSystem>('Metric');
  const getNearby = vi.fn();
  let facade: NearbyDestinationsFacade;

  beforeEach(() => {
    preference.set('Metric');
    getNearby.mockReset().mockReturnValue(of(mixedNearbyDestinations()));
    TestBed.configureTestingModule({ providers: [
      NearbyDestinationsFacade,
      { provide: NEARBY_DESTINATIONS_PORT, useValue: { getNearby } },
      { provide: MeasurementPreferenceService, useValue: { preferredSystem: preference } },
      { provide: CountryDisplayService, useValue: { resolveLocalizedCountryName: () => 'France' } }
    ] });
    facade = TestBed.inject(NearbyDestinationsFacade);
    facade.setLanguage('fr');
  });

  it.each(['Park', 'StandaloneAttraction'] as const)('keeps mixed ranking and localized links for a %s source', kind => {
    facade.load(kind, 'source');
    expect(facade.state().kind).toBe('ready');
    expect(facade.cards().map(destination => destination.card.id)).toEqual(['alpine', 'park']);
    expect(facade.cards()[0].detailLink).toEqual(['/', 'fr', 'attraction', 'alpine', 'luge-alpine']);
    expect(facade.cards()[1].detailLink).toEqual(['/', 'fr', 'park', 'park', 'parc-voisin']);
    expect(facade.cards()[0].card.shortDescription!.length).toBeLessThanOrEqual(140);
    expect(getNearby.mock.calls[0].slice(0, 3)).toEqual([kind, 'source', 4]);
    expect(getNearby.mock.calls[0][3].context.get(SKIP_AUTHORIZATION_HEADER)).toBe(true);
  });

  it('updates distances and links without another request when units or language change', () => {
    facade.load('Park', 'source');
    expect(facade.cards()[0].card.distanceLine).toBe('1,6 km');
    preference.set('Imperial');
    facade.setLanguage('en');
    expect(facade.cards()[0].card.distanceLine).toBe('1 mi');
    expect(facade.cards()[1].card.distanceLine).toBe('2 mi');
    expect(facade.cards()[0].detailLink?.[1]).toBe('en');
    expect(getNearby).toHaveBeenCalledTimes(1);
  });

  it('uses the published standalone photo while preserving park logos and missing-photo fallbacks', () => {
    const targets = mixedNearbyDestinations();
    targets[0].photoImageId = 'published-photo';
    targets[1].park!.currentLogoImageId = 'park-logo';
    getNearby.mockReturnValue(of(targets));
    facade.load('Park', 'source');
    expect(facade.cards()[0].card.photoImageId).toBe('published-photo');
    expect(facade.cards()[1].card.logoImageId).toBe('park-logo');
    expect(facade.cards()[1].card.photoImageId).toBeUndefined();

    getNearby.mockReturnValue(of(mixedNearbyDestinations()));
    facade.load('StandaloneAttraction', 'other');
    expect(facade.cards()[0].card.photoImageId).toBeNull();
  });

  it('cancels stale requests and clears cards when navigating to another source', () => {
    const first = new Subject<NearbyDestination[]>();
    const second = new Subject<NearbyDestination[]>();
    getNearby.mockReturnValueOnce(first).mockReturnValueOnce(second);
    facade.load('Park', 'source');
    first.next(mixedNearbyDestinations());
    facade.load('StandaloneAttraction', 'other');
    expect(facade.state().kind).toBe('loading');
    expect(facade.cards()).toEqual([]);
    first.next(mixedNearbyDestinations());
    expect(facade.cards()).toEqual([]);
    second.next(mixedNearbyDestinations().slice(1));
    expect(facade.cards().map(destination => destination.card.id)).toEqual(['park']);
  });

  it('excludes the current destination without excluding the other kind with the same id', () => {
    const targets = mixedNearbyDestinations();
    targets[1].park!.id = 'alpine';
    getNearby.mockReturnValue(of(targets));
    facade.load('StandaloneAttraction', 'alpine');
    expect(facade.cards()).toHaveLength(1);
    expect(facade.cards()[0].detailLink?.[2]).toBe('park');
  });

  it('handles missing coordinates, empty results and errors without stale cards', () => {
    facade.load('Park', null);
    expect(getNearby).not.toHaveBeenCalled();
    expect(facade.state().kind).toBe('empty');
    getNearby.mockReturnValue(of([]));
    facade.load('Park', 'source');
    expect(facade.state().kind).toBe('empty');
    getNearby.mockReturnValue(throwError(() => new Error('unavailable')));
    facade.load('Park', 'source');
    expect(facade.state().kind).toBe('error');
    expect(facade.cards()).toEqual([]);
  });
});
