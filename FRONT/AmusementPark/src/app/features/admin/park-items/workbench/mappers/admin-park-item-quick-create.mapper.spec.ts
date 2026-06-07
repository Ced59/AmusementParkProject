import {
  createAdminParkItemQuickCreateDraft,
  mapAdminParkItemQuickCreateDraftToParkItem
} from './admin-park-item-quick-create.mapper';
import { AdminParkItemQuickCreateDraft } from '../models/admin-park-item-workbench.model';
import { ParkItem } from '@app/models/parks/park-item';

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
});
