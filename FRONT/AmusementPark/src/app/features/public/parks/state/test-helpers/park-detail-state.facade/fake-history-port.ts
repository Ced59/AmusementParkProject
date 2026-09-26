import { Observable, of } from 'rxjs';

import { PublicParkHistoricalTimeline } from '@app/models/history/public-park-history.models';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { ParkDetailHistoryPort } from '../../park-detail-data.ports';

function createHistoryTimeline(totalEvents: number): PublicParkHistoricalTimeline {
  return {
    parkId: 'park-1',
    parkName: 'Bellewaerde',
    events:
      totalEvents > 0
        ? [
            {
              subjectType: 'Park',
              subjectId: 'park-1',
              subjectLabel: 'Bellewaerde',
              factType: 'Opening',
              period: {
                start: { year: 1954, precision: 'Year' },
                end: null,
                startConfidence: 'Certain',
                endConfidence: 'Unknown'
              },
              evidenceState: 'Verified',
              importance: 'Major',
              uncertaintyExplanations: [],
              sources: []
            }
          ]
        : [],
    pagination: {
      currentPage: 1,
      itemsPerPage: 1,
      totalItems: totalEvents,
      totalPages: totalEvents > 0 ? 1 : 0
    }
  };
}

export class FakeHistoryPort implements ParkDetailHistoryPort {
  public timelineResponse$: Observable<PublicParkHistoricalTimeline> = of(
    createHistoryTimeline(0),
  );
  public timelineResponses$: Observable<PublicParkHistoricalTimeline>[] = [];
  public readonly calls: {
    parkId: string;
    page?: number;
    pageSize?: number;
  }[] = [];
  public readonly options: Array<AnonymousHttpOptions | undefined> = [];

  getPublicParkTimeline(
    parkId: string,
    options?: AnonymousHttpOptions,
    page: number = 1,
    pageSize: number = 50,
  ): Observable<PublicParkHistoricalTimeline> {
    this.calls.push({ parkId, page, pageSize });
    this.options.push(options);
    const queuedResponse$: Observable<PublicParkHistoricalTimeline> | undefined =
      this.timelineResponses$.shift();
    if (queuedResponse$) {
      return queuedResponse$;
    }

    return this.timelineResponse$;
  }
}
