import { Injectable, Signal, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParksApiService } from '@data-access/parks/parks-api.service';

@Injectable()
export class AdminParkEditStateFacade {
  private readonly isSavingSignal = signal(false);

  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();

  constructor(private readonly parksApiService: ParksApiService) {
  }

  async loadPark(parkId: string): Promise<Park> {
    return await firstValueFrom(this.parksApiService.getParkById(parkId));
  }

  async savePark(parkId: string | null, park: Park): Promise<Park> {
    this.isSavingSignal.set(true);

    try {
      if (parkId) {
        return await firstValueFrom(this.parksApiService.updatePark(parkId, park));
      }

      return await firstValueFrom(this.parksApiService.createPark(park));
    } finally {
      this.isSavingSignal.set(false);
    }
  }
}
