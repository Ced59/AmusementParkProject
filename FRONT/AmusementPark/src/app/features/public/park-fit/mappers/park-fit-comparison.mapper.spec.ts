import { ParkFitSearchPark } from '@app/models/park-fit/park-fit-search.models';
import { buildParkFitComparisonSections } from './park-fit-comparison.mapper';

describe('buildParkFitComparisonSections', () => {
  it('marks differing business rows without relying on colour', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 72, 8, 3);

    const sections = buildParkFitComparisonSections([first, second], (): string => '14/09/2026');
    const rows = sections.flatMap((section) => section.rows);

    expect(rows.find((row) => row.id === 'overall')?.isDifferent).toBe(true);
    expect(rows.find((row) => row.id === 'member-1')?.isDifferent).toBe(true);
    expect(rows.find((row) => row.id === 'member-1')?.cells[0]?.primaryParams['notApplicable']).toBe(0);
    expect(rows.find((row) => row.id === 'schedule')?.cells).toHaveLength(2);
  });

  it('compares the verification date exactly as visitors see it', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    first.lastVerifiedAtUtc = '2026-09-14T01:00:00Z';
    second.lastVerifiedAtUtc = '2026-09-14T22:00:00Z';

    const verifiedRow = buildParkFitComparisonSections([first, second], (): string => '14/09/2026')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'verified');

    expect(verifiedRow?.isDifferent).toBe(false);
  });

  it('shows coverage when suspended scores differ on that visible value', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    first.comparativeScore = null;
    second.comparativeScore = null;
    first.coveragePercent = 60;
    second.coveragePercent = 80;

    const overallRow = buildParkFitComparisonSections([first, second], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'overall');

    expect(overallRow?.isDifferent).toBe(true);
    expect(overallRow?.cells.map((comparisonCell) => comparisonCell.primaryParams['coverage'])).toEqual([60, 80]);
  });

  it('keeps a capped score visibly distinct from the same available score', () => {
    const available: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const capped: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    capped.scoreState = 'Capped';
    capped.scoreCeilingPercent = 82;

    const overallRow = buildParkFitComparisonSections([available, capped], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'overall');

    expect(overallRow?.isDifferent).toBe(true);
    expect(overallRow?.cells[0]?.statusKey).toBe('parkFit.results.scoreStates.Available');
    expect(overallRow?.cells[1]?.statusKey).toBe('parkFit.comparison.values.cappedState');
    expect(overallRow?.cells[1]?.statusParams['ceiling']).toBe(82);
  });

  it('only exposes a verified HTTPS source URL in the official-link row', () => {
    const unsafe: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    unsafe.criticalSources = [{
      kind: 'Official',
      url: 'http://internal.test/rules',
      reference: 'technical-id',
      languageCode: 'fr',
      collectedAtUtc: null,
      verifiedAtUtc: null,
      confidence: 'High',
      summaries: []
    }];
    const safe: ParkFitSearchPark = buildPark('park-2', 80, 10, 2);
    safe.criticalSources = [{ ...unsafe.criticalSources[0]!, url: 'https://example.com/rules' }];

    const officialRow = buildParkFitComparisonSections([unsafe, safe], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'official');

    expect(officialRow?.cells[0]?.linkUrl).toBeNull();
    expect(officialRow?.cells[1]?.linkUrl).toBe('https://example.com/rules');
    expect(JSON.stringify(officialRow)).not.toContain('technical-id');
  });
});

function buildPark(id: string, score: number, together: number, incompatible: number): ParkFitSearchPark {
  return {
    parkId: id,
    parkName: id,
    countryCode: 'FR',
    parkType: 'ThemePark',
    scoreState: 'Available',
    comparativeScore: score,
    rawKnownScore: score,
    coveragePercent: 90,
    knownWeightPercent: 90,
    scoreCeilingPercent: null,
    confidence: 'High',
    dateAvailabilityState: 'Available',
    unknownCount: 0,
    everyoneTogetherAttractionCount: together,
    splitRequiredAttractionCount: 1,
    partialAttractionCount: 0,
    noCompatibleMemberAttractionCount: incompatible,
    unknownAttractionCount: 0,
    dataQualityStatus: 'EligibleForFitComparison',
    dataQualityCoveragePercent: 95,
    lastVerifiedAtUtc: '2026-09-14T00:00:00Z',
    reasons: [],
    components: [{
      kind: 'PreferenceCoverage',
      state: 'Known',
      value: score,
      coveragePercent: 100,
      confidence: 'High',
      baseWeightPercent: 20,
      applicableWeightPercent: 20,
      knownScoreWeightPercent: 20,
      contribution: 16,
      reasons: []
    }],
    memberSummaries: [{
      memberNumber: 1,
      compatibleAloneAttractionCount: together,
      compatibleWithCompanionAttractionCount: 0,
      incompatibleAttractionCount: incompatible,
      unknownAttractionCount: 0,
      notApplicableAttractionCount: 0
    }],
    criticalSources: []
  };
}
