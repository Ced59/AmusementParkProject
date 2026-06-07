import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import {
  ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS,
  AdminParkItemDuplicateWarning,
  AdminParkItemQuickCreateContext,
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

export function createAdminParkItemQuickCreateDraftFromContext(
  parkId: string,
  context: AdminParkItemQuickCreateContext | null,
  overrides: Partial<AdminParkItemQuickCreateDraft> = {}
): AdminParkItemQuickCreateDraft {
  return createAdminParkItemQuickCreateDraft(parkId, {
    zoneId: context?.zoneId ?? null,
    category: context?.category ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.category,
    type: context?.type ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.type,
    manufacturerId: context?.manufacturerId ?? null,
    isVisible: context?.isVisible ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.isVisible,
    adminReviewStatus: context?.adminReviewStatus ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.adminReviewStatus,
    ...overrides
  });
}

export function createAdminParkItemQuickCreateContext(
  draft: AdminParkItemQuickCreateDraft
): AdminParkItemQuickCreateContext {
  return {
    zoneId: normalizeOptionalText(draft.zoneId),
    category: draft.category ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.category,
    type: normalizeTypeForCategory(
      draft.type,
      draft.category ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.category
    ),
    manufacturerId: normalizeOptionalText(draft.manufacturerId),
    isVisible: draft.isVisible ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.isVisible,
    adminReviewStatus: draft.adminReviewStatus ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.adminReviewStatus
  };
}

export function resetAdminParkItemQuickCreateDraftForNext(
  draft: AdminParkItemQuickCreateDraft
): AdminParkItemQuickCreateDraft {
  return createAdminParkItemQuickCreateDraftFromContext(
    draft.parkId,
    createAdminParkItemQuickCreateContext(draft),
    { name: '' }
  );
}

export function createAdminParkItemQuickCreateDraftFromRow(
  parkId: string,
  row: ParkItemAdminRow
): AdminParkItemQuickCreateDraft {
  return createAdminParkItemQuickCreateDraft(parkId, {
    zoneId: row.zoneId ?? null,
    name: '',
    category: row.category,
    type: row.type,
    isVisible: row.isVisible,
    adminReviewStatus: row.adminReviewStatus
  });
}

export function createAdminParkItemQuickCreateDraftFromParkItem(
  item: ParkItem
): AdminParkItemQuickCreateDraft {
  return createAdminParkItemQuickCreateDraft(item.parkId, {
    zoneId: item.zoneId ?? null,
    name: '',
    category: item.category,
    type: item.type,
    manufacturerId: item.attractionDetails?.manufacturerId ?? null,
    coordinates: hasValidCoordinates(item.latitude, item.longitude)
      ? {
          latitude: item.latitude!,
          longitude: item.longitude!
        }
      : null,
    isVisible: item.isVisible ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.isVisible,
    adminReviewStatus: item.adminReviewStatus ?? ADMIN_PARK_ITEM_WORKBENCH_DEFAULTS.adminReviewStatus
  });
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

export function findAdminParkItemDuplicateWarnings(
  draft: AdminParkItemQuickCreateDraft,
  rows: ParkItemAdminRow[]
): AdminParkItemDuplicateWarning[] {
  const normalizedName: string = normalizeSearchText(draft.name);

  if (normalizedName.length < 3) {
    return [];
  }

  return rows
    .filter((row: ParkItemAdminRow) => {
      if (!row.id || row.parkId !== draft.parkId) {
        return false;
      }

      const rowName: string = normalizeSearchText(row.name);
      const sameZone: boolean = !draft.zoneId || row.zoneId === draft.zoneId;
      return sameZone && (rowName.includes(normalizedName) || normalizedName.includes(rowName));
    })
    .slice(0, 5)
    .map((row: ParkItemAdminRow) => ({
      id: row.id,
      name: row.name,
      zoneId: row.zoneId ?? null,
      category: row.category,
      type: row.type
    }));
}

export function getAllowedTypesForAdminParkItemCategory(
  category: ParkItemCategory
): ReadonlyArray<ParkItemType> {
  return getAllowedTypes(category);
}

function hasValidCoordinates(latitude: number | null | undefined, longitude: number | null | undefined): boolean {
  return latitude != null
    && longitude != null
    && Number.isFinite(latitude)
    && Number.isFinite(longitude);
}

function normalizeOptionalText(value: string | null | undefined): string | null {
  const normalized: string = String(value ?? '').trim();
  return normalized.length > 0 ? normalized : null;
}

function normalizeSearchText(value: string | null | undefined): string {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .trim()
    .toLocaleLowerCase();
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
