import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';

export function getAttractionStatusValueKey(status: string | null | undefined): string | null {
  const normalized: string = status?.trim() ?? '';
  if (normalized.length === 0) {
    return null;
  }

  const normalizedKey: string = normalized.toLowerCase().replace(/[\s_-]+/g, '');
  const translationSegments: Record<string, string> = {
    operating: 'operating',
    open: 'operating',
    opened: 'operating',
    enfonctionnement: 'operating',
    underconstruction: 'underConstruction',
    construction: 'underConstruction',
    temporarilyclosed: 'temporarilyClosed',
    temporaryclosed: 'temporarilyClosed',
    closedtemporarily: 'temporarilyClosed',
    closeddefinitively: 'closedDefinitively',
    permanentlyclosed: 'closedDefinitively',
    definitivelyclosed: 'closedDefinitively',
    fermedefinitivement: 'closedDefinitively',
    removed: 'removed',
    dismantled: 'removed',
    planned: 'planned',
    announced: 'planned',
    unknown: 'unknown'
  };
  const segment: string | undefined = translationSegments[normalizedKey];

  return segment ? `parkItems.statuses.${segment}` : null;
}

export function getAccessConditionTypeLabelKey(type: AttractionAccessConditionType): string {
  switch (type) {
    case 'MinHeight':
      return 'parkItems.accessConditionTypes.minHeight';
    case 'MinHeightAccompanied':
      return 'parkItems.accessConditionTypes.minHeightAccompanied';
    case 'MaxHeight':
      return 'parkItems.accessConditionTypes.maxHeight';
    case 'MinAge':
      return 'parkItems.accessConditionTypes.minAge';
    case 'MinAgeAccompanied':
      return 'parkItems.accessConditionTypes.minAgeAccompanied';
    case 'PregnancyRestriction':
      return 'parkItems.accessConditionTypes.pregnancyRestriction';
    case 'HeartRestriction':
      return 'parkItems.accessConditionTypes.heartRestriction';
    case 'BackNeckRestriction':
      return 'parkItems.accessConditionTypes.backNeckRestriction';
    case 'WheelchairTransferRequired':
      return 'parkItems.accessConditionTypes.wheelchairTransferRequired';
    case 'AccessPassRequired':
      return 'parkItems.accessConditionTypes.accessPassRequired';
    case 'Custom':
    default:
      return 'parkItems.accessConditionTypes.custom';
  }
}

export function resolveParkItemTypeIconClass(type: string | null | undefined): string {
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
    case 'MeetAndGreet':
      return 'pi pi-heart';
    case 'Service':
      return 'pi pi-wrench';
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
    case 'Transport':
    case 'TransportRide':
      return 'pi pi-car';
    case 'Station':
      return 'pi pi-directions';
    default:
      return 'pi pi-star';
  }
}

export function resolveParkItemTypeTone(type: string | null | undefined, category: string | null | undefined): string {
  switch (type) {
    case 'RollerCoaster':
      return 'coaster';
    case 'WaterRide':
      return 'water';
    case 'FamilyRide':
    case 'Playground':
      return 'family';
    case 'Show':
    case 'Cinema':
      return 'show';
    case 'Restaurant':
    case 'Snack':
      return 'food';
    case 'ThrillRide':
    case 'DropTower':
    case 'Game':
      return 'thrill';
    case 'MeetAndGreet':
      return 'rose';
    case 'Parking':
    case 'Toilets':
    case 'Locker':
    case 'FirstAid':
    case 'Information':
    case 'Service':
      return 'sky';
    case 'Station':
    case 'Transport':
    case 'TransportRide':
      return 'family';
    default:
      return resolveParkItemCategoryTone(category);
  }
}

function resolveParkItemCategoryTone(category: string | null | undefined): string {
  switch (category) {
    case 'Restaurant':
      return 'food';
    case 'Hotel':
    case 'Service':
      return 'sky';
    case 'Show':
      return 'show';
    case 'Shop':
      return 'gold';
    case 'Transport':
      return 'family';
    default:
      return 'coaster';
  }
}
