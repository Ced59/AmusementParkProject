import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import {
  ParkGraphUpsertRequest,
  ParkGraphUpsertResult,
} from '@app/models/admin/park-graph-upsert.models';
import { AdminParkGraphUpsertOperationsFacade } from './admin-park-graph-upsert-operations.facade';
import {
  ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT,
  ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT,
  AdminParkGraphUpsertGraphPort,
  AdminParkGraphUpsertParksPort,
} from './admin-park-graph-upsert-operations.ports';

describe('AdminParkGraphUpsertOperationsFacade', () => {
  let facade: AdminParkGraphUpsertOperationsFacade;
  let parksApi: MockedObject<AdminParkGraphUpsertParksPort>;
  let graphApi: MockedObject<AdminParkGraphUpsertGraphPort>;

  beforeEach(() => {
    parksApi = {
      searchParks: vi
        .fn()
        .mockName('AdminParkGraphUpsertParksPort.searchParks'),
      getParkDataCompletenessScore: vi
        .fn()
        .mockName('AdminParkGraphUpsertParksPort.getParkDataCompletenessScore'),
    } as unknown as MockedObject<AdminParkGraphUpsertParksPort>;
    graphApi = {
      downloadParkExport: vi
        .fn()
        .mockName('AdminParkGraphUpsertGraphPort.downloadParkExport'),
      preview: vi.fn().mockName('AdminParkGraphUpsertGraphPort.preview'),
      apply: vi.fn().mockName('AdminParkGraphUpsertGraphPort.apply'),
    } as unknown as MockedObject<AdminParkGraphUpsertGraphPort>;

    TestBed.configureTestingModule({
      providers: [
        AdminParkGraphUpsertOperationsFacade,
        { provide: ADMIN_PARK_GRAPH_UPSERT_PARKS_PORT, useValue: parksApi },
        { provide: ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT, useValue: graphApi },
      ],
    });
    facade = TestBed.inject(AdminParkGraphUpsertOperationsFacade);
  });

  it('keeps the admin park search contract in one orchestration boundary', () => {
    const response = {
      data: [],
      pagination: {
        totalItems: 0,
        currentPage: 1,
        itemsPerPage: 10,
        totalPages: 0,
      },
    };
    parksApi.searchParks.mockReturnValue(of(response));

    facade.searchParks('Europa').subscribe();

    expect(parksApi.searchParks).toHaveBeenCalledTimes(1);

    expect(parksApi.searchParks).toHaveBeenCalledWith(
      'Europa',
      1,
      10,
      false,
      null,
      null,
    );
  });

  it('delegates preview and apply without changing their request', () => {
    const request: ParkGraphUpsertRequest = {
      createIfMissing: false,
      replaceCollections: false,
      document: {},
    };
    const result = { canApply: true } as ParkGraphUpsertResult;
    graphApi.preview.mockReturnValue(of(result));
    graphApi.apply.mockReturnValue(of(result));

    facade.preview(request).subscribe();
    facade.apply(request).subscribe();

    expect(graphApi.preview).toHaveBeenCalledTimes(1);

    expect(graphApi.preview).toHaveBeenCalledWith(request);
    expect(graphApi.apply).toHaveBeenCalledTimes(1);
    expect(graphApi.apply).toHaveBeenCalledWith(request);
  });
});
