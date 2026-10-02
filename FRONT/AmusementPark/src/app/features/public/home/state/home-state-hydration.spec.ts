import { PLATFORM_ID, TransferState, makeStateKey } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import {
  HOME_STATE_HOME_API_SERVICE_PORT,
  HOME_STATE_PARKS_API_SERVICE_PORT,
  HOME_STATE_SEARCH_API_SERVICE_PORT,
} from './home-state-data.ports';
import { HomeStateFacade } from './home-state.facade';
import { FakeHomePort } from './test-helpers/home-state.facade/fake-home-port';
import { FakeParksPort } from './test-helpers/home-state.facade/fake-parks-port';
import { FakeSearchPort } from './test-helpers/home-state.facade/fake-search-port';

function createHydrationContext(platform: 'server' | 'browser', serializedState?: string) {
  TestBed.resetTestingModule();
  const parksPort = new FakeParksPort();
  const homePort = new FakeHomePort();
  TestBed.configureTestingModule({
    providers: [
      HomeStateFacade,
      NaturalTextTruncatorService,
      CountryDisplayService,
      { provide: PLATFORM_ID, useValue: platform },
      { provide: HOME_STATE_PARKS_API_SERVICE_PORT, useValue: parksPort },
      { provide: HOME_STATE_HOME_API_SERVICE_PORT, useValue: homePort },
      { provide: HOME_STATE_SEARCH_API_SERVICE_PORT, useValue: new FakeSearchPort() },
    ],
  });
  const state = TestBed.inject(TransferState);
  if (serializedState !== undefined) {
    const values: Record<string, unknown> = JSON.parse(serializedState);
    for (const [key, value] of Object.entries(values)) {
      state.set(makeStateKey<unknown>(key), value);
    }
  }
  return { facade: TestBed.inject(HomeStateFacade), parksPort, homePort, state };
}

describe('HomeStateFacade hydration', () => {
  it('restores the exact server cards without repeating either anonymous API request', () => {
    const server = createHydrationContext('server');
    server.facade.loadFeaturedParks('en');
    const heroCards = server.facade.heroParks();
    const featuredCards = server.facade.featuredParks();
    const serializedState = server.state.toJson();

    const browser = createHydrationContext('browser', serializedState);
    browser.facade.loadFeaturedParks('en');

    expect(browser.facade.heroParks()).toEqual(heroCards);
    expect(browser.facade.featuredParks()).toEqual(featuredCards);
    expect(browser.facade.heroParksState().kind).toBe('ready');
    expect(browser.facade.featuredState().kind).toBe('ready');
    expect(browser.parksPort.calls).toEqual([]);
    expect(browser.homePort.featuredCalls).toEqual([]);

    browser.facade.loadFeaturedParks('en');
    expect(browser.parksPort.calls).toEqual([4]);
    expect(browser.homePort.featuredCalls).toEqual([{ excludedParkIds: ['park-1'], limit: 3 }]);
  });

  it('serializes only the mapped localized cards rather than multilingual descriptions', () => {
    const server = createHydrationContext('server');
    server.parksPort.response$ = of([{
      id: 'park-1', name: 'Park', countryCode: 'FR', latitude: 48.8, longitude: 2.3,
      isVisible: true,
      descriptions: [
        { languageCode: 'en', value: '<p>English park description.</p>' },
        { languageCode: 'fr', value: `<p>${'UNUSED_TRANSLATION'.repeat(1000)}</p>` },
      ],
    }]);
    server.facade.loadFeaturedParks('en');
    const serializedState = server.state.toJson();

    expect(serializedState).toContain('English park description.');
    expect(serializedState).not.toContain('UNUSED_TRANSLATION');
    expect(serializedState).not.toContain('languageCode');
    expect(serializedState).not.toContain('descriptions');
    expect(serializedState.length).toBeLessThan(3000);
  });

  it('restores successful empty results without turning them into loading states or requests', () => {
    const server = createHydrationContext('server');
    server.parksPort.response$ = of([]);
    server.homePort.featuredResponse$ = of([]);
    server.facade.loadFeaturedParks('en');
    const browser = createHydrationContext('browser', server.state.toJson());
    browser.facade.loadFeaturedParks('en');

    expect(browser.facade.heroParksState().kind).toBe('empty');
    expect(browser.facade.featuredState().kind).toBe('empty');
    expect(browser.parksPort.calls).toEqual([]);
    expect(browser.homePort.featuredCalls).toEqual([]);
  });

  it('fetches localized cards again when the requested language differs from the server language', () => {
    const server = createHydrationContext('server');
    server.facade.loadFeaturedParks('en');
    const browser = createHydrationContext('browser', server.state.toJson());
    browser.facade.loadFeaturedParks('fr');

    expect(browser.parksPort.calls).toEqual([4]);
    expect(browser.homePort.featuredCalls).toEqual([{ excludedParkIds: ['park-1'], limit: 3 }]);
    expect(browser.facade.featuredParks()[0].detailLink?.[1]).toBe('fr');
  });

  it('retries a failed hero request and does not reuse featured cards with different exclusions', () => {
    const server = createHydrationContext('server');
    server.parksPort.response$ = throwError(() => new Error('network'));
    server.facade.loadFeaturedParks('en');
    const browser = createHydrationContext('browser', server.state.toJson());
    browser.facade.loadFeaturedParks('en');

    expect(browser.parksPort.calls).toEqual([4]);
    expect(browser.homePort.featuredCalls).toEqual([{ excludedParkIds: ['park-1'], limit: 3 }]);
    expect(browser.facade.heroParksState().kind).toBe('ready');
  });
});
