import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterDuplicateResolutionRequest,
  CaptainCoasterExternalVariantResponse,
  CaptainCoasterFieldChangeResponse,
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
export class DataSourcesApiService {
  private readonly sourcesUrl: string = `${environment.apiBaseUrl}admin/data-sources`;
  private readonly baseUrl: string = `${this.sourcesUrl}/captain-coaster`;

  constructor(private readonly http: HttpClient) {
  }

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

    return this.http.get<unknown>(`${this.baseUrl}/comparison-results`, { params }).pipe(
      map((response: unknown) => this.mapComparisonPage(response, page, pageSize))
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



  private mapComparisonPage(response: unknown, requestedPage: number, requestedPageSize: number): CaptainCoasterComparisonPagedResponse {
    const root: Record<string, unknown> = this.asRecord(response);
    const data: unknown = this.readValue(root, 'data', 'Data');
    const payload: Record<string, unknown> = data !== undefined && !Array.isArray(data) && typeof data === 'object' && data !== null
      ? this.asRecord(data)
      : root;
    const pagination: Record<string, unknown> = this.asRecord(this.readValue(root, 'pagination', 'Pagination'));
    const rawItems: unknown = this.readValue(payload, 'items', 'Items');
    const dataItems: unknown = Array.isArray(data) ? data : undefined;
    const items: CaptainCoasterComparisonResultResponse[] = this.asArray(rawItems ?? dataItems)
      .map((item: unknown) => this.mapComparisonItem(item));

    return {
      items,
      totalCount: this.toNumber(
        this.readValue(payload, 'totalCount', 'TotalCount')
          ?? this.readValue(pagination, 'totalItems', 'TotalItems', 'totalRecords', 'TotalRecords'),
        items.length),
      page: this.toNumber(this.readValue(payload, 'page', 'Page'), requestedPage),
      pageSize: this.toNumber(this.readValue(payload, 'pageSize', 'PageSize'), requestedPageSize),
      sessionUpdatedCount: this.toNumber(this.readValue(payload, 'sessionUpdatedCount', 'SessionUpdatedCount'), 0),
      sessionMissingCount: this.toNumber(this.readValue(payload, 'sessionMissingCount', 'SessionMissingCount'), 0),
      sessionDuplicateCount: this.toNumber(this.readValue(payload, 'sessionDuplicateCount', 'SessionDuplicateCount'), 0),
      sessionAppliedCount: this.toNumber(this.readValue(payload, 'sessionAppliedCount', 'SessionAppliedCount'), 0)
    };
  }

  private mapComparisonItem(response: unknown): CaptainCoasterComparisonResultResponse {
    const item: Record<string, unknown> = this.asRecord(response);
    const changes: CaptainCoasterFieldChangeResponse[] = this.asArray(this.readValue(item, 'changes', 'Changes'))
      .map((change: unknown) => this.mapComparisonFieldChange(change));
    const externalVariants: CaptainCoasterExternalVariantResponse[] = this.asArray(this.readValue(item, 'externalVariants', 'ExternalVariants'))
      .map((variant: unknown) => this.mapComparisonExternalVariant(variant));

    return {
      id: this.toStringValue(this.readValue(item, 'id', 'Id')),
      entityType: this.toStringValue(this.readValue(item, 'entityType', 'EntityType')),
      changeType: this.toStringValue(this.readValue(item, 'changeType', 'ChangeType')),
      displayName: this.toStringValue(this.readValue(item, 'displayName', 'DisplayName')),
      localEntityId: this.toNullableStringValue(this.readValue(item, 'localEntityId', 'LocalEntityId')),
      externalEntityId: this.toNullableStringValue(this.readValue(item, 'externalEntityId', 'ExternalEntityId')),
      matchConfidence: this.toStringValue(this.readValue(item, 'matchConfidence', 'MatchConfidence')),
      isApplied: this.toBoolean(this.readValue(item, 'isApplied', 'IsApplied'), false),
      hasExternalDuplicates: this.toBoolean(this.readValue(item, 'hasExternalDuplicates', 'HasExternalDuplicates'), false),
      requiresManualResolution: this.toBoolean(this.readValue(item, 'requiresManualResolution', 'RequiresManualResolution'), false),
      resolutionStatus: this.toStringValue(this.readValue(item, 'resolutionStatus', 'ResolutionStatus')),
      appliedExternalVariantId: this.toNullableStringValue(this.readValue(item, 'appliedExternalVariantId', 'AppliedExternalVariantId')),
      changes,
      externalVariants
    };
  }

  private mapComparisonExternalVariant(response: unknown): CaptainCoasterExternalVariantResponse {
    const variant: Record<string, unknown> = this.asRecord(response);
    const changes: CaptainCoasterFieldChangeResponse[] = this.asArray(this.readValue(variant, 'changes', 'Changes'))
      .map((change: unknown) => this.mapComparisonFieldChange(change));

    return {
      externalVariantId: this.toStringValue(this.readValue(variant, 'externalVariantId', 'ExternalVariantId')),
      displayLabel: this.toStringValue(this.readValue(variant, 'displayLabel', 'DisplayLabel')),
      candidateLocalEntityId: this.toNullableStringValue(this.readValue(variant, 'candidateLocalEntityId', 'CandidateLocalEntityId')),
      sourceUrl: this.toNullableStringValue(this.readValue(variant, 'sourceUrl', 'SourceUrl')),
      isSuggested: this.toBoolean(this.readValue(variant, 'isSuggested', 'IsSuggested'), false),
      changes
    };
  }

  private mapComparisonFieldChange(response: unknown): CaptainCoasterFieldChangeResponse {
    const change: Record<string, unknown> = this.asRecord(response);
    return {
      field: this.toStringValue(this.readValue(change, 'field', 'Field')),
      localValue: this.toNullableStringValue(this.readValue(change, 'localValue', 'LocalValue')),
      externalValue: this.toNullableStringValue(this.readValue(change, 'externalValue', 'ExternalValue')),
      isDifferent: this.toBoolean(this.readValue(change, 'isDifferent', 'IsDifferent'), false)
    };
  }

  private readValue(source: Record<string, unknown>, ...keys: string[]): unknown {
    for (const key of keys) {
      if (Object.prototype.hasOwnProperty.call(source, key)) {
        return source[key];
      }
    }

    return undefined;
  }

  private asRecord(value: unknown): Record<string, unknown> {
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      return value as Record<string, unknown>;
    }

    return {};
  }

  private asArray(value: unknown): unknown[] {
    return Array.isArray(value) ? value : [];
  }

  private toStringValue(value: unknown): string {
    return typeof value === 'string' ? value : value === null || value === undefined ? '' : String(value);
  }

  private toNullableStringValue(value: unknown): string | null {
    const normalizedValue: string = this.toStringValue(value).trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private toNumber(value: unknown, fallback: number): number {
    if (typeof value === 'number' && Number.isFinite(value)) {
      return value;
    }

    if (typeof value === 'string') {
      const parsedValue: number = Number(value);
      return Number.isFinite(parsedValue) ? parsedValue : fallback;
    }

    return fallback;
  }

  private toBoolean(value: unknown, fallback: boolean): boolean {
    if (typeof value === 'boolean') {
      return value;
    }

    if (typeof value === 'string') {
      return value.toLowerCase() === 'true';
    }

    return fallback;
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
