import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkOpeningHoursCalendar } from '@app/models/parks/park-opening-hours';

import { ParkItemDetailParksPort } from '../../park-item-detail-data.ports';

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Phantasialand',
    countryCode: 'DE',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: [],
  };
}

export class FakeParksPort implements ParkItemDetailParksPort {
  public parkResponse$: Observable<Park> = of(createPark());
  public openingHoursResponse$: Observable<ParkOpeningHoursCalendar> = of({
    parkId: 'park-1',
    timeZoneId: 'Europe/Berlin',
    updatedAtUtc: '2026-09-29T00:00:00Z',
    firstDate: null,
    lastDate: null,
    fromDate: '2026-09-29',
    toDate: '2026-09-29',
    days: []
  });
  public readonly calls: string[] = [];
  public readonly openingHoursCalls: Array<{
    id: string;
    from: string | null | undefined;
    to: string | null | undefined;
  }> = [];

  getParkById(id: string): Observable<Park> {
    this.calls.push(id);
    return this.parkResponse$;
  }

  getParkOpeningHours(
    id: string,
    from?: string | null,
    to?: string | null
  ): Observable<ParkOpeningHoursCalendar> {
    this.openingHoursCalls.push({ id, from, to });
    return this.openingHoursResponse$;
  }
}
