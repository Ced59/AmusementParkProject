import { DOCUMENT } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { PassportExport, PassportExportFormat } from '@app/models/passport/passport-export.models';
import { PassportExportApiPort, PASSPORT_EXPORT_API_PORT } from './passport-export-state-data.ports';
import { PassportExportStateFacade } from './passport-export-state.facade';

describe('PassportExportStateFacade', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('exposes a ready export returned immediately by the API', () => {
    const api = createApi(() => of(createExport('Ready')));
    const facade = createFacade(api);

    facade.request('Json');

    expect(api.requestExport).toHaveBeenCalledWith({ format: 'Json' });
    expect(facade.ready()).toBe(true);
    expect(facade.generating()).toBe(false);
    expect(facade.errorKey()).toBeNull();
  });

  it('polls a pending export until it is ready', () => {
    vi.useFakeTimers();
    const api = createApi(
      () => of(createExport('Pending')),
      () => of(createExport('Ready'))
    );
    const facade = createFacade(api);

    facade.request('Csv');
    expect(facade.generating()).toBe(true);
    vi.advanceTimersByTime(1500);

    expect(api.getExport).toHaveBeenCalledWith('export-1');
    expect(facade.ready()).toBe(true);
    expect(facade.generating()).toBe(false);
  });

  it('allows another request after status polling fails', () => {
    vi.useFakeTimers();
    const api = createApi(
      () => of(createExport('Pending')),
      () => throwError(() => new Error('offline'))
    );
    const facade = createFacade(api);

    facade.request('Json');
    vi.advanceTimersByTime(1500);

    expect(facade.errorKey()).toBe('passport.exports.errors.status');
    expect(facade.generating()).toBe(false);
    facade.request('Csv');
    expect(api.requestExport).toHaveBeenCalledTimes(2);
  });
});

function createFacade(api: PassportExportApiPort): PassportExportStateFacade {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      PassportExportStateFacade,
      { provide: PASSPORT_EXPORT_API_PORT, useValue: api },
      { provide: PLATFORM_ID, useValue: 'browser' },
      { provide: DOCUMENT, useValue: document }
    ]
  });
  return TestBed.inject(PassportExportStateFacade);
}

function createApi(
  request: (format: PassportExportFormat) => Observable<PassportExport>,
  status: () => Observable<PassportExport> = () => of(createExport('Ready'))
): PassportExportApiPort & { requestExport: ReturnType<typeof vi.fn>; getExport: ReturnType<typeof vi.fn> } {
  return {
    requestExport: vi.fn((input: { format: PassportExportFormat }) => request(input.format)),
    getExport: vi.fn(() => status()),
    downloadExport: vi.fn(() => of(new Blob()))
  };
}

function createExport(status: PassportExport['status']): PassportExport {
  return {
    id: 'export-1',
    format: 'Json',
    status,
    schemaVersion: 1,
    createdAtUtc: '2026-09-03T12:00:00Z',
    updatedAtUtc: '2026-09-03T12:00:00Z',
    expiresAtUtc: '2026-09-03T13:00:00Z',
    completedAtUtc: status === 'Ready' ? '2026-09-03T12:00:01Z' : null,
    fileName: status === 'Ready' ? 'passport.json' : null,
    sizeBytes: status === 'Ready' ? 128 : null,
    errorCode: null,
    downloadUrl: status === 'Ready' ? '/me/passport/exports/export-1?download=true' : null
  };
}
