import {
  getParkItemCategoryTranslationKey,
  getParkItemTypeTranslationKey,
  resolveParkItemDescription
} from '@shared/utils/display/park-item-presentation.helpers';
import { buildPublicParkItemRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCardViewModel } from '../models/park-item-card.model';

export function mapParkItemToCardViewModel(
  item: ParkItem,
  park: Park | null,
  currentLanguage: string,
  manufacturerName: string | null,
  zoneName: string | null
): ParkItemCardViewModel {
  const modelName: string | null = item.attractionDetails?.model?.trim() ?? null;
  const subtitleParts: string[] = [manufacturerName, modelName]
    .filter((value: string | null): value is string => !!value);

  return {
    id: item.id ?? null,
    name: item.name?.trim() ?? '',
    subtitle: subtitleParts.length > 0 ? subtitleParts.join(' · ') : null,
    description: resolveParkItemDescription(item, currentLanguage),
    categoryLabelKey: getParkItemCategoryTranslationKey(item.category),
    typeLabelKey: getParkItemTypeTranslationKey(item.type),
    typeIconClass: resolveParkItemTypeIconClass(item.type),
    zoneName,
    highlights: buildParkItemHighlights(item, manufacturerName),
    itemLink: buildParkItemLink(park, item, currentLanguage)
  };
}

function buildParkItemHighlights(item: ParkItem, manufacturerName: string | null): string[] {
  const values: string[] = [];

  if (manufacturerName) {
    values.push(manufacturerName);
  }

  if (item.attractionDetails?.model) {
    values.push(item.attractionDetails.model);
  }

  if (item.attractionDetails?.status) {
    values.push(item.attractionDetails.status);
  }

  if (item.attractionDetails?.heightInMeters != null) {
    values.push(`${item.attractionDetails.heightInMeters} m`);
  }

  if (item.attractionDetails?.speedInKmH != null) {
    values.push(`${item.attractionDetails.speedInKmH} km/h`);
  }

  if (item.attractionDetails?.inversionCount != null) {
    values.push(`${item.attractionDetails.inversionCount} inv.`);
  }

  return values.slice(0, 4);
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
    default:
      return 'pi pi-star';
  }
}
