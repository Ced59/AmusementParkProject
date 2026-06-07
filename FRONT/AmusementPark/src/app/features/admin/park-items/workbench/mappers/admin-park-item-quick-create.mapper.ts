import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import {
  ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS,
  AdminParkItemQuickCreateDraft,
  AdminParkItemWorkbenchCoordinates
} from '../models/admin-park-item-workbench.model';

export function createAdminParkItemQuickCreateDraft(
  parkId: string,
  overrides: Partial<AdminParkItemQuickCreateDraft> = {}
): AdminParkItemQuickCreateDraft {
  return {
    parkId,
    zoneId: null,
    name: '',
    category: ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.category,
    type: ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.type,
    manufacturerId: null,
    coordinates: null,
    isVisible: ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.isVisible,
    adminReviewStatus: ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.adminReviewStatus,
    ...overrides
  };
}

export function mapAdminParkItemQuickCreateDraftToParkItem(
  draft: AdminParkItemQuickCreateDraft,
  fallbackCoordinates: AdminParkItemWorkbenchCoordinates
): ParkItem {
  const category: ParkItemCategory = draft.category ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.category;
  const type: ParkItemType = normalizeTypeForCategory(draft.type, category);
  const coordinates: AdminParkItemWorkbenchCoordinates = draft.coordinates ?? fallbackCoordinates;
  const manufacturerId: string | null = normalizeOptionalText(draft.manufacturerId);

  return {
    parkId: draft.parkId,
    zoneId: normalizeOptionalText(draft.zoneId),
    name: draft.name.trim(),
    category,
    type,
    subtype: null,
    latitude: coordinates.latitude,
    longitude: coordinates.longitude,
    descriptions: [],
    attractionDetails: category === 'Attraction' && manufacturerId
      ? { manufacturerId }
      : null,
    attractionLocations: null,
    isVisible: draft.isVisible ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.isVisible,
    adminReviewStatus: draft.adminReviewStatus ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.adminReviewStatus
  };
}

function normalizeOptionalText(value: string | null | undefined): string | null {
  const normalized: string = String(value ?? '').trim();
  return normalized.length > 0 ? normalized : null;
}

function normalizeTypeForCategory(type: ParkItemType | null | undefined, category: ParkItemCategory): ParkItemType {
  const allowedTypes: ReadonlyArray<ParkItemType> = getAllowedTypes(category);

  return type && allowedTypes.includes(type)
    ? type
    : allowedTypes[0];
}

function getAllowedTypes(category: ParkItemCategory): ReadonlyArray<ParkItemType> {
  switch (category) {
    case 'Restaurant':
      return ['Restaurant', 'Snack'];
    case 'Hotel':
      return ['Hotel'];
    case 'Animal':
      return ['AnimalExhibit'];
    case 'Show':
      return ['Show'];
    case 'Shop':
      return ['Shop'];
    case 'Service':
      return ['Service', 'Toilets', 'FirstAid', 'Information', 'Locker', 'Parking'];
    case 'Transport':
      return ['Transport', 'Station'];
    case 'Other':
      return ['Other'];
    case 'Attraction':
    default:
      return [
        'Attraction',
        'RollerCoaster',
        'WaterRide',
        'FlatRide',
        'DarkRide',
        'FamilyRide',
        'ThrillRide',
        'TransportRide',
        'WalkThrough',
        'Playground',
        'InteractiveExperience',
        'Game',
        'MeetAndGreet',
        'ObservationRide',
        'Other'
      ];
  }
}
