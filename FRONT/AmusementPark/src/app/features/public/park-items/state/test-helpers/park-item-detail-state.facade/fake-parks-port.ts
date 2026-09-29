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

  getParkById(id: string): Observable<Park> {
    this.calls.push(id);
    return this.parkResponse$;
  }

  getParkOpeningHours(): Observable<ParkOpeningHoursCalendar> {
    return this.openingHoursResponse$;
  }
}
