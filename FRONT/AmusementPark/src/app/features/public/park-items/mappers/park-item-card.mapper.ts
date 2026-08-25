import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  getAttractionStatusValueKey,
  resolveParkItemDescription
} from '@shared/utils/display/park-item-presentation.helpers';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { NaturalTextTruncatorService } from '@shared/services/text/natural-text-truncator.service';
import { MeasurementSystem, DEFAULT_MEASUREMENT_SYSTEM } from '@shared/models/measurements/measurement-system.model';
import { MeasurementConversionService } from '@shared/services/measurements/measurement-conversion.service';
import { ParkItemCardLifecycleStatusViewModel, ParkItemCardViewModel } from '../models/park-item-card.model';

const PARK_ITEM_CARD_DESCRIPTION_MAX_LENGTH = 160;
const defaultMeasurementConversionService = new MeasurementConversionService();

export function mapParkItemToCardViewModel(
  item: ParkItem,
  park: Park | null,
  currentLanguage: string,
  manufacturerName: string | null,
  zoneName: string | null,
  textTruncator: NaturalTextTruncatorService | null = null,
  measurementSystem: MeasurementSystem = DEFAULT_MEASUREMENT_SYSTEM,
  measurementConversionService: MeasurementConversionService = defaultMeasurementConversionService,
  imageUrl: string | null = null,
  imageSrcSet: string | null = null
): ParkItemCardViewModel {
  const modelName: string | null = item.attractionDetails?.model?.trim() ?? null;
  const subtitleParts: string[] = [manufacturerName, modelName]
    .filter((value: string | null): value is string => !!value);

  return {
    id: item.id ?? null,
    name: item.name?.trim() ?? '',
    subtitle: subtitleParts.length > 0 ? subtitleParts.join(' · ') : null,
    description: buildCardDescription(item, currentLanguage, textTruncator),
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    typeIconClass: resolveParkItemTypeIconClass(item.type),
    zoneName,
    imageUrl,
    imageSrcSet,
    lifecycleStatus: buildLifecycleStatus(item.attractionDetails?.status),
    highlights: buildParkItemHighlights(item, manufacturerName, currentLanguage, measurementSystem, measurementConversionService),
    itemLink: buildParkItemLink(park, item, currentLanguage)
  };
}

function buildCardDescription(
  item: ParkItem,
  currentLanguage: string,
  textTruncator: NaturalTextTruncatorService | null
): string | null {
  const description: string | null = resolveParkItemDescription(item, currentLanguage);

  if (!textTruncator) {
    return description;
  }

  return textTruncator.truncate(description, { maxLength: PARK_ITEM_CARD_DESCRIPTION_MAX_LENGTH, ellipsis: '...' });
}

function buildParkItemHighlights(
  item: ParkItem,
  manufacturerName: string | null,
  currentLanguage: string,
  measurementSystem: MeasurementSystem,
  measurementConversionService: MeasurementConversionService
): string[] {
  const values: string[] = [];

  if (manufacturerName) {
    values.push(manufacturerName);
  }

  if (item.attractionDetails?.model) {
    values.push(item.attractionDetails.model);
  }

  const heightLine: string | null = measurementConversionService.formatLengthFromMeters(
    item.attractionDetails?.heightInMeters,
    measurementSystem,
    currentLanguage
  );
  if (heightLine) {
    values.push(heightLine);
  }

  const speedLine: string | null = measurementConversionService.formatSpeedFromKilometersPerHour(
    item.attractionDetails?.speedInKmH,
    measurementSystem,
    currentLanguage
  );
  if (speedLine) {
    values.push(speedLine);
  }

  if (item.attractionDetails?.inversionCount != null) {
    values.push(`${item.attractionDetails.inversionCount} inv.`);
  }

  return values.slice(0, 4);
}


function buildLifecycleStatus(status: string | null | undefined): ParkItemCardLifecycleStatusViewModel | null {
  const normalized: string = status?.trim() ?? '';
  if (normalized.length === 0) {
    return null;
  }

  const labelKey: string | null = getAttractionStatusValueKey(normalized);
  if (labelKey === 'parkItems.statuses.operating') {
    return null;
  }

  if (labelKey === 'parkItems.statuses.temporarilyClosed') {
    return { labelKey, label: null, tone: 'gold', iconClass: 'pi pi-pause-circle' };
  }

  if (labelKey === 'parkItems.statuses.closedDefinitively' || labelKey === 'parkItems.statuses.removed') {
    return { labelKey, label: null, tone: 'rose', iconClass: 'pi pi-ban' };
  }

  if (labelKey === 'parkItems.statuses.underConstruction' || labelKey === 'parkItems.statuses.planned') {
    return { labelKey, label: null, tone: 'sky', iconClass: 'pi pi-clock' };
  }

  return {
    labelKey,
    label: labelKey ? null : normalized,
    tone: 'soft',
    iconClass: 'pi pi-info-circle'
  };
}

function buildParkItemLink(park: Park | null, item: ParkItem, currentLanguage: string): string[] | null {
  return buildPublicParkItemRouteCommands({
    language: currentLanguage,
    parkId: park?.id,
    parkName: park?.name,
    itemId: item.id,
    itemName: item.name
  });
}

function resolveParkItemTypeIconClass(type: string | null | undefined): string {
  switch (type) {
    case 'RollerCoaster':
      return 'pi pi-bolt';
    case 'WaterRide':
      return 'pi pi-compass';
    case 'FlatRide':
      return 'pi pi-sync';
    case 'DarkRide':
      return 'pi pi-moon';
    case 'FamilyRide':
    case 'MeetAndGreet':
      return 'pi pi-heart';
    case 'ThrillRide':
    case 'DropTower':
      return 'pi pi-send';
    case 'Restaurant':
    case 'Snack':
      return 'pi pi-shopping-bag';
    case 'Show':
    case 'Cinema':
      return 'pi pi-video';
    case 'Hotel':
      return 'pi pi-home';
    case 'Shop':
      return 'pi pi-shopping-cart';
    case 'Game':
    case 'InteractiveExperience':
      return 'pi pi-bullseye';
    case 'Transport':
    case 'TransportRide':
      return 'pi pi-car';
    case 'Station':
      return 'pi pi-directions';
    case 'Toilets':
      return 'pi pi-users';
    case 'FirstAid':
      return 'pi pi-plus-circle';
    case 'Information':
      return 'pi pi-info-circle';
    case 'Locker':
      return 'pi pi-lock';
    case 'Parking':
      return 'pi pi-car';
    case 'Service':
      return 'pi pi-wrench';
    default:
      return 'pi pi-star';
  }
}
