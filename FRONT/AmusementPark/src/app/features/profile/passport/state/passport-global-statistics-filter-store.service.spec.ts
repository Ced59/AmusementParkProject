import { PassportGlobalStatisticsFilterStoreService } from './passport-global-statistics-filter-store.service';

describe('PassportGlobalStatisticsFilterStoreService', () => {
  const key: string = 'passport-global-statistics-filter';

  afterEach(() => sessionStorage.removeItem(key));

  it('keeps the private filter in session storage only', () => {
    const service: PassportGlobalStatisticsFilterStoreService =
      new PassportGlobalStatisticsFilterStoreService('browser' as unknown as object);

    service.write({ year: 2025, parkId: 'park-1' });

    expect(service.read()).toEqual({ year: 2025, parkId: 'park-1' });
    expect(localStorage.getItem(key)).toBeNull();
  });

  it('ignores malformed or server-side storage values', () => {
    sessionStorage.setItem(key, '{invalid');
    const browserService: PassportGlobalStatisticsFilterStoreService =
      new PassportGlobalStatisticsFilterStoreService('browser' as unknown as object);
    const serverService: PassportGlobalStatisticsFilterStoreService =
      new PassportGlobalStatisticsFilterStoreService('server' as unknown as object);

    expect(browserService.read()).toEqual({ year: null, parkId: null });
    expect(serverService.read()).toEqual({ year: null, parkId: null });
  });
});
