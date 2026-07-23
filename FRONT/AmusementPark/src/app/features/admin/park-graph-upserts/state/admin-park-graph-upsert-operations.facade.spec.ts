import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { AdminParkGraphUpsertOperationsFacade } from './admin-park-graph-upsert-operations.facade';
import {
  ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT,
  ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT,
  AdminParkGraphUpsertGraphPort,
  AdminParkGraphUpsertParksPort
} from './admin-park-graph-upsert-operations.ports';

describe('AdminParkGraphUpsertOperationsFacade', () => {
  let facade: AdminParkGraphUpsertOperationsFacade;
  let parksApi: jasmine.SpyObj<AdminParkGraphUpsertParksPort>;
  let graphApi: jasmine.SpyObj<AdminParkGraphUpsertGraphPort>;

  beforeEach(() => {
    parksApi = jasmine.createSpyObj<AdminParkGraphUpsertParksPort>('AdminParkGraphUpsertParksPort', ['searchParks', 'getParkDataCompletenessScore']);
    graphApi = jasmine.createSpyObj<AdminParkGraphUpsertGraphPort>('AdminParkGraphUpsertGraphPort', ['downloadParkExport', 'preview', 'apply']);

    TestBed.configureTestingModule({
      providers: [
        AdminParkGraphUpsertOperationsFacade,
        { provide: ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT, useValue: parksApi },
        { provide: ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT, useValue: graphApi }
      ]
    });
    facade = TestBed.inject(AdminParkGraphUpsertOperationsFacade);
  });

  it('keeps the admin park search contract in one orchestration boundary', () => {
    const response = {
      data: [],
      pagination: { totalItems: 0, currentPage: 1, itemsPerPage: 10, totalPages: 0 }
    };
    parksApi.searchParks.and.returnValue(of(response));

    facade.searchParks('Europa').subscribe();

    expect(parksApi.searchParks).toHaveBeenCalledOnceWith('Europa', 1, 10, false, null, null);
  });

  it('delegates preview and apply without changing their request', () => {
    const request: ParkGraphUpsertRequest = {
      createIfMissing: false,
      replaceCollections: false,
      document: {}
    };
    const result = { canApply: true } as ParkGraphUpsertResult;
    graphApi.preview.and.returnValue(of(result));
    graphApi.apply.and.returnValue(of(result));

    facade.preview(request).subscribe();
    facade.apply(request).subscribe();

    expect(graphApi.preview).toHaveBeenCalledOnceWith(request);
    expect(graphApi.apply).toHaveBeenCalledOnceWith(request);
  });
});
