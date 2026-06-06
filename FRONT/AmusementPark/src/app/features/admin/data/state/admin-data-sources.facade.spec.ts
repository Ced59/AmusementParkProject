import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { DataSourceSummary } from '@app/models/admin/data/data-management.models';
import {
  ADMIN_DATA_SOURCES_DATA_SOURCES_API_SERVICE_PORT,
  AdminDataSourcesDataSourcesApiServicePort
} from './admin-data-sources-data.ports';
import { AdminDataSourcesFacade } from './admin-data-sources.facade';

class FakeDataSourcesPort implements AdminDataSourcesDataSourcesApiServicePort {
  public response$: Observable<DataSourceSummary[]> = of([createSource('captain-coaster')]);
  public callCount = 0;

  listSources(): Observable<DataSourceSummary[]> {
    this.callCount += 1;
    return this.response$;
  }
}

function createSource(key: string): DataSourceSummary {
  return {
    key,
    label: key,
    description: 'Source de test',
    icon: 'pi pi-test',
    isEnabled: true,
    lastImportUtc: null,
    totalSessions: 1,
    statusLabel: 'Disponible'
  };
}

describe('AdminDataSourcesFacade', () => {
  let facade: AdminDataSourcesFacade;
  let port: FakeDataSourcesPort;

  beforeEach(() => {
    port = new FakeDataSourcesPort();

    TestBed.configureTestingModule({
      providers: [
        AdminDataSourcesFacade,
        { provide: ADMIN_DATA_SOURCES_DATA_SOURCES_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminDataSourcesFacade);
  });

  it('loads sources from the data port', async () => {
    await facade.loadSourcesAsync();

    expect(port.callCount).toBe(1);
    expect(facade.dataSources().map((source: DataSourceSummary) => source.key)).toEqual(['captain-coaster']);
  });

  it('keeps a fallback source when the data port fails', async () => {
    port.response$ = throwError(() => new Error('network'));

    await facade.loadSourcesAsync();

    expect(facade.dataSources().length).toBe(1);
    expect(facade.dataSources()[0].key).toBe('captain-coaster');
    expect(facade.dataSources()[0].isEnabled).toBeFalse();
  });

  it('selects and clears the current source without reloading data', async () => {
    await facade.loadSourcesAsync();

    facade.selectSource('captain-coaster');

    expect(facade.selectedSourceKey()).toBe('captain-coaster');
    expect(facade.selectedSource()?.key).toBe('captain-coaster');

    facade.clearSelection();

    expect(facade.selectedSourceKey()).toBeNull();
    expect(facade.selectedSource()).toBeNull();
  });
});
