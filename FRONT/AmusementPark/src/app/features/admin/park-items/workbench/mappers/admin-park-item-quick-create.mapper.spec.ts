import {
  createAdminParkItemQuickCreateDraft,
  createAdminParkItemQuickCreateDraftFromParkItem,
  findAdminParkItemDuplicateWarnings,
  resetAdminParkItemQuickCreateDraftForNext,
  mapAdminParkItemQuickCreateDraftToParkItem
} from './admin-park-item-quick-create.mapper';
import { AdminParkItemQuickCreateDraft } from '../models/admin-park-item-workbench.model';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';

describe('admin park item quick create mapper', () => {
  it('creates a draft with safe workbench defaults', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1');

    expect(draft.parkId).toBe('park-1');
    expect(draft.category).toBe('Attraction');
    expect(draft.type).toBe('Attraction');
    expect(draft.isVisible).toBeFalse();
    expect(draft.adminReviewStatus).toBe('ToReview');
  });

  it('maps a minimal draft to a hidden ToReview park item with empty descriptions', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1', {
      name: '  Taron  ',
      zoneId: ' zone-1 '
    });

    const item: ParkItem = mapAdminParkItemQuickCreateDraftToParkItem(draft, { latitude: 50.801, longitude: 6.879 });

    expect(item.name).toBe('Taron');
    expect(item.zoneId).toBe('zone-1');
    expect(item.latitude).toBe(50.801);
    expect(item.longitude).toBe(6.879);
    expect(item.descriptions).toEqual([]);
    expect(item.isVisible).toBeFalse();
    expect(item.adminReviewStatus).toBe('ToReview');
  });

  it('normalizes type when category changes', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1', {
      name: 'Burger',
      category: 'Restaurant',
      type: 'RollerCoaster'
    });

    const item: ParkItem = mapAdminParkItemQuickCreateDraftToParkItem(draft, { latitude: 1, longitude: 2 });

    expect(item.category).toBe('Restaurant');
    expect(item.type).toBe('Restaurant');
    expect(item.attractionDetails).toBeNull();
  });

  it('keeps manufacturer only for attraction drafts', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1', {
      name: 'Coaster',
      manufacturerId: ' intamin '
    });

    const item: ParkItem = mapAdminParkItemQuickCreateDraftToParkItem(draft, { latitude: 1, longitude: 2 });

    expect(item.attractionDetails?.manufacturerId).toBe('intamin');
  });

  it('keeps drop tower as an allowed attraction type', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1', {
      name: 'Drop tower',
      category: 'Attraction',
      type: 'DropTower'
    });

    const item: ParkItem = mapAdminParkItemQuickCreateDraftToParkItem(draft, { latitude: 1, longitude: 2 });

    expect(item.type).toBe('DropTower');
  });

  it('resets only the name when creating the next draft', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1', {
      name: 'First item',
      zoneId: 'zone-1',
      category: 'Restaurant',
      type: 'Snack',
      manufacturerId: 'manufacturer-1',
      isVisible: true,
      adminReviewStatus: 'Validated'
    });

    const nextDraft: AdminParkItemQuickCreateDraft = resetAdminParkItemQuickCreateDraftForNext(draft);

    expect(nextDraft.name).toBe('');
    expect(nextDraft.zoneId).toBe('zone-1');
    expect(nextDraft.category).toBe('Restaurant');
    expect(nextDraft.type).toBe('Snack');
    expect(nextDraft.manufacturerId).toBe('manufacturer-1');
    expect(nextDraft.isVisible).toBeTrue();
    expect(nextDraft.adminReviewStatus).toBe('Validated');
  });

  it('creates a duplicate draft from a full item including manufacturer and coordinates', () => {
    const item: ParkItem = {
      id: 'item-1',
      parkId: 'park-1',
      zoneId: 'zone-1',
      name: 'Original',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: 50,
      longitude: 3,
      descriptions: [],
      attractionDetails: { manufacturerId: 'manufacturer-1' },
      attractionLocations: null,
      isVisible: false,
      adminReviewStatus: 'ToReview'
    };

    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraftFromParkItem(item);

    expect(draft.name).toBe('');
    expect(draft.zoneId).toBe('zone-1');
    expect(draft.category).toBe('Attraction');
    expect(draft.type).toBe('RollerCoaster');
    expect(draft.manufacturerId).toBe('manufacturer-1');
    expect(draft.coordinates).toEqual({ latitude: 50, longitude: 3 });
  });

  it('warns about close names in the same park and zone', () => {
    const draft: AdminParkItemQuickCreateDraft = createAdminParkItemQuickCreateDraft('park-1', {
      name: 'Taron',
      zoneId: 'zone-1'
    });
    const rows: ParkItemAdminRow[] = [
      {
        id: 'item-1',
        parkId: 'park-1',
        parkName: 'Park',
        zoneId: 'zone-1',
        name: 'Taron Clone',
        category: 'Attraction',
        type: 'RollerCoaster',
        isVisible: false,
        adminReviewStatus: 'ToReview'
      },
      {
        id: 'item-2',
        parkId: 'park-1',
        parkName: 'Park',
        zoneId: 'zone-2',
        name: 'Taron Shop',
        category: 'Shop',
        type: 'Shop',
        isVisible: false,
        adminReviewStatus: 'ToReview'
      }
    ];

    expect(findAdminParkItemDuplicateWarnings(draft, rows).map((warning) => warning.id)).toEqual(['item-1']);
  });
});
