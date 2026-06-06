import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { Pagination } from '@app/models/shared/pagination';
import { UserDto } from '@app/models/users/user_dto';
import { UsersApiResponse } from '@app/models/users/users_api_response';
import {
  ADMIN_USERS_STATE_USERS_API_SERVICE_PORT,
  AdminUsersStateUsersApiServicePort
} from './admin-users-state-data.ports';
import { AdminUsersStateFacade } from './admin-users-state.facade';

class FakeUsersPort implements AdminUsersStateUsersApiServicePort {
  public response$: Observable<UsersApiResponse> = of(createResponse([createUser('user-1')], createPagination(1, 10, 1)));
  public readonly calls: { page: number; size: number }[] = [];

  getUsers(page: number, size: number): Observable<UsersApiResponse> {
    this.calls.push({ page, size });
    return this.response$;
  }
}

function createUser(id: string): UserDto {
  return {
    id,
    email: `${id}@example.test`,
    firstName: 'Test',
    lastName: 'User',
    isActivated: true,
    isBlocked: false,
    roles: ['User'],
    preferredLanguage: 'fr',
    avatarUrl: '',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createPagination(currentPage: number, itemsPerPage: number, totalItems: number): Pagination {
  return {
    currentPage,
    itemsPerPage,
    totalItems,
    totalPages: Math.ceil(totalItems / itemsPerPage)
  };
}

function createResponse(data: UserDto[], pagination: Pagination): UsersApiResponse {
  return { data, pagination };
}

describe('AdminUsersStateFacade', () => {
  let facade: AdminUsersStateFacade;
  let port: FakeUsersPort;

  beforeEach(() => {
    port = new FakeUsersPort();

    TestBed.configureTestingModule({
      providers: [
        AdminUsersStateFacade,
        { provide: ADMIN_USERS_STATE_USERS_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminUsersStateFacade);
  });

  it('loads users and synchronizes pagination signals', () => {
    facade.loadUsers(2, 25);

    expect(port.calls).toEqual([{ page: 2, size: 25 }]);
    expect(facade.users().map((user: UserDto) => user.id)).toEqual(['user-1']);
    expect(facade.totalRecords()).toBe(1);
    expect(facade.currentPage()).toBe(1);
    expect(facade.pageSize()).toBe(10);
    expect(facade.state().kind).toBe('ready');
  });

  it('sets an empty state when the page has no users', () => {
    port.response$ = of(createResponse([], createPagination(1, 10, 0)));

    facade.loadUsers();

    expect(facade.users()).toEqual([]);
    expect(facade.totalRecords()).toBe(0);
    expect(facade.state().kind).toBe('empty');
  });

  it('keeps previous data when a reload fails', () => {
    facade.loadUsers();
    port.response$ = throwError(() => new Error('network'));

    facade.loadUsers(3, 10);

    expect(facade.users().map((user: UserDto) => user.id)).toEqual(['user-1']);
    expect(facade.state().kind).toBe('error');
  });
});
