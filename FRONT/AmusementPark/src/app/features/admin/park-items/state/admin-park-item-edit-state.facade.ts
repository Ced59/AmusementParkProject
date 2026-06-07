import { Injectable, Signal, signal, Inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import {
  ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemEditStateParkItemsApiServicePort,
  ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT,
  AdminParkItemEditStateParksApiServicePort
} from './admin-park-item-edit-state-data.ports';
@Injectable()
export class AdminParkItemEditStateFacade {
  private static readonly parkOptionsCacheTtlMs: number = 5 * 60 * 1000;
  private static parkOptionsCache: { expiresAt: number; options: EntitySelectOption[] } | null = null;

  private readonly isSavingSignal = signal(false);
  private readonly parkOptionsSignal = signal<EntitySelectOption[]>([]);
  private readonly parkOptionsLoadingSignal = signal(false);

  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();
  public readonly parkOptions: Signal<EntitySelectOption[]> = this.parkOptionsSignal.asReadonly();
  public readonly parkOptionsLoading: Signal<boolean> = this.parkOptionsLoadingSignal.asReadonly();

  constructor(
    @Inject(ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminParkItemEditStateParkItemsApiServicePort,
    @Inject(ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: AdminParkItemEditStateParksApiServicePort
  ) {
  }

  async loadItem(itemId: string): Promise<ParkItem> {
    return await firstValueFrom(this.parkItemsApiService.getParkItemById(itemId));
  }

  async loadParkOptions(): Promise<void> {
    if (this.parkOptionsSignal().length > 0 || this.parkOptionsLoadingSignal()) {
      return;
    }

    const cachedOptions: { expiresAt: number; options: EntitySelectOption[] } | null = AdminParkItemEditStateFacade.parkOptionsCache;

    if (cachedOptions && cachedOptions.expiresAt > Date.now()) {
      this.parkOptionsSignal.set(cachedOptions.options);
      return;
    }

    this.parkOptionsLoadingSignal.set(true);

    try {
      const parks: Park[] = [];
      const firstResponse: ParksApiResponse = await firstValueFrom(this.parksApiService.getParksPaginated(1, 100));
      parks.push(...(firstResponse.data ?? []));

      const totalPages: number = firstResponse.pagination?.totalPages ?? 1;
      for (let currentPage: number = 2; currentPage <= totalPages; currentPage += 1) {
        const pageResponse: ParksApiResponse = await firstValueFrom(this.parksApiService.getParksPaginated(currentPage, 100));
        parks.push(...(pageResponse.data ?? []));
      }

      const options: EntitySelectOption[] = parks
        .filter((park: Park): park is Park & { id: string } => !!park.id)
        .map((park: Park & { id: string }): EntitySelectOption => ({
          id: park.id,
          label: this.buildParkOptionLabel(park)
        }))
        .sort((left: EntitySelectOption, right: EntitySelectOption): number => left.label.localeCompare(right.label));

      AdminParkItemEditStateFacade.parkOptionsCache = {
        expiresAt: Date.now() + AdminParkItemEditStateFacade.parkOptionsCacheTtlMs,
        options
      };
      this.parkOptionsSignal.set(options);
    } finally {
      this.parkOptionsLoadingSignal.set(false);
    }
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

  invalidateParkOptionsCache(): void {
    AdminParkItemEditStateFacade.parkOptionsCache = null;
  }

  private buildParkOptionLabel(park: Park): string {
    const name: string = park.name?.trim() || park.id || '';
    const countryCode: string | undefined = park.countryCode?.trim();
    const city: string | undefined = park.city?.trim();
    const details: string[] = [city, countryCode].filter((value: string | undefined): value is string => !!value);

    return details.length > 0 ? `${name} — ${details.join(', ')}` : name;
  }
}
