import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterDuplicateResolutionRequest,
  CaptainCoasterSessionResponse,
  CaptainCoasterSettingsResponse,
  CaptainCoasterStatusResponse,
  ComparisonFilters,
  DataSourceSummary,
  StartCaptainCoasterImportRequest,
  UpdateCaptainCoasterSettingsRequest
} from '@app/models/admin/data/data-management.models';

interface PagedResponseDto<TItem> {
  data: TItem[];
  pagination?: {
    totalItems: number;
    totalPages: number;
    currentPage: number;
    itemsPerPage: number;
  };
}

interface DataSourceStatusApiDto {
  sourceKey: string;
  displayName: string;
  isEnabled: boolean;
  lastSuccessfulImportUtc: string | null;
  totalSessionsCount: number;
}

interface DataSourceSettingsApiDto {
  sourceKey: string;
  displayName: string;
  isEnabled: boolean;
  options: Record<string, string | null>;
}

interface DataSourceSessionApiDto {
  sessionId: string;
  sourceKey: string;
  status: string;
  importKind: string;
  progressPercentage: number;
  currentStep: string;
  lastCompletedStep: string | null;
  message: string;
  canResume: boolean;
  availableSteps: string[];
  startedAtUtc: string;
  completedAtUtc: string | null;
  metrics: {
    itemsFetchedPrimary: number;
    itemsFetchedSecondary: number;
    comparisonResults: number;
    appliedChanges: number;
    duplicateConflicts: number;
    discoveredItems: number;
    processedItems: number;
    failedItems: number;
    skippedItems: number;
  };
  logs: Array<{
    occurredAtUtc: string;
    level: string;
    message: string;
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class CaptainCoasterDataService {
  private readonly sourcesUrl: string = `${environment.apiBaseUrl}admin/data-sources`;
  private readonly baseUrl: string = `${this.sourcesUrl}/captain-coaster`;

  constructor(private readonly http: HttpClient) {}

  listSources(): Observable<DataSourceSummary[]> {
    return this.http.get<PagedResponseDto<DataSourceStatusApiDto>>(this.sourcesUrl).pipe(
      map((response: PagedResponseDto<DataSourceStatusApiDto>) =>
        (response.data ?? []).map((source: DataSourceStatusApiDto) => this.mapSourceSummary(source))
      )
    );
  }

  getStatus(): Observable<CaptainCoasterStatusResponse> {
    return this.http.get<DataSourceStatusApiDto>(`${this.baseUrl}/status`).pipe(
      map((status: DataSourceStatusApiDto) => this.mapStatus(status))
    );
  }

  getSettings(): Observable<CaptainCoasterSettingsResponse> {
    return this.http.get<DataSourceSettingsApiDto>(`${this.baseUrl}/settings`).pipe(
      map((settings: DataSourceSettingsApiDto) => ({
        sourceKey: settings.sourceKey,
        displayName: settings.displayName,
        isEnabled: settings.isEnabled,
        options: settings.options ?? {}
      }))
    );
  }

  updateSettings(request: UpdateCaptainCoasterSettingsRequest): Observable<CaptainCoasterSettingsResponse> {
    return this.http.put<DataSourceSettingsApiDto>(`${this.baseUrl}/settings`, request).pipe(
      map((settings: DataSourceSettingsApiDto) => ({
        sourceKey: settings.sourceKey,
        displayName: settings.displayName,
        isEnabled: settings.isEnabled,
        options: settings.options ?? {}
      }))
    );
  }

  getLatestSession(): Observable<CaptainCoasterSessionResponse | null> {
    return this.http.get<DataSourceSessionApiDto | null>(`${this.baseUrl}/sessions/latest`, {
      observe: 'response'
    }).pipe(
      map((response: HttpResponse<DataSourceSessionApiDto | null>) => {
        if (response.status === 204 || response.body === null) {
          return null;
        }

        return this.mapSession(response.body);
      })
    );
  }

  getSessionById(sessionId: string): Observable<CaptainCoasterSessionResponse | null> {
    return this.http.get<DataSourceSessionApiDto | null>(`${this.baseUrl}/sessions/${sessionId}`, {
      observe: 'response'
    }).pipe(
      map((response: HttpResponse<DataSourceSessionApiDto | null>) => {
        if (response.status === 204 || response.body === null) {
          return null;
        }

        return this.mapSession(response.body);
      })
    );
  }

  startImport(request: StartCaptainCoasterImportRequest): Observable<CaptainCoasterSessionResponse> {
    return this.http.post<DataSourceSessionApiDto>(`${this.baseUrl}/import`, request).pipe(
      map((session: DataSourceSessionApiDto) => this.mapSession(session))
    );
  }

  getComparisonResults(
    sessionId: string | null | undefined,
    filters: ComparisonFilters,
    page: number,
    pageSize: number
  ): Observable<CaptainCoasterComparisonPagedResponse> {
    let params: HttpParams = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (sessionId) {
      params = params.set('sessionId', sessionId);
    }
    if (filters.entityType) {
      params = params.set('entityType', filters.entityType);
    }
    if (filters.changeType) {
      params = params.set('changeType', filters.changeType);
    }
    if (filters.isApplied !== null && filters.isApplied !== undefined) {
      params = params.set('isApplied', filters.isApplied.toString());
    }

    return this.http.get<CaptainCoasterComparisonPagedResponse>(
      `${this.baseUrl}/comparison-results`,
      { params }
    );
  }

  applySelectedIds(
    ids: string[],
    duplicateResolutions: CaptainCoasterDuplicateResolutionRequest[]
  ): Observable<{ appliedCount: number }> {
    return this.http.post<{ appliedCount: number }>(`${this.baseUrl}/apply`, {
      comparisonResultIds: ids,
      applyAll: false,
      duplicateResolutions
    });
  }

  applyAll(
    sessionId: string | null,
    entityTypeFilter: string | null,
    changeTypeFilter: string | null
  ): Observable<{ appliedCount: number }> {
    return this.http.post<{ appliedCount: number }>(`${this.baseUrl}/apply`, {
      comparisonResultIds: [],
      applyAll: true,
      sessionId,
      entityTypeFilter,
      changeTypeFilter,
      duplicateResolutions: []
    });
  }

  private mapSourceSummary(source: DataSourceStatusApiDto): DataSourceSummary {
    return {
      key: source.sourceKey,
      label: source.displayName,
      description: this.resolveSourceDescription(source.sourceKey),
      icon: this.resolveSourceIcon(source.sourceKey),
      isEnabled: source.isEnabled,
      lastImportUtc: source.lastSuccessfulImportUtc,
      totalSessions: source.totalSessionsCount,
      statusLabel: this.resolveSourceStatusLabel(source)
    };
  }

  private mapStatus(status: DataSourceStatusApiDto): CaptainCoasterStatusResponse {
    return {
      source: status.sourceKey,
      isEnabled: status.isEnabled,
      lastSuccessfulImportUtc: status.lastSuccessfulImportUtc,
      totalSessionsCount: status.totalSessionsCount
    };
  }

  private mapSession(session: DataSourceSessionApiDto): CaptainCoasterSessionResponse {
    return {
      id: session.sessionId,
      status: session.status,
      importKind: session.importKind,
      progressPercentage: session.progressPercentage,
      currentStep: session.currentStep,
      lastCompletedStep: session.lastCompletedStep,
      message: session.message,
      canResume: session.canResume,
      availableSteps: session.availableSteps ?? [],
      startedAtUtc: session.startedAtUtc,
      completedAtUtc: session.completedAtUtc,
      parksFetched: session.metrics?.itemsFetchedPrimary ?? 0,
      coastersFetched: session.metrics?.itemsFetchedSecondary ?? 0,
      comparisonResults: session.metrics?.comparisonResults ?? 0,
      appliedChanges: session.metrics?.appliedChanges ?? 0,
      duplicateConflicts: session.metrics?.duplicateConflicts ?? 0,
      discoveredItems: session.metrics?.discoveredItems ?? 0,
      processedItems: session.metrics?.processedItems ?? 0,
      failedItems: session.metrics?.failedItems ?? 0,
      skippedItems: session.metrics?.skippedItems ?? 0,
      logs: session.logs ?? []
    };
  }

  private resolveSourceDescription(sourceKey: string): string {
    if (sourceKey === 'captain-coaster') {
      return 'Acquisition automatisée Captain Coaster via sitemap, URLs ciblées et pipeline de comparaison.';
    }

    return 'Source de données externe.';
  }

  private resolveSourceIcon(sourceKey: string): string {
    if (sourceKey === 'captain-coaster') {
      return 'pi pi-cloud-download';
    }

    return 'pi pi-database';
  }

  private resolveSourceStatusLabel(source: DataSourceStatusApiDto): string {
    if (!source.isEnabled) {
      return 'Désactivée';
    }

    if (source.lastSuccessfulImportUtc) {
      return 'Active';
    }

    return 'Jamais importée';
  }
}
