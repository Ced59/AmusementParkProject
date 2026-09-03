import { DestroyRef } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';

import { CreatePassportVisitRequest, PassportVisit } from '@app/models/passport/passport-visit.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { TranslateService } from '@ngx-translate/core';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { PassportVisitQuickCreateDraft } from '../models/passport-visit-quick-create.models';
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

class FakeModalService {
  readonly opened: string[] = [];

  openModal(id: string): void {
    this.opened.push(id);
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

    expect(api.calls.map((call: { key: string }): string => call.key)).toEqual(['operation-1', 'operation-2']);
  });

  it('opens the login dialog without discarding the validated draft when authentication is missing', () => {
    const api: FakeVisitApi = new FakeVisitApi();
    const auth: FakeAuthService = new FakeAuthService();
    const modal: FakeModalService = new FakeModalService();
    auth.token = null;
    const facade: PassportVisitQuickCreateStateFacade = createFacade(api, auth, modal);

    facade.createVisit(createDraft());

    expect(api.calls).toHaveLength(0);
    expect(modal.opened).toEqual(['loginModal']);
    expect(facade.errorKey()).toBe('passport.quickCreate.errors.signInRequired');
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
  modal: FakeModalService = new FakeModalService()
): PassportVisitQuickCreateStateFacade {
  return new PassportVisitQuickCreateStateFacade(
    api,
    new FakeParksApi(),
    new FakeOperationIds(),
    auth as unknown as AuthService,
    modal as unknown as ModalService,
    new FakeMessages() as unknown as ToastMessageService,
    new FakeTranslateService() as unknown as TranslateService,
    new FakeDestroyRef()
  );
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
