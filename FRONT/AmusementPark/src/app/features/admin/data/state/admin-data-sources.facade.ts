import {
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { DataSourceSummary } from '@app/models/admin/data/data-management.models';

import {
  ADMIN_DATA_SOURCES_DATA_SOURCES_API_SERVICE_PORT,
  AdminDataSourcesDataSourcesApiServicePort
} from './admin-data-sources-data.ports';
@Injectable()
export class AdminDataSourcesFacade {
  private readonly dataSourcesSignal = signal<DataSourceSummary[]>([]);
  private readonly selectedSourceKeySignal = signal<string | null>(null);

  public readonly dataSources: Signal<DataSourceSummary[]> = this.dataSourcesSignal.asReadonly();
  public readonly selectedSourceKey: Signal<string | null> = this.selectedSourceKeySignal.asReadonly();
  public readonly selectedSource = computed(() => {
    const selectedSourceKey: string | null = this.selectedSourceKeySignal();
    return this.dataSourcesSignal().find((source: DataSourceSummary) => source.key === selectedSourceKey) ?? null;
  });

  constructor(@Inject(ADMIN_DATA_SOURCES_DATA_SOURCES_API_SERVICE_PORT) private readonly dataSourcesApiService: AdminDataSourcesDataSourcesApiServicePort) {
  }

  async loadSourcesAsync(): Promise<void> {
    try {
      const sources: DataSourceSummary[] = await firstValueFrom(this.dataSourcesApiService.listSources());
      this.dataSourcesSignal.set(sources);
    } catch {
      this.dataSourcesSignal.set([{
        key: 'captain-coaster',
        label: 'Captain Coaster',
        description: 'Acquisition automatisée Captain Coaster via sitemap, URLs ciblées et pipeline de comparaison.',
        icon: 'pi pi-cloud-download',
        isEnabled: false,
        lastImportUtc: null,
        totalSessions: 0,
        statusLabel: 'Indisponible'
      }]);
    }
  }

  selectSource(sourceKey: string): void {
    this.selectedSourceKeySignal.set(sourceKey);
  }

  clearSelection(): void {
    this.selectedSourceKeySignal.set(null);
  }
}
