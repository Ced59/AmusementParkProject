import { Observable, of } from 'rxjs';

import { ParkGraphUpsertRequest, ParkGraphUpsertResult } from '@app/models/admin/park-graph-upsert.models';

function createUpsertResult(
  isApplied: boolean,
  canApply: boolean,
): ParkGraphUpsertResult {
  return {
    operationId: 'operation-1',
    mode: isApplied ? 'Apply' : 'Preview',
    isApplied,
    canApply,
    previewedAtUtc: '2026-08-20T10:00:00Z',
    appliedAtUtc: isApplied ? '2026-08-20T10:01:00Z' : null,
    targetStandaloneAttractionId: 'standalone-1',
    targetStandaloneAttractionName: 'Bardonecchia Alpine Coaster',
    counts: {
      created: 0,
      updated: 1,
      deleted: 0,
      unchanged: 0,
      warnings: 0,
      errors: 0,
    },
    changes: [],
    warnings: [],
    errors: [],
  };
}

export class FakeParkGraphUpsertsApiService {
  public previewResponse$: Observable<ParkGraphUpsertResult> = of(
    createUpsertResult(false, true),
  );
  public applyResponse$: Observable<ParkGraphUpsertResult> = of(
    createUpsertResult(true, false),
  );
  public readonly previewCalls: ParkGraphUpsertRequest[] = [];
  public readonly applyCalls: ParkGraphUpsertRequest[] = [];

  preview(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    this.previewCalls.push(request);
    return this.previewResponse$;
  }

  apply(request: ParkGraphUpsertRequest): Observable<ParkGraphUpsertResult> {
    this.applyCalls.push(request);
    return this.applyResponse$;
  }
}
