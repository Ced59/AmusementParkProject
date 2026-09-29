import { StandaloneAttraction } from '@app/models/standalone-attractions/standalone-attraction';
import { normalizeTranslationSegment } from '@shared/utils/display/display-label.helpers';
import { getAttractionStatusValueKey } from '@shared/utils/display/park-item-presentation.helpers';

export interface StandaloneAttractionDetailLabels {
  type: string;
  subtype: string;
  model: string;
  status: string;
  manufacturer: string;
  length: string;
  speed: string;
  duration: string;
}

export interface StandaloneAttractionDetailRow {
  label: string;
  value: string;
  translationKey: string | null;
}

const SUPPORTED_TYPE_SEGMENTS: ReadonlySet<string> = new Set<string>([
  'attraction',
  'rollerCoaster',
  'waterRide',
  'flatRide',
  'darkRide',
  'familyRide',
  'thrillRide',
  'transportRide',
  'walkThrough',
  'playground',
  'interactiveExperience',
  'cinema',
  'dropTower',
  'observationRide',
  'animalExhibit',
  'restaurant',
  'snack',
  'hotel',
  'show',
  'shop',
  'game',
  'meetAndGreet',
  'service',
  'toilets',
  'firstAid',
  'information',
  'locker',
  'parking',
  'transport',
  'station',
  'other'
]);

export function getStandaloneAttractionTypeTranslationKey(type: StandaloneAttraction['type']): string {
  const segment: string = normalizeTranslationSegment(type, 'other');
  return `standaloneAttractionTypes.${SUPPORTED_TYPE_SEGMENTS.has(segment) ? segment : 'other'}`;
}

export function getStandaloneAttractionStatusTranslationKey(status: string | null | undefined): string | null {
  const normalizedStatus: string = status?.trim() ?? '';
  if (normalizedStatus.length === 0) {
    return null;
  }

  return getAttractionStatusValueKey(normalizedStatus) ?? 'parkItems.statuses.unknown';
}

export function getStandaloneAttractionSubtypeLabel(subtype: string | null | undefined): string {
  return subtype?.trim() ?? '';
}

export function buildStandaloneAttractionDetailRows(
  attraction: StandaloneAttraction,
  labels: StandaloneAttractionDetailLabels,
  manufacturerName: string | null,
  formatMetric: (value: number | null | undefined, suffix: string) => string
): StandaloneAttractionDetailRow[] {
  const statusTranslationKey: string | null = getStandaloneAttractionStatusTranslationKey(attraction.attractionDetails?.status);

  return [
    { label: labels.type, value: '', translationKey: getStandaloneAttractionTypeTranslationKey(attraction.type) },
    { label: labels.subtype, value: getStandaloneAttractionSubtypeLabel(attraction.subtype), translationKey: null },
    { label: labels.model, value: attraction.attractionDetails?.model?.trim() ?? '', translationKey: null },
    { label: labels.status, value: '', translationKey: statusTranslationKey },
    { label: labels.manufacturer, value: manufacturerName?.trim() ?? '', translationKey: null },
    { label: labels.length, value: formatMetric(attraction.attractionDetails?.lengthInMeters, 'm'), translationKey: null },
    { label: labels.speed, value: formatMetric(attraction.attractionDetails?.speedInKmH, 'km/h'), translationKey: null },
    { label: labels.duration, value: formatMetric(attraction.attractionDetails?.durationInSeconds, 's'), translationKey: null }
  ].filter((row: StandaloneAttractionDetailRow) => row.value.length > 0 || row.translationKey !== null);
}
