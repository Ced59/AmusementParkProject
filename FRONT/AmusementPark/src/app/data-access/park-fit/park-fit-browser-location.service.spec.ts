import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { ParkFitBrowserLocationService } from './park-fit-browser-location.service';

describe('ParkFitBrowserLocationService', () => {
  const originalGeolocation: Geolocation | undefined = navigator.geolocation;

  afterEach(() => {
    Object.defineProperty(navigator, 'geolocation', {
      configurable: true,
      value: originalGeolocation
    });
    TestBed.resetTestingModule();
  });

  it('requests a fresh low-accuracy position and minimizes coordinate precision', () => {
    const getCurrentPosition = vi.fn((
      success: PositionCallback,
      _error?: PositionErrorCallback | null,
      _options?: PositionOptions
    ): void => {
      success({
        coords: {
          latitude: 50.629249,
          longitude: 3.057349,
          accuracy: 10,
          altitude: null,
          altitudeAccuracy: null,
          heading: null,
          speed: null,
          toJSON: (): object => ({})
        },
        timestamp: Date.now(),
        toJSON: (): object => ({})
      });
    });
    Object.defineProperty(navigator, 'geolocation', {
      configurable: true,
      value: { getCurrentPosition }
    });
    TestBed.configureTestingModule({
      providers: [
        ParkFitBrowserLocationService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    const service: ParkFitBrowserLocationService = TestBed.inject(ParkFitBrowserLocationService);
    let result: { latitude: number; longitude: number } | undefined;
    service.requestCurrentPosition().subscribe((position) => {
      result = position;
    });

    expect(result).toEqual({ latitude: 50.6292, longitude: 3.0573 });
    expect(getCurrentPosition).toHaveBeenCalledOnce();
    expect(getCurrentPosition.mock.calls[0]?.[2]).toEqual({
      enableHighAccuracy: false,
      timeout: 10000,
      maximumAge: 0
    });
  });

  it('does not access browser geolocation during server-side rendering', () => {
    const getCurrentPosition = vi.fn();
    Object.defineProperty(navigator, 'geolocation', {
      configurable: true,
      value: { getCurrentPosition }
    });
    TestBed.configureTestingModule({
      providers: [
        ParkFitBrowserLocationService,
        { provide: PLATFORM_ID, useValue: 'server' }
      ]
    });

    const service: ParkFitBrowserLocationService = TestBed.inject(ParkFitBrowserLocationService);
    let error: unknown;
    service.requestCurrentPosition().subscribe({
      error: (receivedError: unknown): void => {
        error = receivedError;
      }
    });

    expect(error).toBe('unsupported');
    expect(getCurrentPosition).not.toHaveBeenCalled();
  });
});
