import {
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterDuplicateResolutionRequest,
  CaptainCoasterExternalVariantResponse,
  CaptainCoasterFieldResolutionRequest
} from '@app/models/admin/data/data-management.models';

export interface DuplicateResolutionState {
  strategy: 'SelectVariant' | 'Merge';
  selectedExternalVariantId: string | null;
  fieldSelections: Record<string, string>;
}

const mergeEligibleFieldOrder: readonly string[] = [
  'name',
  'countryCode',
  'parkName',
  'manufacturer',
  'model',
  'sourceUrl',
  'status',
  'materialType',
  'seatingType',
  'launchType',
  'restraintType',
  'isLaunched',
  'openingDate',
  'closingDate',
  'heightInMeters',
  'lengthInMeters',
  'speedInKmH',
  'inversionCount'
];

const fieldsExcludedFromManualMerge: ReadonlySet<string> = new Set<string>([
  'duplicateVariants',
  'externalId',
  'externalSource',
  'heightInFeet',
  'lengthInFeet',
  'speedInMph'
]);

export function ensureDuplicateResolutionStates(
  items: CaptainCoasterComparisonResultResponse[],
  stateByResultId: Map<string, DuplicateResolutionState>
): void {
  for (const item of items) {
    ensureDuplicateResolutionState(item, stateByResultId);
  }
}

export function ensureDuplicateResolutionState(
  item: CaptainCoasterComparisonResultResponse,
  stateByResultId: Map<string, DuplicateResolutionState>
): void {
  if (!item.requiresManualResolution || stateByResultId.has(item.id)) {
    return;
  }

  const suggestedVariantId: string | null = getSuggestedVariantId(item);
  const fieldSelections: Record<string, string> = {};

  for (const field of getMergeEligibleFields(item)) {
    fieldSelections[field] = suggestedVariantId === null ? 'local' : `variant:${suggestedVariantId}`;
  }

  stateByResultId.set(item.id, {
    strategy: 'SelectVariant',
    selectedExternalVariantId: suggestedVariantId,
    fieldSelections
  });
}

export function getSuggestedVariantId(item: CaptainCoasterComparisonResultResponse): string | null {
  return item.externalVariants.find((variant: CaptainCoasterExternalVariantResponse) => variant.isSuggested)?.externalVariantId
    ?? item.externalVariants[0]?.externalVariantId
    ?? null;
}

export function getMergeEligibleFields(item: CaptainCoasterComparisonResultResponse): string[] {
  const fields: Set<string> = new Set<string>();

  for (const variant of item.externalVariants) {
    for (const change of variant.changes) {
      if (isMergeEligibleField(change.field)) {
        fields.add(change.field);
      }
    }
  }

  return Array.from(fields).sort((left: string, right: string) => compareMergeEligibleFields(left, right));
}

export function getExternalFieldValue(variant: CaptainCoasterExternalVariantResponse, field: string): string | null {
  const change: { externalValue: string | null } | undefined = variant.changes.find((item) => item.field === field);
  return change?.externalValue ?? null;
}

export function buildDuplicateResolutions(
  items: CaptainCoasterComparisonResultResponse[],
  stateByResultId: Map<string, DuplicateResolutionState>
): CaptainCoasterDuplicateResolutionRequest[] {
  const resolutions: CaptainCoasterDuplicateResolutionRequest[] = [];

  for (const item of items) {
    if (!item.requiresManualResolution) {
      continue;
    }

    const state: DuplicateResolutionState | undefined = stateByResultId.get(item.id);
    if (!state) {
      continue;
    }

    if (state.strategy === 'SelectVariant') {
      if (state.selectedExternalVariantId === null) {
        continue;
      }

      resolutions.push({
        comparisonResultId: item.id,
        strategy: 'SelectVariant',
        selectedExternalVariantId: state.selectedExternalVariantId,
        fieldResolutions: []
      });
      continue;
    }

    const fieldResolutions: CaptainCoasterFieldResolutionRequest[] = getMergeEligibleFields(item)
      .map((field: string) => buildFieldResolution(field, state.fieldSelections[field] ?? 'local'));

    resolutions.push({
      comparisonResultId: item.id,
      strategy: 'Merge',
      selectedExternalVariantId: state.selectedExternalVariantId,
      fieldResolutions
    });
  }

  return resolutions;
}

function isMergeEligibleField(field: string): boolean {
  return !fieldsExcludedFromManualMerge.has(field);
}

function compareMergeEligibleFields(left: string, right: string): number {
  const leftIndex: number = mergeEligibleFieldOrder.indexOf(left);
  const rightIndex: number = mergeEligibleFieldOrder.indexOf(right);
  const safeLeftIndex: number = leftIndex === -1 ? 999 : leftIndex;
  const safeRightIndex: number = rightIndex === -1 ? 999 : rightIndex;

  if (safeLeftIndex !== safeRightIndex) {
    return safeLeftIndex - safeRightIndex;
  }

  return left.localeCompare(right);
}

function buildFieldResolution(field: string, rawSelection: string): CaptainCoasterFieldResolutionRequest {
  if (rawSelection === 'local') {
    return {
      field,
      sourceType: 'Local',
      externalVariantId: null
    };
  }

  const variantId: string = rawSelection.replace('variant:', '');
  return {
    field,
    sourceType: 'Variant',
    externalVariantId: variantId
  };
}
