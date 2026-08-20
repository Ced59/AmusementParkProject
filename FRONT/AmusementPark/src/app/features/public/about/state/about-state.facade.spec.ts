import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { ABOUT_STATE_HOME_STATS_PORT, AboutStateHomeStatsPort } from './about-state-data.ports';
import { AboutStateFacade } from './about-state.facade';

class FakeAboutHomeStatsPort implements AboutStateHomeStatsPort {
  public response$: Observable<HomeStatsModel> = of({
    parksCount: 47,
    attractionsCount: 830,
    countriesCount: 12
  });
  public calls: number = 0;

  getHomeStats(): Observable<HomeStatsModel> {
    this.calls += 1;
    return this.response$;
  }
}

describe('AboutStateFacade', () => {
  let facade: AboutStateFacade;
  let homeStatsPort: FakeAboutHomeStatsPort;

  beforeEach(() => {
    homeStatsPort = new FakeAboutHomeStatsPort();

    TestBed.configureTestingModule({
      providers: [
        AboutStateFacade,
        { provide: ABOUT_STATE_HOME_STATS_PORT, useValue: homeStatsPort }
      ]
    });

    facade = TestBed.inject(AboutStateFacade);
  });

  it('loads the visible park count through the public stats port', () => {
    facade.loadVisibleParkCount();

    expect(homeStatsPort.calls).toBe(1);
    expect(facade.visibleParkCount()).toBe(47);
  });

  it('keeps the count unavailable when public stats cannot be loaded', () => {
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    homeStatsPort.response$ = throwError(() => new Error('network error'));

    facade.loadVisibleParkCount();

    expect(facade.visibleParkCount()).toBeNull();
    expect(consoleErrorSpy).toHaveBeenCalledOnce();
    consoleErrorSpy.mockRestore();
  });
});
