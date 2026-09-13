import { Observable, of } from 'rxjs';

import { Pagination } from '@app/models/shared/pagination';

import { UserDto } from '@app/models/users/user_dto';

import { UsersApiResponse } from '@app/models/users/users_api_response';

import { AdminUsersStateUsersApiServicePort } from '../../admin-users-state-data.ports';

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
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createPagination(
  currentPage: number,
  itemsPerPage: number,
  totalItems: number,
): Pagination {
  return {
    currentPage,
    itemsPerPage,
    totalItems,
    totalPages: Math.ceil(totalItems / itemsPerPage),
  };
}

function createResponse(
  data: UserDto[],
  pagination: Pagination,
): UsersApiResponse {
  return { data, pagination };
}

export class FakeUsersPort implements AdminUsersStateUsersApiServicePort {
  public response$: Observable<UsersApiResponse> = of(
    createResponse([createUser('user-1')], createPagination(1, 10, 1)),
  );
  public readonly calls: {
    page: number;
    size: number;
  }[] = [];

  getUsers(page: number, size: number): Observable<UsersApiResponse> {
    this.calls.push({ page, size });
    return this.response$;
  }
}
