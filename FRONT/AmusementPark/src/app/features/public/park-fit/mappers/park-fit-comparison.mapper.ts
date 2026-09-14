import { ParkFitSearchMemberSummary, ParkFitSearchPark, ParkFitScoreComponent } from '@app/models/park-fit/park-fit-search.models';
import {
  ParkFitComparisonCell,
  ParkFitComparisonRow,
  ParkFitComparisonSection
} from '../models/park-fit-comparison.models';
import {
  parkFitAvailabilityKey,
  parkFitComponentStateKey,
  parkFitConfidenceKey,
  parkFitQualityKey,
  parkFitScoreReasonKey,
  parkFitScoreStateKey,
  resolveParkFitSourceUrl
} from './park-fit-result-display.helpers';

type CellBuilder = (park: ParkFitSearchPark) => ParkFitComparisonCell;

export function buildParkFitComparisonSections(
  parks: ParkFitSearchPark[],
  formatDate: (value: string | null) => string
): ParkFitComparisonSection[] {
  const memberNumbers: number[] = Array.from(new Set(
    parks.flatMap((park: ParkFitSearchPark): number[] =>
      park.memberSummaries.map((member: ParkFitSearchMemberSummary): number => member.memberNumber)
    )
  )).sort((left: number, right: number): number => left - right);

  return [
    {
      id: 'decision',
      titleKey: 'parkFit.comparison.sections.decision',
      rows: [
        buildRow('overall', 'parkFit.comparison.rows.overall', 'pi pi-compass', parks, buildOverallCell),
        buildRow(
          'group-compatibility',
          'parkFit.comparison.rows.groupCompatibility',
          'pi pi-sitemap',
          parks,
          (park: ParkFitSearchPark): ParkFitComparisonCell => buildComponentCell(park, 'GroupCompatibility')
        ),
        buildRow('together', 'parkFit.comparison.rows.together', 'pi pi-users', parks, buildTogetherCell),
        buildRow('unknowns', 'parkFit.comparison.rows.unknowns', 'pi pi-question-circle', parks, buildUnknownCell),
        buildRow('opening', 'parkFit.comparison.rows.opening', 'pi pi-calendar', parks, buildOpeningCell)
      ]
    },
    {
      id: 'people',
      titleKey: 'parkFit.comparison.sections.people',
      rows: memberNumbers.map((memberNumber: number): ParkFitComparisonRow => buildRow(
        `member-${memberNumber}`,
        'parkFit.comparison.rows.member',
        'pi pi-user',
        parks,
        (park: ParkFitSearchPark): ParkFitComparisonCell => buildMemberCell(park, memberNumber),
        { number: memberNumber }
      ))
    },
    {
      id: 'preferences',
      titleKey: 'parkFit.comparison.sections.preferences',
      rows: [
        buildRow('preferences', 'parkFit.comparison.rows.preferences', 'pi pi-heart', parks, (park: ParkFitSearchPark): ParkFitComparisonCell => buildComponentCell(park, 'PreferenceCoverage')),
        buildRow('indoor', 'parkFit.comparison.rows.indoor', 'pi pi-home', parks, (park: ParkFitSearchPark): ParkFitComparisonCell => buildComponentCell(park, 'IndoorResilience'))
      ]
    },
    {
      id: 'practical',
      titleKey: 'parkFit.comparison.sections.practical',
      rows: [
        buildRow('travel', 'parkFit.comparison.rows.travel', 'pi pi-car', parks, (park: ParkFitSearchPark): ParkFitComparisonCell => buildComponentCell(park, 'TravelConvenience')),
        buildRow('schedule', 'parkFit.comparison.rows.schedule', 'pi pi-clock', parks, buildScheduleCell),
        buildRow('budget', 'parkFit.comparison.rows.budget', 'pi pi-wallet', parks, (park: ParkFitSearchPark): ParkFitComparisonCell => buildComponentCell(park, 'BudgetFit'))
      ]
    },
    {
      id: 'trust',
      titleKey: 'parkFit.comparison.sections.trust',
      rows: [
        buildRow('quality', 'parkFit.comparison.rows.quality', 'pi pi-shield', parks, buildQualityCell),
        buildRow('verified', 'parkFit.comparison.rows.verified', 'pi pi-verified', parks, (park: ParkFitSearchPark): ParkFitComparisonCell => buildVerifiedCell(park, formatDate)),
        buildRow('official', 'parkFit.comparison.rows.official', 'pi pi-external-link', parks, buildOfficialSourceCell)
      ]
    }
  ];
}

function buildRow(
  id: string,
  labelKey: string,
  iconClass: string,
  parks: ParkFitSearchPark[],
  buildCell: CellBuilder,
  labelParams: Record<string, string | number> = {}
): ParkFitComparisonRow {
  const cells: ParkFitComparisonCell[] = parks.map(buildCell);
  return {
    id,
    labelKey,
    labelParams,
    iconClass,
    cells,
    isDifferent: new Set(cells.map((cell: ParkFitComparisonCell): string => cell.fingerprint)).size > 1
  };
}

function buildOverallCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  const score: number | null = park.comparativeScore === null ? null : Math.round(park.comparativeScore);
  const scoreCeiling: number | null = park.scoreCeilingPercent === null
    ? null
    : Math.round(park.scoreCeilingPercent);
  const renderedScoreCeiling: number | null = park.scoreState === 'Capped' ? scoreCeiling : null;
  const statusKey: string = renderedScoreCeiling !== null
    ? 'parkFit.comparison.values.cappedState'
    : parkFitScoreStateKey(park.scoreState);
  const reasonKeys: string[] = park.reasons.map(parkFitScoreReasonKey);
  return cell(
    park,
    score === null ? 'parkFit.comparison.values.scoreUnavailable' : 'parkFit.comparison.values.score',
    score === null
      ? { coverage: Math.round(park.coveragePercent) }
      : { score, coverage: Math.round(park.coveragePercent) },
    parkFitConfidenceKey(park.confidence),
    {},
    null,
    `${score ?? 'unknown'}|${Math.round(park.coveragePercent)}|${park.confidence}|${park.scoreState}|${renderedScoreCeiling ?? 'none'}|${reasonKeys.join(',')}`,
    statusKey,
    renderedScoreCeiling === null ? {} : { ceiling: renderedScoreCeiling },
    reasonKeys
  );
}

function buildTogetherCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  const organizedCount: number = park.splitRequiredAttractionCount + park.partialAttractionCount;
  return cell(
    park,
    'parkFit.comparison.values.together',
    { count: park.everyoneTogetherAttractionCount },
    'parkFit.comparison.values.organized',
    { count: organizedCount, unavailable: park.noCompatibleMemberAttractionCount },
    null,
    `${park.everyoneTogetherAttractionCount}|${organizedCount}|${park.noCompatibleMemberAttractionCount}`
  );
}

function buildUnknownCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  return cell(
    park,
    'parkFit.comparison.values.unknowns',
    { attractions: park.unknownAttractionCount, factors: park.unknownCount },
    null,
    {},
    null,
    `${park.unknownAttractionCount}|${park.unknownCount}`
  );
}

function buildOpeningCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  return cell(park, parkFitAvailabilityKey(park.dateAvailabilityState), {}, null, {}, null, park.dateAvailabilityState);
}

function buildMemberCell(park: ParkFitSearchPark, memberNumber: number): ParkFitComparisonCell {
  const member: ParkFitSearchMemberSummary | undefined = park.memberSummaries.find(
    (candidate: ParkFitSearchMemberSummary): boolean => candidate.memberNumber === memberNumber
  );
  if (!member) {
    return cell(park, 'parkFit.comparison.values.unavailable', {}, null, {}, null, 'unavailable');
  }

  return cell(
    park,
    'parkFit.comparison.values.member',
    {
      alone: member.compatibleAloneAttractionCount,
      accompanied: member.compatibleWithCompanionAttractionCount,
      unavailable: member.incompatibleAttractionCount,
      unknown: member.unknownAttractionCount,
      notApplicable: member.notApplicableAttractionCount
    },
    null,
    {},
    null,
    `${member.compatibleAloneAttractionCount}|${member.compatibleWithCompanionAttractionCount}|${member.incompatibleAttractionCount}|${member.unknownAttractionCount}|${member.notApplicableAttractionCount}`
  );
}

function buildComponentCell(park: ParkFitSearchPark, componentKind: string): ParkFitComparisonCell {
  const component: ParkFitScoreComponent | undefined = park.components.find(
    (candidate: ParkFitScoreComponent): boolean => candidate.kind === componentKind
  );
  if (!component) {
    return cell(park, 'parkFit.comparison.values.unavailable', {}, null, {}, null, 'unavailable');
  }

  const value: number | null = component.value === null ? null : Math.round(component.value);
  const coverage: number = Math.round(component.coveragePercent);
  return cell(
    park,
    value === null ? parkFitComponentStateKey(component.state) : 'parkFit.comparison.values.componentScore',
    value === null ? {} : { score: value },
    parkFitConfidenceKey(component.confidence),
    {},
    null,
    `${component.state}|${value ?? 'unknown'}|${coverage}|${component.confidence}`,
    'parkFit.comparison.values.componentCoverage',
    { coverage }
  );
}

function buildScheduleCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  return cell(
    park,
    'parkFit.comparison.values.scheduleNotExposed',
    {},
    parkFitAvailabilityKey(park.dateAvailabilityState),
    {},
    null,
    `not-exposed|${park.dateAvailabilityState}`
  );
}

function buildQualityCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  return cell(
    park,
    parkFitQualityKey(park.dataQualityStatus),
    {},
    'parkFit.comparison.values.qualityCoverage',
    { coverage: Math.round(park.dataQualityCoveragePercent) },
    null,
    `${park.dataQualityStatus}|${Math.round(park.dataQualityCoveragePercent)}`
  );
}

function buildVerifiedCell(park: ParkFitSearchPark, formatDate: (value: string | null) => string): ParkFitComparisonCell {
  const formattedDate: string = formatDate(park.lastVerifiedAtUtc);
  return cell(
    park,
    'parkFit.comparison.values.verified',
    { date: formattedDate },
    null,
    {},
    null,
    formattedDate
  );
}

function buildOfficialSourceCell(park: ParkFitSearchPark): ParkFitComparisonCell {
  const linkUrl: string | null = park.criticalSources
    .filter((source): boolean => source.kind === 'Official' || source.kind === 'OperatorProvided')
    .map(resolveParkFitSourceUrl)
    .find((url: string | null): url is string => url !== null) ?? null;
  return cell(
    park,
    linkUrl ? 'parkFit.comparison.values.officialAvailable' : 'parkFit.comparison.values.officialUnavailable',
    {},
    null,
    {},
    linkUrl,
    linkUrl ? 'available' : 'unavailable'
  );
}

function cell(
  park: ParkFitSearchPark,
  primaryKey: string,
  primaryParams: Record<string, string | number>,
  secondaryKey: string | null,
  secondaryParams: Record<string, string | number>,
  linkUrl: string | null,
  fingerprint: string,
  statusKey: string | null = null,
  statusParams: Record<string, string | number> = {},
  reasonKeys: string[] = []
): ParkFitComparisonCell {
  return {
    parkId: park.parkId,
    primaryKey,
    primaryParams,
    secondaryKey,
    secondaryParams,
    statusKey,
    statusParams,
    reasonKeys,
    linkUrl,
    fingerprint
  };
}
