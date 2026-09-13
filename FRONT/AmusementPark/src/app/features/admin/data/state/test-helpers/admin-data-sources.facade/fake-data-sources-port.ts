import { Observable, of } from 'rxjs';

import { DataSourceSummary } from '@app/models/admin/data/data-management.models';

import { AdminDataSourcesDataSourcesApiServicePort } from '../../admin-data-sources-data.ports';

function createSource(key: string): DataSourceSummary {
  return {
    key,
    label: key,
    description: 'Source de test',
    icon: 'pi pi-test',
    isEnabled: true,
    lastImportUtc: null,
    totalSessions: 1,
    statusLabel: 'Disponible',
  };
}

export class FakeDataSourcesPort implements AdminDataSourcesDataSourcesApiServicePort {
  public response$: Observable<DataSourceSummary[]> = of([
    createSource('captain-coaster'),
  ]);
  public callCount = 0;

  listSources(): Observable<DataSourceSummary[]> {
    this.callCount += 1;
    return this.response$;
  }
}
