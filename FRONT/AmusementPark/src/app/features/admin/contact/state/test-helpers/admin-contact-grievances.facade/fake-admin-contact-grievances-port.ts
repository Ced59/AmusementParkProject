import { Observable, of } from 'rxjs';

import { AdminContactGrievance, AdminContactGrievanceQuery, AdminContactGrievanceResponse } from '@app/models/contact/contact-grievance.models';

import { Pagination } from '@app/models/shared/pagination';

import { AdminContactGrievancesDataPort } from '../../admin-contact-grievances-data.ports';

function createGrievance(id: string): AdminContactGrievance {
  return {
    id,
    message: 'Suggestion de test.',
    languageCode: 'fr',
    ipAddress: '127.0.0.1',
    userAgent: 'Karma',
    createdAtUtc: '2026-06-17T00:00:00Z',
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
  data: AdminContactGrievance[],
  pagination: Pagination,
): AdminContactGrievanceResponse {
  return { data, pagination };
}

export class FakeAdminContactGrievancesPort implements AdminContactGrievancesDataPort {
  public response$: Observable<AdminContactGrievanceResponse> = of(
    createResponse(
      [createGrievance('grievance-1')],
      createPagination(2, 10, 12),
    ),
  );
  public readonly calls: AdminContactGrievanceQuery[] = [];

  searchAdminGrievances(
    query: AdminContactGrievanceQuery,
  ): Observable<AdminContactGrievanceResponse> {
    this.calls.push(query);
    return this.response$;
  }
}
