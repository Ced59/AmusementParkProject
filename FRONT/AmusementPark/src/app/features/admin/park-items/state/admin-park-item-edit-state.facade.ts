import { Injectable, Signal, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';

@Injectable()
export class AdminParkItemEditStateFacade {
  private readonly isSavingSignal = signal(false);

  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();

  constructor(private readonly parkItemsApiService: ParkItemsApiService) {
  }

  async loadItem(itemId: string): Promise<ParkItem> {
    return await firstValueFrom(this.parkItemsApiService.getParkItemById(itemId));
  }

  async saveItem(itemId: string | null, item: ParkItem): Promise<ParkItem> {
    this.isSavingSignal.set(true);

    try {
      if (itemId) {
        return await firstValueFrom(this.parkItemsApiService.updateParkItem(itemId, item));
      }

      return await firstValueFrom(this.parkItemsApiService.createParkItem(item));
    } finally {
      this.isSavingSignal.set(false);
    }
  }
}
