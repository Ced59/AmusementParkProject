import { DestroyRef } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { CreatePassportVisitRequest, PassportVisit } from '@app/models/passport/passport-visit.models';
import { AuthService } from '@app/services/auth/auth.service';
import { PassportProductAnalyticsPort } from '@core/analytics/passport-product-analytics.port';
import { PassportProductEvent } from '@core/analytics/passport-product-event.model';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslateService } from '@ngx-translate/core';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { PassportVisitQuickCreateDraft } from '../models/passport-visit-quick-create.models';
import { PassportAnonymousDraft } from '../anonymous-drafts/models/passport-anonymous-draft.models';
import { PassportAnonymousDraftStorePort } from '../anonymous-drafts/state/passport-anonymous-draft-store.ports';
import {
  PassportVisitOperationIdPort,
  PassportVisitQuickCreateApiPort,
  PassportVisitQuickCreateParksPort
} from './passport-visit-quick-create-state-data.ports';
import { PassportVisitQuickCreateStateFacade } from './passport-visit-quick-create-state.facade';

class FakeDestroyRef implements DestroyRef {
  readonly destroyed = false;

  onDestroy(callback: () => void): () => void {
    void callback;
    return (): void => undefined;
  }
}

class FakeVisitApi implements PassportVisitQuickCreateApiPort {
  readonly calls: Array<{ request: CreatePassportVisitRequest; key: string }> = [];
  responses: Observable<PassportVisit>[] = [];

  createVisit(request: CreatePassportVisitRequest, idempotencyKey: string): Observable<PassportVisit> {
    this.calls.push({ request, key: idempotencyKey });
    return this.responses.shift() ?? of(createVisit());
  }
}

class FakeParksApi implements PassportVisitQuickCreateParksPort {
  searchParks(_query: string, _page: number, _size: number, _visibleOnly: boolean): Observable<ParksApiResponse> {
    return of({ data: [], pagination: { currentPage: 1, itemsPerPage: 8, totalItems: 0, totalPages: 0 } });
  }
}

class FakeOperationIds implements PassportVisitOperationIdPort {
  private count: number = 0;

  create(): string {
    this.count += 1;
    return `operation-${this.count}`;
  }
}

class FakeAuthService {
  token: string | null = 'token';

  ensureValidAccessToken(_forceRefresh: boolean): Observable<string | null> {
    return of(this.token);
  }
}

class FakeMessages {
  readonly details: string[] = [];

  add(_severity: 'success' | 'info' | 'warn' | 'error', _summary: string, detail: string): void {
    this.details.push(detail);
  }
}

class FakeTranslateService {
  instant(key: string): string {
    return key;
  }
}

describe('PassportVisitQuickCreateStateFacade', () => {
  it('reuses the same idempotency key when a network response is lost and the same form is retried', () => {
    const api: FakeVisitApi = new FakeVisitApi();
    api.responses = [
      throwError(() => new HttpErrorResponse({ status: 0 })),
      of(createVisit())
    ];
    const facade: PassportVisitQuickCreateStateFacade = createFacade(api);
    const draft: PassportVisitQuickCreateDraft = createDraft();

    facade.createVisit(draft);
    expect(facade.errorKey()).toBe('passport.quickCreate.errors.network');
    facade.createVisit(draft);

    expect(api.calls).toHaveLength(2);
    expect(api.calls[0].key).toBe('operation-1');
    expect(api.calls[1].key).toBe('operation-1');
    expect(facade.createdVisit()?.id).toBe('visit-1');
  });

  it('uses a new idempotency key when the payload changes after a failed attempt', () => {
    const api: FakeVisitApi = new FakeVisitApi();
    api.responses = [
      throwError(() => new Error('failed')),
      of(createVisit({ title: 'Changed' }))
    ];
    const facade: PassportVisitQuickCreateStateFacade = createFacade(api);

    facade.createVisit(createDraft());
    facade.createVisit(createDraft({ title: 'Changed' }));

    expect(api.calls.map((call: { key: string }): string => call.key)).toEqual(['operation-1', 'operation-4']);
  });

  it('keeps the validated visit only in IndexedDB when authentication is missing', async () => {
    const api: FakeVisitApi = new FakeVisitApi();
    const auth: FakeAuthService = new FakeAuthService();
    const savedDrafts: PassportAnonymousDraft[] = [];
    const events: PassportProductEvent[] = [];
    auth.token = null;
    const facade: PassportVisitQuickCreateStateFacade = createFacade(
      api,
      auth,
      createDraftStore(savedDrafts),
      {
        track: (event: PassportProductEvent): void => {
          events.push(event);
        }
      }
    );

    facade.createVisit(createDraft(), 'Parc test');
    await vi.waitFor((): void => {
      expect(facade.createdLocalDraftId()).toBe('operation-2');
    });

    expect(api.calls).toHaveLength(0);
    expect(savedDrafts).toHaveLength(1);
    expect(savedDrafts[0].parkName).toBe('Parc test');
    expect(savedDrafts[0].visitOperationId).toBe('operation-1');
    expect(savedDrafts[0].rideOperationId).toBe('operation-3');
    expect(facade.errorKey()).toBeNull();
    expect(events).toEqual([
      {
        type: 'visit_creation_started',
        source: 'anonymous-local',
        datePrecision: 'Day'
      },
      {
        type: 'visit_created',
        source: 'anonymous-local',
        datePrecision: 'Day'
      }
    ]);
  });

  it('records the anonymous second-visit signal only once after the milestone is reached', async () => {
    const api: FakeVisitApi = new FakeVisitApi();
    const auth: FakeAuthService = new FakeAuthService();
    const savedDrafts: PassportAnonymousDraft[] = [createAnonymousDraft()];
    const events: PassportProductEvent[] = [];
    auth.token = null;
    const facade: PassportVisitQuickCreateStateFacade = createFacade(
      api,
      auth,
      createDraftStore(savedDrafts),
      {
        track: (event: PassportProductEvent): void => {
          events.push(event);
        }
      }
    );

    facade.createVisit(createDraft({ parkId: 'park-2' }), 'Deuxième parc');

    await vi.waitFor((): void => {
      expect(events.some((event: PassportProductEvent): boolean =>
        event.type === 'second_visit_recorded')).toBe(true);
    });
    expect(savedDrafts).toHaveLength(2);

    savedDrafts.splice(0, 1);
    facade.clearCreationResult();
    facade.createVisit(createDraft({ parkId: 'park-3' }), 'Troisième parc');

    await vi.waitFor((): void => {
      expect(savedDrafts).toHaveLength(2);
    });
    expect(events.filter((event: PassportProductEvent): boolean =>
      event.type === 'second_visit_recorded')).toHaveLength(1);
  });

  it('does not call the API for an invalid partial date', () => {
    const api: FakeVisitApi = new FakeVisitApi();
    const facade: PassportVisitQuickCreateStateFacade = createFacade(api);

    facade.createVisit(createDraft({ precision: 'Month', month: null, day: null }));

    expect(api.calls).toHaveLength(0);
    expect(facade.errorKey()).toBe('passport.quickCreate.validation.monthInvalid');
  });
});

function createFacade(
  api: FakeVisitApi,
  auth: FakeAuthService = new FakeAuthService(),
  anonymousDrafts: PassportAnonymousDraftStorePort = createDraftStore(),
  analytics: PassportProductAnalyticsPort = { track: vi.fn() }
): PassportVisitQuickCreateStateFacade {
  return new PassportVisitQuickCreateStateFacade(
    api,
    new FakeParksApi(),
    new FakeOperationIds(),
    anonymousDrafts,
    analytics,
    auth as unknown as AuthService,
    new FakeMessages() as unknown as ToastMessageService,
    new FakeTranslateService() as unknown as TranslateService,
    new FakeDestroyRef()
  );
}

function createDraftStore(
  savedDrafts: PassportAnonymousDraft[] = []
): PassportAnonymousDraftStorePort {
  let secondVisitMilestoneClaimed: boolean = false;
  return {
    isAvailable: (): boolean => true,
    list: async (): Promise<PassportAnonymousDraft[]> => [...savedDrafts],
    get: async (draftId: string): Promise<PassportAnonymousDraft | null> =>
      savedDrafts.find((draft: PassportAnonymousDraft): boolean => draft.id === draftId) ?? null,
    save: async (draft: PassportAnonymousDraft): Promise<void> => {
      const existingIndex: number = savedDrafts.findIndex(
        (candidate: PassportAnonymousDraft): boolean => candidate.id === draft.id
      );
      if (existingIndex >= 0) {
        savedDrafts[existingIndex] = draft;
      } else {
        savedDrafts.push(draft);
      }
    },
    claimSecondVisitMilestone: async (): Promise<boolean> => {
      if (secondVisitMilestoneClaimed || savedDrafts.length < 2) {
        return false;
      }

      secondVisitMilestoneClaimed = true;
      return true;
    },
    compareAndSet: async (): Promise<boolean> => true,
    deleteIfUnchanged: async (): Promise<boolean> => true,
    delete: async (draftId: string): Promise<void> => {
      const index: number = savedDrafts.findIndex(
        (candidate: PassportAnonymousDraft): boolean => candidate.id === draftId
      );
      if (index >= 0) {
        savedDrafts.splice(index, 1);
      }
    },
    clear: async (): Promise<void> => {
      savedDrafts.splice(0, savedDrafts.length);
    }
  };
}

function createDraft(overrides: Partial<PassportVisitQuickCreateDraft> = {}): PassportVisitQuickCreateDraft {
  return {
    parkId: 'park-1',
    precision: 'Day',
    year: 2026,
    month: 9,
    day: 3,
    isApproximate: false,
    timeZoneId: 'Europe/Paris',
    title: '',
    privateNote: '',
    ...overrides
  };
}

function createAnonymousDraft(): PassportAnonymousDraft {
  return {
    schemaVersion: 1,
    id: 'existing-draft',
    visitOperationId: 'existing-visit-operation',
    rideOperationId: 'existing-ride-operation',
    parkName: 'Premier parc',
    visit: {
      parkId: 'park-1',
      date: { year: 2026, month: 8, day: 1, precision: 'Day', isApproximate: false },
      timeZoneId: 'Europe/Paris',
      serviceDayConvention: 'VisitStartLocalDate',
      title: null,
      privateNote: null
    },
    rides: [],
    createdAtUtc: '2026-08-01T10:00:00.000Z',
    updatedAtUtc: '2026-08-01T10:00:00.000Z'
  };
}

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
