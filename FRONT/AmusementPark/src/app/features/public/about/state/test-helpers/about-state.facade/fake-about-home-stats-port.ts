import { Observable, of } from 'rxjs';

import { HomeStatsModel } from '@app/models/home/home-stats.model';

import { AboutStateHomeStatsPort } from '../../about-state-data.ports';

export class FakeAboutHomeStatsPort implements AboutStateHomeStatsPort {
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
