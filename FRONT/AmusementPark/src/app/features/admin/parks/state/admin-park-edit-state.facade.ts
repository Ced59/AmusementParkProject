import { Injectable, Signal, signal, Inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkOpeningHoursSchedule } from '@app/models/parks/park-opening-hours';

import {
  ADMIN_PARK_EDIT_STATE_PARKS_API_SERVICE_PORT,
  AdminParkEditStateParksApiServicePort
} from './admin-park-edit-state-data.ports';
@Injectable()
export class AdminParkEditStateFacade {
  private readonly isSavingSignal = signal(false);
  private readonly openingHoursLoadingSignal = signal(false);
  private readonly openingHoursSavingSignal = signal(false);

  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();
  public readonly openingHoursLoading: Signal<boolean> = this.openingHoursLoadingSignal.asReadonly();
  public readonly openingHoursSaving: Signal<boolean> = this.openingHoursSavingSignal.asReadonly();

  constructor(@Inject(ADMIN_PARK_EDIT_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: AdminParkEditStateParksApiServicePort) {
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

  async loadOpeningHours(parkId: string): Promise<ParkOpeningHoursSchedule> {
    this.openingHoursLoadingSignal.set(true);

    try {
      return await firstValueFrom(this.parksApiService.getAdminParkOpeningHours(parkId));
    } finally {
      this.openingHoursLoadingSignal.set(false);
    }
  }

  async saveOpeningHours(parkId: string, schedule: ParkOpeningHoursSchedule): Promise<ParkOpeningHoursSchedule> {
    this.openingHoursSavingSignal.set(true);

    try {
      return await firstValueFrom(this.parksApiService.upsertAdminParkOpeningHours(parkId, schedule));
    } finally {
      this.openingHoursSavingSignal.set(false);
    }
  }
}
