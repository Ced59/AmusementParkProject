import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ParkDataEditorTokenApiService } from './park-data-editor-token-api.service';

describe('ParkDataEditorTokenApiService', () => {
  let service: ParkDataEditorTokenApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ParkDataEditorTokenApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the editor’s own token endpoint for creation and revocation', () => {
    service.createToken().subscribe();
    const creation = http.expectOne(`${environment.apiBaseUrl}park-data-editor/tokens`);
    expect(creation.request.method).toBe('POST');
    expect(creation.request.body).toEqual({ label: 'Codex cloud editor', expiresInDays: 30 });
    creation.flush({ token: { id: 'token-1', expiresAtUtc: '2026-12-14T00:00:00Z' }, plainTextToken: 'secret' });

    service.revoke('token/1').subscribe();
    const revocation = http.expectOne(`${environment.apiBaseUrl}park-data-editor/tokens/token%2F1`);
    expect(revocation.request.method).toBe('DELETE');
    revocation.flush({ revokedCount: 1 });
  });
});
