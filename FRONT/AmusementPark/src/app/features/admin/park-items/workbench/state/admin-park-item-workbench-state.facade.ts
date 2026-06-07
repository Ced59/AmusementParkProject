import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, Signal, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import {
  AdminParkItemDuplicateWarning,
  AdminParkItemQuickCreateContext,
  AdminParkItemQuickCreateDraft,
  AdminParkItemWorkbenchCoordinates
} from '../models/admin-park-item-workbench.model';
import {
  createAdminParkItemQuickCreateDraft,
  createAdminParkItemQuickCreateDraftFromContext,
  createAdminParkItemQuickCreateDraftFromParkItem,
  createAdminParkItemQuickCreateDraftFromRow,
  createAdminParkItemQuickCreateContext,
  findAdminParkItemDuplicateWarnings,
  mapAdminParkItemQuickCreateDraftToParkItem
} from '../mappers/admin-park-item-quick-create.mapper';
import {
  ADMIN_PARK_ITEM_WORKBENCH_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemWorkbenchStateParkItemsApiServicePort
} from './admin-park-item-workbench-state-data.ports';

@Injectable()
export class AdminParkItemWorkbenchStateFacade {
  private readonly storageKeyPrefix: string = 'admin-park-item-workbench-context';
  private readonly isCreatingSignal = signal(false);
  private readonly duplicateWarningsSignal = signal<AdminParkItemDuplicateWarning[]>([]);

  public readonly isCreating: Signal<boolean> = this.isCreatingSignal.asReadonly();
  public readonly duplicateWarnings: Signal<AdminParkItemDuplicateWarning[]> = this.duplicateWarningsSignal.asReadonly();

  constructor(
    @Inject(ADMIN_PARK_ITEM_WORKBENCH_STATE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminParkItemWorkbenchStateParkItemsApiServicePort,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {
  }

  createDraft(parkId: string, overrides: Partial<AdminParkItemQuickCreateDraft> = {}): AdminParkItemQuickCreateDraft {
    return createAdminParkItemQuickCreateDraft(parkId, overrides);
  }

  createDraftFromStoredContext(parkId: string): AdminParkItemQuickCreateDraft {
    return createAdminParkItemQuickCreateDraftFromContext(parkId, this.readContext(parkId));
  }

  createNextDraft(draft: AdminParkItemQuickCreateDraft): AdminParkItemQuickCreateDraft {
    const context: AdminParkItemQuickCreateContext = createAdminParkItemQuickCreateContext(draft);
    this.writeContext(draft.parkId, context);
    return createAdminParkItemQuickCreateDraftFromContext(draft.parkId, context, { name: '' });
  }

  createDraftFromRow(parkId: string, row: ParkItemAdminRow): AdminParkItemQuickCreateDraft {
    return createAdminParkItemQuickCreateDraftFromRow(parkId, row);
  }

  async createDraftFromExistingItem(row: ParkItemAdminRow): Promise<AdminParkItemQuickCreateDraft> {
    const item: ParkItem = await firstValueFrom(this.parkItemsApiService.getParkItemById(row.id));
    return createAdminParkItemQuickCreateDraftFromParkItem(item);
  }

  async createQuickItem(
    draft: AdminParkItemQuickCreateDraft,
    fallbackCoordinates: AdminParkItemWorkbenchCoordinates
  ): Promise<ParkItem> {
    this.isCreatingSignal.set(true);

    try {
      const item: ParkItem = mapAdminParkItemQuickCreateDraftToParkItem(draft, fallbackCoordinates);
      const createdItem: ParkItem = await firstValueFrom(this.parkItemsApiService.createParkItem(item));
      this.writeContext(draft.parkId, createAdminParkItemQuickCreateContext(draft));
      return createdItem;
    } finally {
      this.isCreatingSignal.set(false);
    }
  }

  refreshDuplicateWarnings(
    draft: AdminParkItemQuickCreateDraft,
    rows: ParkItemAdminRow[]
  ): void {
    this.duplicateWarningsSignal.set(findAdminParkItemDuplicateWarnings(draft, rows));
  }

  clearDuplicateWarnings(): void {
    this.duplicateWarningsSignal.set([]);
  }

  private readContext(parkId: string): AdminParkItemQuickCreateContext | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const rawValue: string | null = window.localStorage.getItem(this.buildStorageKey(parkId));
    if (!rawValue) {
      return null;
    }

    try {
      return JSON.parse(rawValue) as AdminParkItemQuickCreateContext;
    } catch {
      window.localStorage.removeItem(this.buildStorageKey(parkId));
      return null;
    }
  }

  private writeContext(parkId: string, context: AdminParkItemQuickCreateContext): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    window.localStorage.setItem(this.buildStorageKey(parkId), JSON.stringify(context));
  }

  private buildStorageKey(parkId: string): string {
    return `${this.storageKeyPrefix}:${parkId.trim()}`;
  }
}
