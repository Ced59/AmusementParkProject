import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../environments/environment';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { UsersApiService } from './users-api.service';

describe('UsersApiService', () => {
  let service: UsersApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(UsersApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('gets users with default pagination', () => {
    service.getUsers().subscribe();

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}users?page=1&size=10`);
    expect(request.request.method).toBe('GET');
    request.flush({ data: [] });
  });

  it('gets users with explicit pagination and a user by id', () => {
    service.getUsers(3, 50).subscribe();
    service.getUserById('user-1').subscribe();

    const usersRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users?page=3&size=50`);
    expect(usersRequest.request.method).toBe('GET');
    usersRequest.flush({ data: [] });

    const userRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/user-1`);
    expect(userRequest.request.method).toBe('GET');
    userRequest.flush({ id: 'user-1' });
  });

  it('puts user payloads as JSON strings even for null payloads', () => {
    service.putUserById('user-1', { firstName: 'Ada' } as never).subscribe();
    service.putUserById(null, null).subscribe();

    const putRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/user-1`);
    expect(putRequest.request.method).toBe('PUT');
    expect(putRequest.request.headers.get('Content-Type')).toBe('application/json');
    expect(putRequest.request.body).toBe(JSON.stringify({ firstName: 'Ada' }));
    putRequest.flush({ id: 'user-1' });

    const nullRequest = httpTestingController.expectOne(`${environment.apiBaseUrl}users/null`);
    expect(nullRequest.request.body).toBe(JSON.stringify(null));
    nullRequest.flush({ id: 'null' });
  });
});
