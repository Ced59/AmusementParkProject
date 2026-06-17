import { Injectable, Signal, signal, Inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ApiResponse } from '@app/models/shared/api_reponse';
import {
  AdminParkItemSequentialNavigationState,
  EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE
} from '@features/admin/park-items/models/admin-park-item-sequential-navigation.model';

import {
  ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemEditStateParkItemsApiServicePort,
  ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT,
  AdminParkItemEditStateParksApiServicePort
} from './admin-park-item-edit-state-data.ports';

interface ParkItemNavigationCacheEntry {
  readonly expiresAt: number;
  readonly rows: ParkItemAdminRow[];
}

@Injectable()
export class AdminParkItemEditStateFacade {
  private static readonly parkOptionsCacheTtlMs: number = 5 * 60 * 1000;
  private static readonly navigationCacheTtlMs: number = 2 * 60 * 1000;
  private static readonly pageSize: number = 100;
  private static readonly interPageRequestDelayMs: number = 50;
  private static parkOptionsCache: { expiresAt: number; options: EntitySelectOption[] } | null = null;
  private static readonly parkItemNavigationCache: Map<string, ParkItemNavigationCacheEntry> = new Map<string, ParkItemNavigationCacheEntry>();

  private readonly isSavingSignal = signal(false);
  private readonly parkOptionsSignal = signal<EntitySelectOption[]>([]);
  private readonly parkOptionsLoadingSignal = signal(false);
  private readonly sequentialNavigationStateSignal = signal<AdminParkItemSequentialNavigationState>(EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE);
  private navigationLoadVersion: number = 0;
  private parkOptionsFullyLoaded: boolean = false;

  public readonly isSaving: Signal<boolean> = this.isSavingSignal.asReadonly();
  public readonly parkOptions: Signal<EntitySelectOption[]> = this.parkOptionsSignal.asReadonly();
  public readonly parkOptionsLoading: Signal<boolean> = this.parkOptionsLoadingSignal.asReadonly();
  public readonly sequentialNavigationState: Signal<AdminParkItemSequentialNavigationState> = this.sequentialNavigationStateSignal.asReadonly();

  constructor(
    @Inject(ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminParkItemEditStateParkItemsApiServicePort,
    @Inject(ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT) private readonly parksApiService: AdminParkItemEditStateParksApiServicePort
  ) {
  }

  async loadItem(itemId: string): Promise<ParkItem> {
    return await firstValueFrom(this.parkItemsApiService.getParkItemById(itemId));
  }

  async ensureParkOption(parkId: string): Promise<void> {
    const normalizedParkId: string = parkId.trim();
    if (!normalizedParkId || this.hasParkOption(normalizedParkId)) {
      return;
    }

    const cachedOptions: { expiresAt: number; options: EntitySelectOption[] } | null = AdminParkItemEditStateFacade.parkOptionsCache;
    if (cachedOptions && cachedOptions.expiresAt > Date.now()) {
      this.parkOptionsSignal.set(cachedOptions.options);
      this.parkOptionsFullyLoaded = true;
      return;
    }

    try {
      const park: Park = await firstValueFrom(this.parksApiService.getParkById(normalizedParkId));
      if (!park.id) {
        return;
      }

      this.mergeParkOption({
        id: park.id,
        label: this.buildParkOptionLabel(park)
      });
    } catch (error: unknown) {
      console.error('Error loading current park option', error);
    }
  }

  async loadParkOptions(): Promise<void> {
    if (this.parkOptionsFullyLoaded || this.parkOptionsLoadingSignal()) {
      return;
    }

    const cachedOptions: { expiresAt: number; options: EntitySelectOption[] } | null = AdminParkItemEditStateFacade.parkOptionsCache;

    if (cachedOptions && cachedOptions.expiresAt > Date.now()) {
      this.parkOptionsSignal.set(cachedOptions.options);
      this.parkOptionsFullyLoaded = true;
      return;
    }

    this.parkOptionsLoadingSignal.set(true);

    try {
      const parks: Park[] = [];
      const pageSize: number = AdminParkItemEditStateFacade.pageSize;
      const firstResponse: ParksApiResponse = await firstValueFrom(this.parksApiService.getParksPaginated(1, pageSize));
      parks.push(...(firstResponse.data ?? []));

      const totalPages: number = firstResponse.pagination?.totalPages ?? 1;
      for (let currentPage: number = 2; currentPage <= totalPages; currentPage += 1) {
        await this.waitBeforeNextPagedRequest();
        const pageResponse: ParksApiResponse = await firstValueFrom(this.parksApiService.getParksPaginated(currentPage, pageSize));
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
      this.parkOptionsFullyLoaded = true;
      this.parkOptionsSignal.set(options);
    } finally {
      this.parkOptionsLoadingSignal.set(false);
    }
  }

  async loadSequentialNavigation(parkId: string, currentItemId: string, forceReload: boolean = false): Promise<void> {
    const normalizedParkId: string = parkId.trim();
    const normalizedCurrentItemId: string = currentItemId.trim();

    if (!normalizedParkId || !normalizedCurrentItemId) {
      this.clearSequentialNavigation();
      return;
    }

    const loadVersion: number = ++this.navigationLoadVersion;
    this.sequentialNavigationStateSignal.set({
      ...this.buildSequentialNavigationState([], normalizedCurrentItemId),
      isLoading: true
    });

    try {
      const rows: ParkItemAdminRow[] = await this.loadParkItemRowsForNavigation(normalizedParkId, forceReload);

      if (loadVersion !== this.navigationLoadVersion) {
        return;
      }

      this.sequentialNavigationStateSignal.set(this.buildSequentialNavigationState(rows, normalizedCurrentItemId));
    } catch (error: unknown) {
      console.error('Error loading park item sequential navigation', error);

      if (loadVersion !== this.navigationLoadVersion) {
        return;
      }

      this.sequentialNavigationStateSignal.set({
        ...EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE,
        currentItemId: normalizedCurrentItemId
      });
    }
  }

  clearSequentialNavigation(): void {
    this.navigationLoadVersion += 1;
    this.sequentialNavigationStateSignal.set(EMPTY_ADMIN_PARK_ITEM_SEQUENTIAL_NAVIGATION_STATE);
  }

  async saveItem(itemId: string | null, item: ParkItem): Promise<ParkItem> {
    this.isSavingSignal.set(true);

    try {
      const savedItem: ParkItem = itemId
        ? await firstValueFrom(this.parkItemsApiService.updateParkItem(itemId, item))
        : await firstValueFrom(this.parkItemsApiService.createParkItem(item));

      AdminParkItemEditStateFacade.parkItemNavigationCache.clear();
      return savedItem;
    } finally {
      this.isSavingSignal.set(false);
    }
  }

  invalidateParkOptionsCache(): void {
    AdminParkItemEditStateFacade.parkOptionsCache = null;
  }

  private async loadParkItemRowsForNavigation(parkId: string, forceReload: boolean): Promise<ParkItemAdminRow[]> {
    const cachedEntry: ParkItemNavigationCacheEntry | undefined = AdminParkItemEditStateFacade.parkItemNavigationCache.get(parkId);

    if (!forceReload && cachedEntry && cachedEntry.expiresAt > Date.now()) {
      return cachedEntry.rows;
    }

    const rows: ParkItemAdminRow[] = [];
    const pageSize: number = AdminParkItemEditStateFacade.pageSize;
    const firstResponse: ApiResponse<ParkItemAdminRow> = await firstValueFrom(
      this.parkItemsApiService.getParkItemsPaginated(1, pageSize, parkId)
    );
    rows.push(...(firstResponse.data ?? []));

    const totalPages: number = firstResponse.pagination?.totalPages ?? 1;
    for (let currentPage: number = 2; currentPage <= totalPages; currentPage += 1) {
      await this.waitBeforeNextPagedRequest();
      const pageResponse: ApiResponse<ParkItemAdminRow> = await firstValueFrom(this.parkItemsApiService.getParkItemsPaginated(currentPage, pageSize, parkId));
      rows.push(...(pageResponse.data ?? []));
    }

    const normalizedRows: ParkItemAdminRow[] = rows.filter((row: ParkItemAdminRow): boolean => !!row.id);
    AdminParkItemEditStateFacade.parkItemNavigationCache.set(parkId, {
      expiresAt: Date.now() + AdminParkItemEditStateFacade.navigationCacheTtlMs,
      rows: normalizedRows
    });

    return normalizedRows;
  }

  private async waitBeforeNextPagedRequest(): Promise<void> {
    await new Promise<void>((resolve: () => void): void => {
      setTimeout(resolve, AdminParkItemEditStateFacade.interPageRequestDelayMs);
    });
  }

  private buildSequentialNavigationState(rows: ParkItemAdminRow[], currentItemId: string): AdminParkItemSequentialNavigationState {
    const normalizedCurrentItemId: string = currentItemId.trim();
    const currentIndex: number = rows.findIndex((row: ParkItemAdminRow): boolean => row.id?.trim() === normalizedCurrentItemId);
    const totalItems: number = rows.length;
    const currentPosition: number = currentIndex >= 0 ? currentIndex + 1 : 0;

    return {
      isLoading: false,
      currentItemId: normalizedCurrentItemId,
      currentPosition,
      remainingItems: currentPosition > 0 ? Math.max(totalItems - currentPosition, 0) : 0,
      totalItems,
      previousItemId: currentIndex > 0 ? rows[currentIndex - 1].id : null,
      nextItemId: currentIndex >= 0 && currentIndex < totalItems - 1 ? rows[currentIndex + 1].id : null
    };
  }

  private hasParkOption(parkId: string): boolean {
    return this.parkOptionsSignal().some((option: EntitySelectOption): boolean => option.id === parkId);
  }

  private mergeParkOption(option: EntitySelectOption): void {
    const existingOptions: EntitySelectOption[] = this.parkOptionsSignal();
    if (existingOptions.some((existingOption: EntitySelectOption): boolean => existingOption.id === option.id)) {
      return;
    }

    this.parkOptionsSignal.set(
      [...existingOptions, option]
        .sort((left: EntitySelectOption, right: EntitySelectOption): number => left.label.localeCompare(right.label))
    );
  }

  private buildParkOptionLabel(park: Park): string {
    const name: string = park.name?.trim() || park.id || '';
    const countryCode: string | undefined = park.countryCode?.trim();
    const city: string | undefined = park.city?.trim();
    const details: string[] = [city, countryCode].filter((value: string | undefined): value is string => !!value);

    return details.length > 0 ? `${name} — ${details.join(', ')}` : name;
  }
}
