import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { ParkItem } from '@app/models/parks/park-item';
import { MeasurementSystem } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import {
  ParkItemAccessConditionMetricViewModel,
  ParkItemAccessConditionViewModel,
  ParkItemDetailRowViewModel
} from '../models/park-item-detail-view.model';
import {
  formatAccessConditionValue,
  formatAge,
  formatBoolean,
  resolveOptionalLocalizedText
} from './park-item-detail-formatters';
import { getAccessConditionTypeLabelKey } from './park-item-detail-presentation.mapper';
import { pushRow } from './park-item-detail-row.helpers';

export function buildAccessConditions(
  item: ParkItem,
  currentLanguage: string,
  measurementSystem: MeasurementSystem,
  measurementConversionService: MeasurementConversionService
): ParkItemAccessConditionViewModel[] {
  const conditions: AttractionAccessCondition[] = [...(item.attractionDetails?.accessConditions ?? [])]
    .sort((first: AttractionAccessCondition, second: AttractionAccessCondition) => (first.displayOrder ?? 0) - (second.displayOrder ?? 0));
  const viewModels: ParkItemAccessConditionViewModel[] = [];
  const heightCondition: ParkItemAccessConditionViewModel | null = buildHeightAccessCondition(
    conditions,
    currentLanguage,
    measurementSystem,
    measurementConversionService
  );

  if (heightCondition) {
    viewModels.push(heightCondition);
  }

  viewModels.push(
    ...conditions
      .filter((condition: AttractionAccessCondition) => !isHeightAccessCondition(condition.type))
      .map((condition: AttractionAccessCondition) => mapAccessCondition(condition, currentLanguage, measurementSystem, measurementConversionService))
  );

  return viewModels;
}

function buildHeightAccessCondition(
  conditions: AttractionAccessCondition[],
  currentLanguage: string,
  measurementSystem: MeasurementSystem,
  measurementConversionService: MeasurementConversionService
): ParkItemAccessConditionViewModel | null {
  const metrics: ParkItemAccessConditionMetricViewModel[] = [];

  pushHeightMetric(
    metrics,
    conditions,
    'MinHeight',
    'parkItems.accessConditions.height.minHeight',
    'parkItems.accessConditions.height.minHeightHelp',
    'pi pi-user',
    currentLanguage,
    measurementSystem,
    measurementConversionService
  );
  pushHeightMetric(
    metrics,
    conditions,
    'MinHeightAccompanied',
    'parkItems.accessConditions.height.minHeightAccompanied',
    'parkItems.accessConditions.height.minHeightAccompaniedHelp',
    'pi pi-users',
    currentLanguage,
    measurementSystem,
    measurementConversionService
  );
  pushHeightMetric(
    metrics,
    conditions,
    'MaxHeight',
    'parkItems.accessConditions.height.maxHeight',
    'parkItems.accessConditions.height.maxHeightHelp',
    'pi pi-ban',
    currentLanguage,
    measurementSystem,
    measurementConversionService
  );

  if (metrics.length === 0) {
    return null;
  }

  return {
    title: null,
    titleKey: 'parkItems.accessConditions.height.title',
    description: null,
    rows: [],
    metrics,
    kind: 'height',
    iconClass: 'pi pi-arrows-v',
    tone: 'height'
  };
}

function pushHeightMetric(
  metrics: ParkItemAccessConditionMetricViewModel[],
  conditions: AttractionAccessCondition[],
  type: AttractionAccessConditionType,
  labelKey: string,
  helperKey: string,
  iconClass: string,
  currentLanguage: string,
  measurementSystem: MeasurementSystem,
  measurementConversionService: MeasurementConversionService
): void {
  const condition: AttractionAccessCondition | undefined = conditions.find((candidate: AttractionAccessCondition) => candidate.type === type);

  if (!condition || condition.value == null) {
    return;
  }

  metrics.push({
    labelKey,
    value: formatAccessConditionValue(condition.value, condition.unit ?? 'Centimeter', currentLanguage, measurementSystem, measurementConversionService),
    helperKey,
    iconClass
  });
}

function mapAccessCondition(
  condition: AttractionAccessCondition,
  currentLanguage: string,
  measurementSystem: MeasurementSystem,
  measurementConversionService: MeasurementConversionService
): ParkItemAccessConditionViewModel {
  const rows: ParkItemDetailRowViewModel[] = [];
  const title: string | null = resolveOptionalLocalizedText(condition.label, currentLanguage)
    ?? resolveOptionalLocalizedText(condition.customTypeLabel, currentLanguage);
  const description: string | null = resolveOptionalLocalizedText(condition.description, currentLanguage);

  if (condition.value != null) {
    pushRow(rows, 'parkItems.accessConditionFields.value', formatAccessConditionValue(condition.value, condition.unit, currentLanguage, measurementSystem, measurementConversionService), null, 'pi pi-sliders-h');
  }

  if (condition.requiresAccompaniment === true) {
    pushRow(rows, 'parkItems.accessConditionFields.requiresAccompaniment', formatBoolean(condition.requiresAccompaniment, currentLanguage), null, 'pi pi-users');
  }

  if (condition.minimumCompanionAge != null) {
    pushRow(rows, 'parkItems.accessConditionFields.minimumCompanionAge', formatAge(condition.minimumCompanionAge, currentLanguage), null, 'pi pi-user-plus');
  }

  return {
    title,
    titleKey: getAccessConditionTypeLabelKey(condition.type),
    description,
    rows,
    metrics: [],
    kind: getAccessConditionKind(condition.type),
    iconClass: getAccessConditionIconClass(condition.type),
    tone: getAccessConditionTone(condition.type)
  };
}

function isHeightAccessCondition(type: AttractionAccessConditionType): boolean {
  return type === 'MinHeight' || type === 'MinHeightAccompanied' || type === 'MaxHeight';
}

function getAccessConditionKind(type: AttractionAccessConditionType): 'restriction' | 'default' {
  switch (type) {
    case 'PregnancyRestriction':
    case 'HeartRestriction':
    case 'BackNeckRestriction':
    case 'WheelchairTransferRequired':
    case 'AccessPassRequired':
      return 'restriction';
    default:
      return 'default';
  }
}

function getAccessConditionIconClass(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'MinAge':
    case 'MinAgeAccompanied':
      return 'pi pi-calendar';
    case 'PregnancyRestriction':
      return 'pi pi-exclamation-triangle';
    case 'HeartRestriction':
      return 'pi pi-heart';
    case 'BackNeckRestriction':
      return 'pi pi-shield';
    case 'WheelchairTransferRequired':
      return 'pi pi-directions';
    case 'AccessPassRequired':
      return 'pi pi-ticket';
    case 'Custom':
      return 'pi pi-info-circle';
    default:
      return 'pi pi-lock';
  }
}

function getAccessConditionTone(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'PregnancyRestriction':
    case 'HeartRestriction':
    case 'BackNeckRestriction':
      return 'restriction';
    case 'WheelchairTransferRequired':
    case 'AccessPassRequired':
      return 'sky';
    case 'MinAge':
    case 'MinAgeAccompanied':
      return 'gold';
    default:
      return 'default';
  }
}
