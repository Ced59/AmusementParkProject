import { ParkFitSearchPark } from '@app/models/park-fit/park-fit-search.models';
import { buildParkFitComparisonSections } from './park-fit-comparison.mapper';

describe('buildParkFitComparisonSections', () => {
  it('marks differing business rows without relying on colour', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 72, 8, 3);

    const sections = buildParkFitComparisonSections([first, second], (): string => '14/09/2026');
    const rows = sections.flatMap((section) => section.rows);

    expect(rows.find((row) => row.id === 'overall')?.isDifferent).toBe(true);
    expect(rows.find((row) => row.id === 'group-compatibility')).toBeDefined();
    expect(rows.find((row) => row.id === 'member-1')?.isDifferent).toBe(true);
    expect(rows.find((row) => row.id === 'member-1')?.cells[0]?.primaryParams['notApplicable']).toBe(0);
    expect(rows.find((row) => row.id === 'together')?.cells[0]?.secondaryParams)
      .toEqual({ split: 1, partial: 0, unavailable: 1 });
    expect(rows.find((row) => row.id === 'schedule')?.cells).toHaveLength(2);
  });

  it('maps global group compatibility and score reasons explicitly', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    first.reasons = ['IncompleteCoverageCapApplied'];
    first.components.unshift({
      kind: 'GroupCompatibility',
      state: 'Known',
      value: 88,
      coveragePercent: 100,
      confidence: 'High',
      baseWeightPercent: 60,
      applicableWeightPercent: 60,
      knownScoreWeightPercent: 60,
      contribution: 52.8,
      reasons: []
    });

    const rows = buildParkFitComparisonSections([first, second], (): string => 'date')
      .flatMap((section) => section.rows);

    expect(rows.find((row) => row.id === 'group-compatibility')?.cells[0]?.primaryParams['score']).toBe(88);
    expect(rows.find((row) => row.id === 'overall')?.cells[0]?.reasonKeys)
      .toEqual(['parkFit.results.scoreReasons.IncompleteCoverageCapApplied']);
    expect(rows.find((row) => row.id === 'overall')?.isDifferent).toBe(true);
  });

  it('keeps component reliability visible and comparable', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    second.components[0]!.coveragePercent = 65;
    second.components[0]!.confidence = 'Low';

    const preferenceRow = buildParkFitComparisonSections([first, second], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'preferences');

    expect(preferenceRow?.isDifferent).toBe(true);
    expect(preferenceRow?.cells[0]?.statusParams['coverage']).toBe(100);
    expect(preferenceRow?.cells[0]?.secondaryKey).toBe('parkFit.results.confidenceLevels.High');
    expect(preferenceRow?.cells[1]?.statusParams['coverage']).toBe(65);
    expect(preferenceRow?.cells[1]?.secondaryKey).toBe('parkFit.results.confidenceLevels.Low');
  });

  it('keeps component business reasons visible and comparable', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    first.components[0]!.reasons = ['KnownFactsNormalized'];
    second.components[0]!.reasons = ['MinimumMemberBoundApplied'];

    const preferenceRow = buildParkFitComparisonSections([first, second], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'preferences');

    expect(preferenceRow?.isDifferent).toBe(true);
    expect(preferenceRow?.cells[0]?.reasonKeys)
      .toEqual(['parkFit.results.subscoreReasons.KnownFactsNormalized']);
    expect(preferenceRow?.cells[1]?.reasonKeys)
      .toEqual(['parkFit.results.subscoreReasons.MinimumMemberBoundApplied']);
  });

  it('distinguishes splitting the whole group from partial participation', () => {
    const split: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const partial: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    split.splitRequiredAttractionCount = 5;
    split.partialAttractionCount = 0;
    partial.splitRequiredAttractionCount = 0;
    partial.partialAttractionCount = 5;

    const togetherRow = buildParkFitComparisonSections([split, partial], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'together');

    expect(togetherRow?.isDifferent).toBe(true);
    expect(togetherRow?.cells[0]?.secondaryParams).toEqual({ split: 5, partial: 0, unavailable: 1 });
    expect(togetherRow?.cells[1]?.secondaryParams).toEqual({ split: 0, partial: 5, unavailable: 1 });
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

  it('ignores an unapplied ceiling that is not visible on available scores', () => {
    const first: ParkFitSearchPark = buildPark('park-1', 82, 12, 1);
    const second: ParkFitSearchPark = buildPark('park-2', 82, 12, 1);
    first.scoreCeilingPercent = 90;
    second.scoreCeilingPercent = 95;

    const overallRow = buildParkFitComparisonSections([first, second], (): string => 'date')
      .flatMap((section) => section.rows)
      .find((row) => row.id === 'overall');

    expect(overallRow?.isDifferent).toBe(false);
    expect(overallRow?.cells.every((comparisonCell) => comparisonCell.statusParams['ceiling'] === undefined))
      .toBe(true);
  });

  it('only exposes an official HTTPS source URL with neutral availability wording', () => {
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
    expect(officialRow?.cells[1]?.primaryKey).toBe('parkFit.comparison.values.officialAvailable');
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
