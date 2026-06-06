import { CaptainCoasterComparisonResultResponse } from '@app/models/admin/data/data-management.models';
import {
  buildDuplicateResolutions,
  DuplicateResolutionState,
  ensureDuplicateResolutionState,
  getExternalFieldValue,
  getMergeEligibleFields,
  getSuggestedVariantId
} from './captain-coaster-duplicate-resolution.helpers';

function createDuplicateResult(): CaptainCoasterComparisonResultResponse {
  return {
    id: 'comparison-1',
    entityType: 'Coaster',
    changeType: 'DuplicateExternal',
    displayName: 'Taron',
    localEntityId: 'local-1',
    externalEntityId: 'external-1',
    matchConfidence: 'High',
    isApplied: false,
    hasExternalDuplicates: true,
    requiresManualResolution: true,
    resolutionStatus: 'Pending',
    appliedExternalVariantId: null,
    changes: [],
    externalVariants: [
      {
        externalVariantId: 'variant-b',
        displayLabel: 'Variant B',
        candidateLocalEntityId: null,
        sourceUrl: null,
        isSuggested: false,
        changes: [
          { field: 'speedInMph', localValue: '70', externalValue: '73', isDifferent: true },
          { field: 'model', localValue: 'Old model', externalValue: 'Launch Coaster', isDifferent: true }
        ]
      },
      {
        externalVariantId: 'variant-a',
        displayLabel: 'Variant A',
        candidateLocalEntityId: null,
        sourceUrl: null,
        isSuggested: true,
        changes: [
          { field: 'name', localValue: 'Taron old', externalValue: 'Taron', isDifferent: true },
          { field: 'heightInMeters', localValue: '29', externalValue: '30', isDifferent: true }
        ]
      }
    ]
  };
}

describe('captain coaster duplicate resolution helpers', () => {
  it('selects the suggested external variant first', () => {
    expect(getSuggestedVariantId(createDuplicateResult())).toBe('variant-a');
  });

  it('builds ordered merge fields and excludes non-canonical imperial fields', () => {
    expect(getMergeEligibleFields(createDuplicateResult())).toEqual(['name', 'model', 'heightInMeters']);
  });

  it('initializes duplicate resolution state with suggested variant defaults', () => {
    const states: Map<string, DuplicateResolutionState> = new Map<string, DuplicateResolutionState>();

    ensureDuplicateResolutionState(createDuplicateResult(), states);

    expect(states.get('comparison-1')).toEqual({
      strategy: 'SelectVariant',
      selectedExternalVariantId: 'variant-a',
      fieldSelections: {
        name: 'variant:variant-a',
        model: 'variant:variant-a',
        heightInMeters: 'variant:variant-a'
      }
    });
  });

  it('builds merge requests from explicit field selections', () => {
    const item = createDuplicateResult();
    const states: Map<string, DuplicateResolutionState> = new Map<string, DuplicateResolutionState>([
      [
        'comparison-1',
        {
          strategy: 'Merge',
          selectedExternalVariantId: 'variant-a',
          fieldSelections: {
            name: 'local',
            model: 'variant:variant-b',
            heightInMeters: 'variant:variant-a'
          }
        }
      ]
    ]);

    expect(buildDuplicateResolutions([item], states)).toEqual([
      {
        comparisonResultId: 'comparison-1',
        strategy: 'Merge',
        selectedExternalVariantId: 'variant-a',
        fieldResolutions: [
          { field: 'name', sourceType: 'Local', externalVariantId: null },
          { field: 'model', sourceType: 'Variant', externalVariantId: 'variant-b' },
          { field: 'heightInMeters', sourceType: 'Variant', externalVariantId: 'variant-a' }
        ]
      }
    ]);
  });

  it('reads a field value from an external variant', () => {
    const variant = createDuplicateResult().externalVariants[1];

    expect(getExternalFieldValue(variant, 'heightInMeters')).toBe('30');
    expect(getExternalFieldValue(variant, 'missing')).toBeNull();
  });
});
