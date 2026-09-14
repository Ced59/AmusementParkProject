import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';

import { ParkFitSearchOrigin } from '../models/park-fit-search-form.models';
import {
  PARK_FIT_BROWSER_LOCATION_DATA_PORT,
  ParkFitBrowserLocationDataPort
} from './park-fit-browser-location-data.port';
import { ParkFitBrowserLocationFacade } from './park-fit-browser-location.facade';

describe('ParkFitBrowserLocationFacade', () => {
  let position$: Subject<ParkFitSearchOrigin>;
  let facade: ParkFitBrowserLocationFacade;

  beforeEach(() => {
    position$ = new Subject<ParkFitSearchOrigin>();
    const port: ParkFitBrowserLocationDataPort = {
      requestCurrentPosition: () => position$.asObservable()
    };
    TestBed.configureTestingModule({
      providers: [
        ParkFitBrowserLocationFacade,
        { provide: PARK_FIT_BROWSER_LOCATION_DATA_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(ParkFitBrowserLocationFacade);
  });

  it('keeps the explicitly requested position only in memory', () => {
    facade.request();
    expect(facade.status()).toBe('loading');

    position$.next({ latitude: 50.6292, longitude: 3.0573 });
    position$.complete();

    expect(facade.status()).toBe('ready');
    expect(facade.position()).toEqual({ latitude: 50.6292, longitude: 3.0573 });

    facade.clear();
    expect(facade.status()).toBe('idle');
    expect(facade.position()).toBeNull();
  });

  it('turns a refusal into actionable feedback without retaining coordinates', () => {
    facade.request();
    position$.error('denied');

    expect(facade.status()).toBe('error');
    expect(facade.position()).toBeNull();
    expect(facade.errorKey()).toBe('parkFit.location.errors.denied');
  });
});
