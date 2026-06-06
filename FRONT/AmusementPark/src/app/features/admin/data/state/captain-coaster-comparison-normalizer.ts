import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterExternalVariantResponse,
  CaptainCoasterFieldChangeResponse
} from '@app/models/admin/data/data-management.models';

interface RawCaptainCoasterComparisonPagedResponse {
  items?: RawCaptainCoasterComparisonResultResponse[];
  Items?: RawCaptainCoasterComparisonResultResponse[];
  totalCount?: number;
  TotalCount?: number;
  page?: number;
  Page?: number;
  pageSize?: number;
  PageSize?: number;
  sessionUpdatedCount?: number;
  SessionUpdatedCount?: number;
  sessionMissingCount?: number;
  SessionMissingCount?: number;
  sessionDuplicateCount?: number;
  SessionDuplicateCount?: number;
  sessionAppliedCount?: number;
  SessionAppliedCount?: number;
}

interface RawCaptainCoasterComparisonResultResponse {
  id?: string;
  Id?: string;
  entityType?: string;
  EntityType?: string;
  changeType?: string;
  ChangeType?: string;
  displayName?: string;
  DisplayName?: string;
  localEntityId?: string | null;
  LocalEntityId?: string | null;
  externalEntityId?: string | null;
  ExternalEntityId?: string | null;
  matchConfidence?: string;
  MatchConfidence?: string;
  isApplied?: boolean;
  IsApplied?: boolean;
  hasExternalDuplicates?: boolean;
  HasExternalDuplicates?: boolean;
  requiresManualResolution?: boolean;
  RequiresManualResolution?: boolean;
  resolutionStatus?: string;
  ResolutionStatus?: string;
  appliedExternalVariantId?: string | null;
  AppliedExternalVariantId?: string | null;
  changes?: RawCaptainCoasterFieldChangeResponse[];
  Changes?: RawCaptainCoasterFieldChangeResponse[];
  externalVariants?: RawCaptainCoasterExternalVariantResponse[];
  ExternalVariants?: RawCaptainCoasterExternalVariantResponse[];
}

interface RawCaptainCoasterExternalVariantResponse {
  externalVariantId?: string;
  ExternalVariantId?: string;
  displayLabel?: string;
  DisplayLabel?: string;
  candidateLocalEntityId?: string | null;
  CandidateLocalEntityId?: string | null;
  sourceUrl?: string | null;
  SourceUrl?: string | null;
  isSuggested?: boolean;
  IsSuggested?: boolean;
  changes?: RawCaptainCoasterFieldChangeResponse[];
  Changes?: RawCaptainCoasterFieldChangeResponse[];
}

interface RawCaptainCoasterFieldChangeResponse {
  field?: string;
  Field?: string;
  localValue?: string | null;
  LocalValue?: string | null;
  externalValue?: string | null;
  ExternalValue?: string | null;
  isDifferent?: boolean;
  IsDifferent?: boolean;
}

export function normalizeCaptainCoasterComparisonPage(
  rawResult: CaptainCoasterComparisonPagedResponse | RawCaptainCoasterComparisonPagedResponse,
  fallbackPage: number,
  fallbackPageSize: number
): CaptainCoasterComparisonPagedResponse {
  const normalizedRawResult: RawCaptainCoasterComparisonPagedResponse = rawResult as RawCaptainCoasterComparisonPagedResponse;
  const rawItems: RawCaptainCoasterComparisonResultResponse[] = normalizedRawResult.items ?? normalizedRawResult.Items ?? [];

  return {
    items: rawItems.map((item: RawCaptainCoasterComparisonResultResponse) => normalizeCaptainCoasterComparisonItem(item)),
    totalCount: normalizedRawResult.totalCount ?? normalizedRawResult.TotalCount ?? rawItems.length,
    page: normalizedRawResult.page ?? normalizedRawResult.Page ?? fallbackPage,
    pageSize: normalizedRawResult.pageSize ?? normalizedRawResult.PageSize ?? fallbackPageSize,
    sessionUpdatedCount: normalizedRawResult.sessionUpdatedCount ?? normalizedRawResult.SessionUpdatedCount ?? 0,
    sessionMissingCount: normalizedRawResult.sessionMissingCount ?? normalizedRawResult.SessionMissingCount ?? 0,
    sessionDuplicateCount: normalizedRawResult.sessionDuplicateCount ?? normalizedRawResult.SessionDuplicateCount ?? 0,
    sessionAppliedCount: normalizedRawResult.sessionAppliedCount ?? normalizedRawResult.SessionAppliedCount ?? 0
  };
}

function normalizeCaptainCoasterComparisonItem(rawItem: RawCaptainCoasterComparisonResultResponse): CaptainCoasterComparisonResultResponse {
  const rawChanges: RawCaptainCoasterFieldChangeResponse[] = rawItem.changes ?? rawItem.Changes ?? [];
  const rawVariants: RawCaptainCoasterExternalVariantResponse[] = rawItem.externalVariants ?? rawItem.ExternalVariants ?? [];

  return {
    id: rawItem.id ?? rawItem.Id ?? '',
    entityType: rawItem.entityType ?? rawItem.EntityType ?? '',
    changeType: rawItem.changeType ?? rawItem.ChangeType ?? '',
    displayName: rawItem.displayName ?? rawItem.DisplayName ?? '',
    localEntityId: rawItem.localEntityId ?? rawItem.LocalEntityId ?? null,
    externalEntityId: rawItem.externalEntityId ?? rawItem.ExternalEntityId ?? null,
    matchConfidence: rawItem.matchConfidence ?? rawItem.MatchConfidence ?? '',
    isApplied: rawItem.isApplied ?? rawItem.IsApplied ?? false,
    hasExternalDuplicates: rawItem.hasExternalDuplicates ?? rawItem.HasExternalDuplicates ?? false,
    requiresManualResolution: rawItem.requiresManualResolution ?? rawItem.RequiresManualResolution ?? false,
    resolutionStatus: rawItem.resolutionStatus ?? rawItem.ResolutionStatus ?? '',
    appliedExternalVariantId: rawItem.appliedExternalVariantId ?? rawItem.AppliedExternalVariantId ?? null,
    changes: rawChanges.map((change: RawCaptainCoasterFieldChangeResponse) => normalizeCaptainCoasterFieldChange(change)),
    externalVariants: rawVariants.map((variant: RawCaptainCoasterExternalVariantResponse) => normalizeCaptainCoasterExternalVariant(variant))
  };
}

function normalizeCaptainCoasterExternalVariant(rawVariant: RawCaptainCoasterExternalVariantResponse): CaptainCoasterExternalVariantResponse {
  const rawChanges: RawCaptainCoasterFieldChangeResponse[] = rawVariant.changes ?? rawVariant.Changes ?? [];

  return {
    externalVariantId: rawVariant.externalVariantId ?? rawVariant.ExternalVariantId ?? '',
    displayLabel: rawVariant.displayLabel ?? rawVariant.DisplayLabel ?? '',
    candidateLocalEntityId: rawVariant.candidateLocalEntityId ?? rawVariant.CandidateLocalEntityId ?? null,
    sourceUrl: rawVariant.sourceUrl ?? rawVariant.SourceUrl ?? null,
    isSuggested: rawVariant.isSuggested ?? rawVariant.IsSuggested ?? false,
    changes: rawChanges.map((change: RawCaptainCoasterFieldChangeResponse) => normalizeCaptainCoasterFieldChange(change))
  };
}

function normalizeCaptainCoasterFieldChange(rawChange: RawCaptainCoasterFieldChangeResponse): CaptainCoasterFieldChangeResponse {
  return {
    field: rawChange.field ?? rawChange.Field ?? '',
    localValue: rawChange.localValue ?? rawChange.LocalValue ?? null,
    externalValue: rawChange.externalValue ?? rawChange.ExternalValue ?? null,
    isDifferent: rawChange.isDifferent ?? rawChange.IsDifferent ?? false
  };
}
