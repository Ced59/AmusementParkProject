import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  AdminContactGrievance,
  AdminContactGrievanceQuery,
  AdminContactGrievanceResponse
} from '@app/models/contact/contact-grievance.models';
import { Pagination } from '@app/models/shared/pagination';
import {
  ADMIN_CONTACT_GRIEVANCES_DATA_PORT,
  AdminContactGrievancesDataPort
} from './admin-contact-grievances-data.ports';
import { AdminContactGrievancesFacade } from './admin-contact-grievances.facade';

class FakeAdminContactGrievancesPort implements AdminContactGrievancesDataPort {
  public response$: Observable<AdminContactGrievanceResponse> = of(createResponse([createGrievance('grievance-1')], createPagination(2, 10, 12)));
  public readonly calls: AdminContactGrievanceQuery[] = [];

  searchAdminGrievances(query: AdminContactGrievanceQuery): Observable<AdminContactGrievanceResponse> {
    this.calls.push(query);
    return this.response$;
  }
}

function createGrievance(id: string): AdminContactGrievance {
  return {
    id,
    message: 'Suggestion de test.',
    languageCode: 'fr',
    ipAddress: '127.0.0.1',
    userAgent: 'Karma',
    createdAtUtc: '2026-06-17T00:00:00Z'
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

function createResponse(data: AdminContactGrievance[], pagination: Pagination): AdminContactGrievanceResponse {
  return { data, pagination };
}

describe('AdminContactGrievancesFacade', () => {
  let facade: AdminContactGrievancesFacade;
  let port: FakeAdminContactGrievancesPort;

  beforeEach(() => {
    port = new FakeAdminContactGrievancesPort();

    TestBed.configureTestingModule({
      providers: [
        AdminContactGrievancesFacade,
        { provide: ADMIN_CONTACT_GRIEVANCES_DATA_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminContactGrievancesFacade);
  });

  it('loads grievances with pagination state from the admin port', () => {
    facade.load({ page: 2, size: 10, search: 'queue' });

    expect(port.calls).toEqual([{ page: 2, size: 10, search: 'queue' }]);
    expect(facade.state().kind).toBe('ready');
    expect(facade.grievances().map((grievance: AdminContactGrievance) => grievance.id)).toEqual(['grievance-1']);
    expect(facade.currentPage()).toBe(2);
    expect(facade.pageSize()).toBe(10);
    expect(facade.totalRecords()).toBe(12);
  });

  it('sets an empty state when no grievance is returned', () => {
    port.response$ = of(createResponse([], createPagination(1, 20, 0)));

    facade.load();

    expect(facade.state().kind).toBe('empty');
    expect(facade.grievances()).toEqual([]);
    expect(facade.totalRecords()).toBe(0);
  });

  it('keeps the admin screen in error state when loading fails', () => {
    port.response$ = throwError(() => new Error('network'));

    facade.load();

    expect(facade.state().kind).toBe('error');
    expect(facade.state().error).toBe('admin.contactGrievances.loadError');
  });
});
