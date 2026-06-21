import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ITEMS_DATA_PORT,
  ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ZONES_DATA_PORT
} from './admin-contextual-block-child-add-data.ports';
import { AdminContextualBlockChildAddFacade } from './admin-contextual-block-child-add.facade';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

describe('AdminContextualBlockChildAddFacade', () => {
  let facade: AdminContextualBlockChildAddFacade;
  let parkItemsApi: jasmine.SpyObj<ParkItemsApiService>;
  let parkZonesApi: jasmine.SpyObj<ParkZonesApiService>;
  let refreshEvents: jasmine.SpyObj<AdminContextualBlockRefreshEvents>;

  beforeEach(() => {
    parkItemsApi = jasmine.createSpyObj<ParkItemsApiService>('ParkItemsApiService', ['createParkItem']);
    parkZonesApi = jasmine.createSpyObj<ParkZonesApiService>('ParkZonesApiService', ['getParkZonesByParkId']);
    refreshEvents = jasmine.createSpyObj<AdminContextualBlockRefreshEvents>('AdminContextualBlockRefreshEvents', ['notifyBlockApplied']);
    parkZonesApi.getParkZonesByParkId.and.returnValue(of(createZones()));

    TestBed.configureTestingModule({
      providers: [
        AdminContextualBlockChildAddFacade,
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ITEMS_DATA_PORT,
          useValue: parkItemsApi
        },
        {
          provide: ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ZONES_DATA_PORT,
          useValue: parkZonesApi
        },
        {
          provide: AdminContextualBlockRefreshEvents,
          useValue: refreshEvents
        }
      ]
    });

    facade = TestBed.inject(AdminContextualBlockChildAddFacade);
  });

  it('loads park zones when a targeted child add block is selected', () => {
    facade.resetForBlock(createBlock());

    expect(parkZonesApi.getParkZonesByParkId).toHaveBeenCalledOnceWith('park-1');
    expect(facade.zoneOptions()).toEqual([
      { id: 'zone-1', label: 'Berlin', latitude: 50.1, longitude: 3.2 },
      { id: 'zone-2', label: 'zone-2', latitude: null, longitude: null }
    ]);
  });

  it('creates a hidden to-review park item attached to the selected park and zone', () => {
    const createdItem: ParkItem = {
      parkId: 'park-1',
      zoneId: 'zone-1',
      id: 'item-1',
      name: 'New ride',
      category: 'Attraction',
      type: 'Attraction',
      latitude: 50.1,
      longitude: 3.2,
      isVisible: false,
      adminReviewStatus: 'ToReview'
    };
    parkItemsApi.createParkItem.and.returnValue(of(createdItem));
    const block: AdminContextualBlockInstance = createBlock();
    facade.resetForBlock(block);

    facade.updateItemName(' New ride ');
    facade.updateSelectedZoneId('zone-1');
    facade.createChild(block);

    expect(parkItemsApi.createParkItem).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      parkId: 'park-1',
      zoneId: 'zone-1',
      name: 'New ride',
      category: 'Attraction',
      type: 'Attraction',
      descriptions: [],
      latitude: 50.1,
      longitude: 3.2,
      isVisible: false,
      adminReviewStatus: 'ToReview'
    }));
    expect(facade.successKey()).toBe('admin.contextualBlocks.drawer.addChildSucceeded');
    expect(facade.createdItemAdminRoute()).toEqual(['/', 'fr', 'admin', 'parks', 'edit', 'park-1', 'items', 'item-1']);
    expect(refreshEvents.notifyBlockApplied).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      blockType: 'park.hero',
      entityType: 'Park',
      entityId: 'park-1'
    }));
  });

  it('keeps the draft name when creation fails', () => {
    parkItemsApi.createParkItem.and.returnValue(throwError(() => new Error('failed')));
    const block: AdminContextualBlockInstance = createBlock();
    facade.resetForBlock(block);

    facade.updateItemName('Draft item');
    facade.createChild(block);

    expect(facade.itemName()).toBe('Draft item');
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.addChildError');
    expect(refreshEvents.notifyBlockApplied).not.toHaveBeenCalled();
  });

  it('does not create a child without the targeted capability', () => {
    const block: AdminContextualBlockInstance = {
      ...createBlock(),
      capabilities: ['fullAdminEdit']
    };

    facade.updateItemName('Draft item');
    facade.createChild(block);

    expect(parkItemsApi.createParkItem).not.toHaveBeenCalled();
    expect(facade.errorKey()).toBe('admin.contextualBlocks.drawer.addChildUnavailable');
  });
});

function createZones(): ParkZone[] {
  return [
    {
      id: 'zone-1',
      parkId: 'park-1',
      name: 'Berlin',
      latitude: 50.1,
      longitude: 3.2
    },
    {
      id: 'zone-2',
      parkId: 'park-1'
    }
  ];
}

function createBlock(): AdminContextualBlockInstance {
  return {
    id: 'park.hero:park-1',
    type: 'park.hero',
    entityType: 'Park',
    entityId: 'park-1',
    contextLabel: 'Phantasialand',
    ids: { parkId: 'park-1' },
    labelKey: 'admin.contextualBlocks.blocks.parkHero.label',
    descriptionKey: 'admin.contextualBlocks.blocks.parkHero.description',
    iconClass: 'pi pi-image',
    capabilities: ['fullAdminEdit', 'targetedChildAdd'],
    jsonScope: ['park.id'],
    localizedLanguageCodes: [],
    locationFallbackCenter: null,
    adminRoute: ['/', 'fr', 'admin', 'parks', 'edit', 'park-1']
  };
}
