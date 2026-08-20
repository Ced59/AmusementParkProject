import type { MockedObject } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';
import { Park } from '@app/models/parks/park';
import {
  ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT,
  AdminParkGraphUpsertGraphPort
} from '../../park-graph-upserts/state/admin-park-graph-upsert-operations.ports';
import { AdminStandaloneAttractionsFacade } from './admin-standalone-attractions.facade';
import {
  ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT,
  ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT,
  AdminStandaloneAttractionsDataPort,
  AdminStandaloneAttractionsParksPort
} from './admin-standalone-attractions.ports';

describe('AdminStandaloneAttractionsFacade', () => {
  let facade: AdminStandaloneAttractionsFacade;
  let dataApi: MockedObject<AdminStandaloneAttractionsDataPort>;
  let parksApi: MockedObject<AdminStandaloneAttractionsParksPort>;
  let graphApi: MockedObject<AdminParkGraphUpsertGraphPort>;

  beforeEach(() => {
    dataApi = {
      getAdminPage: vi.fn(),
      getAdminById: vi.fn(),
      create: vi.fn(),
      update: vi.fn(),
      updateBulkAdministration: vi.fn(),
      migrateFromPark: vi.fn(),
      downloadExport: vi.fn()
    } as unknown as MockedObject<AdminStandaloneAttractionsDataPort>;
    parksApi = {
      getParkById: vi.fn(),
      searchParks: vi.fn()
    } as unknown as MockedObject<AdminStandaloneAttractionsParksPort>;
    graphApi = {
      downloadParkExport: vi.fn(),
      preview: vi.fn(),
      apply: vi.fn()
    } as unknown as MockedObject<AdminParkGraphUpsertGraphPort>;

    TestBed.configureTestingModule({
      providers: [
        AdminStandaloneAttractionsFacade,
        { provide: ADMIN_STANDALONE_ATTRACTIONS_DATA_PORT, useValue: dataApi },
        { provide: ADMIN_STANDALONE_ATTRACTIONS_PARKS_PORT, useValue: parksApi },
        { provide: ADMIN_PARK_GRAPH_UPSERT_GRAPH_PORT, useValue: graphApi }
      ]
    });
    facade = TestBed.inject(AdminStandaloneAttractionsFacade);
  });

  it('resolves a legacy park id without broad search orchestration in the component', async () => {
    const park: Park = {
      id: 'b2ddc5c4-bfa5-430b-bcbb-5ba8c6a183cb',
      name: 'Legacy',
      latitude: 0,
      longitude: 0
    };
    parksApi.getParkById.mockReturnValue(of(park));

    const result: Park[] = await firstValueFrom(facade.loadLegacyParks(park.id!, null));

    expect(result).toEqual([park]);
    expect(parksApi.getParkById).toHaveBeenCalledWith(park.id);
    expect(parksApi.searchParks).not.toHaveBeenCalled();
  });

  it('keeps standalone JSON preview and apply behind the graph upsert port', () => {
    const request: ParkGraphUpsertRequest = {
      targetParkId: null,
      createIfMissing: false,
      replaceCollections: false,
      document: { documentType: 'standaloneAttractionGraph' }
    };
    const result = { canApply: true } as ParkGraphUpsertResult;
    graphApi.preview.mockReturnValue(of(result));
    graphApi.apply.mockReturnValue(of(result));

    facade.previewImport(request).subscribe();
    facade.applyImport(request).subscribe();

    expect(graphApi.preview).toHaveBeenCalledWith(request);
    expect(graphApi.apply).toHaveBeenCalledWith(request);
  });
});
