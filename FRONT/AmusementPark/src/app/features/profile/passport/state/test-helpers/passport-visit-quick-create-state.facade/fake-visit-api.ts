import { Observable, of } from 'rxjs';

import { CreatePassportVisitRequest, PassportVisit } from '@app/models/passport/passport-visit.models';

import { PassportVisitQuickCreateApiPort } from '../../passport-visit-quick-create-state-data.ports';

function createVisit(overrides: Partial<PassportVisit> = {}): PassportVisit {
  return {
    id: 'visit-1',
    parkId: 'park-1',
    date: {
      year: 2026,
      month: 9,
      day: 3,
      precision: 'Day',
      isApproximate: false
    },
    timeZoneId: 'Europe/Paris',
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: null,
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-03T12:00:00Z',
    updatedAtUtc: '2026-09-03T12:00:00Z',
    completedAtUtc: null,
    ...overrides
  };
}

export class FakeVisitApi implements PassportVisitQuickCreateApiPort {
  readonly calls: Array<{ request: CreatePassportVisitRequest; key: string }> = [];
  responses: Observable<PassportVisit>[] = [];

  createVisit(request: CreatePassportVisitRequest, idempotencyKey: string): Observable<PassportVisit> {
    this.calls.push({ request, key: idempotencyKey });
    return this.responses.shift() ?? of(createVisit());
  }
}
