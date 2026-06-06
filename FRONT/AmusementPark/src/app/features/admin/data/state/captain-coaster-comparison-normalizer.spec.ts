import { normalizeCaptainCoasterComparisonPage } from './captain-coaster-comparison-normalizer';

describe('normalizeCaptainCoasterComparisonPage', () => {
  it('normalizes PascalCase API payloads and nested variants', () => {
    const normalized = normalizeCaptainCoasterComparisonPage({
      Items: [
        {
          Id: 'comparison-1',
          EntityType: 'Coaster',
          ChangeType: 'Updated',
          DisplayName: 'Taron',
          LocalEntityId: 'local-1',
          ExternalEntityId: 'external-1',
          MatchConfidence: 'High',
          IsApplied: false,
          HasExternalDuplicates: true,
          RequiresManualResolution: true,
          ResolutionStatus: 'Pending',
          AppliedExternalVariantId: null,
          Changes: [
            { Field: 'name', LocalValue: 'Old', ExternalValue: 'New', IsDifferent: true }
          ],
          ExternalVariants: [
            {
              ExternalVariantId: 'variant-1',
              DisplayLabel: 'Suggested variant',
              CandidateLocalEntityId: 'candidate-1',
              SourceUrl: 'https://example.test',
              IsSuggested: true,
              Changes: [
                { Field: 'speedInKmH', LocalValue: '100', ExternalValue: '117', IsDifferent: true }
              ]
            }
          ]
        }
      ],
      TotalCount: 1,
      Page: 2,
      PageSize: 25,
      SessionUpdatedCount: 3,
      SessionMissingCount: 4,
      SessionDuplicateCount: 5,
      SessionAppliedCount: 6
    }, 0, 50);

    expect(normalized.totalCount).toBe(1);
    expect(normalized.page).toBe(2);
    expect(normalized.pageSize).toBe(25);
    expect(normalized.sessionUpdatedCount).toBe(3);
    expect(normalized.sessionMissingCount).toBe(4);
    expect(normalized.sessionDuplicateCount).toBe(5);
    expect(normalized.sessionAppliedCount).toBe(6);
    expect(normalized.items[0].id).toBe('comparison-1');
    expect(normalized.items[0].changes[0].field).toBe('name');
    expect(normalized.items[0].externalVariants[0].externalVariantId).toBe('variant-1');
    expect(normalized.items[0].externalVariants[0].changes[0].externalValue).toBe('117');
  });

  it('uses safe defaults when optional API fields are absent', () => {
    const normalized = normalizeCaptainCoasterComparisonPage({}, 3, 100);

    expect(normalized).toEqual({
      items: [],
      totalCount: 0,
      page: 3,
      pageSize: 100,
      sessionUpdatedCount: 0,
      sessionMissingCount: 0,
      sessionDuplicateCount: 0,
      sessionAppliedCount: 0
    });
  });
});
