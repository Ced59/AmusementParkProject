import { Inject, Injectable, Signal, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';
import {
  AdminParkItemQuickCreateDraft,
  AdminParkItemWorkbenchCoordinates
} from '../models/admin-park-item-workbench.model';
import {
  createAdminParkItemQuickCreateDraft,
  mapAdminParkItemQuickCreateDraftToParkItem
} from '../mappers/admin-park-item-quick-create.mapper';
import {
  ADMIN_PARK_ITEM_WORKBENCH_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemWorkbenchStateParkItemsApiServicePort
} from './admin-park-item-workbench-state-data.ports';

@Injectable()
export class AdminParkItemWorkbenchStateFacade {
  private readonly isCreatingSignal = signal(false);

  public readonly isCreating: Signal<boolean> = this.isCreatingSignal.asReadonly();

  constructor(
    @Inject(ADMIN_PARK_ITEM_WORKBENCH_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminParkItemWorkbenchStateParkItemsApiServicePort
  ) {
  }

  createDraft(parkId: string, overrides: Partial<AdminParkItemQuickCreateDraft> = {}): AdminParkItemQuickCreateDraft {
    return createAdminParkItemQuickCreateDraft(parkId, overrides);
  }

  async createQuickItem(
    draft: AdminParkItemQuickCreateDraft,
    fallbackCoordinates: AdminParkItemWorkbenchCoordinates
  ): Promise<ParkItem> {
    this.isCreatingSignal.set(true);

    try {
      const item: ParkItem = mapAdminParkItemQuickCreateDraftToParkItem(draft, fallbackCoordinates);
      return await firstValueFrom(this.parkItemsApiService.createParkItem(item));
    } finally {
      this.isCreatingSignal.set(false);
    }
  }
}
