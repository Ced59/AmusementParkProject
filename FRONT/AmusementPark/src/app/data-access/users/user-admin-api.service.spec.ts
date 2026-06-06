import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { UserAdminApiService } from './user-admin-api.service';

describe('UserAdminApiService', () => {
  let service: UserAdminApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(UserAdminApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('assigns and removes roles using expected HTTP methods and bodies', () => {
    const payload = { role: 'ADMIN' } as never;

    service.assignRoleToUser('user/1', payload).subscribe();
    service.removeRoleFromUser('user/1', payload).subscribe();

    const assignRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/roles/assign/user%2F1`);
    expect(assignRequest.request.method).toBe('POST');
    expect(assignRequest.request.body).toBe(payload);
    assignRequest.flush({ roles: ['ADMIN'] });

    const removeRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/roles/remove/user%2F1`);
    expect(removeRequest.request.method).toBe('DELETE');
    expect(removeRequest.request.body).toBe(payload);
    removeRequest.flush({ roles: [] });
  });

  it('locks, unlocks and changes passwords through admin endpoints', () => {
    const lockPayload = { userId: 'user-1' } as never;
    const passwordPayload = { newPassword: 'Secret123!' } as never;

    service.lockUser(lockPayload).subscribe();
    service.unlockUser(lockPayload).subscribe();
    service.changeUserPassword('user-1', passwordPayload).subscribe();

    const lockRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/lock`);
    expect(lockRequest.request.method).toBe('POST');
    expect(lockRequest.request.body).toBe(lockPayload);
    lockRequest.flush({});

    const unlockRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/unlock`);
    expect(unlockRequest.request.method).toBe('POST');
    expect(unlockRequest.request.body).toBe(lockPayload);
    unlockRequest.flush({});

    const passwordRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/change-password?idUser=user-1`);
    expect(passwordRequest.request.method).toBe('POST');
    expect(passwordRequest.request.body).toBe(passwordPayload);
    passwordRequest.flush({ message: 'ok' });
  });
});
