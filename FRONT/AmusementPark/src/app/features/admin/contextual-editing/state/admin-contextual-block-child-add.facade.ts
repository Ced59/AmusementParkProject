import { Inject, Injectable, Signal, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { AdminReviewStatus } from '@app/models/admin/admin-review-status';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParkZone } from '@app/models/parks/park-zone';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ITEMS_DATA_PORT,
  ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ZONES_DATA_PORT,
  AdminContextualBlockChildAddParkItemsDataPort,
  AdminContextualBlockChildAddParkZonesDataPort
} from './admin-contextual-block-child-add-data.ports';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';

export interface AdminContextualBlockChildAddZoneOption {
  readonly id: string;
  readonly label: string;
  readonly latitude: number | null;
  readonly longitude: number | null;
}

const DEFAULT_CHILD_CATEGORY: ParkItemCategory = 'Attraction';
const DEFAULT_CHILD_TYPE: ParkItemType = 'Attraction';
const DEFAULT_CHILD_REVIEW_STATUS: AdminReviewStatus = 'ToReview';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockChildAddFacade {
  private readonly itemNameSignal = signal<string>('');
  private readonly selectedZoneIdSignal = signal<string | null>(null);
  private readonly zoneOptionsSignal = signal<readonly AdminContextualBlockChildAddZoneOption[]>([]);
  private readonly isLoadingZonesSignal = signal<boolean>(false);
  private readonly isCreatingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly successKeySignal = signal<string | null>(null);
  private readonly createdItemAdminRouteSignal = signal<readonly string[] | null>(null);
  private activeBlockId: string | null = null;

  public readonly itemName: Signal<string> = this.itemNameSignal.asReadonly();
  public readonly selectedZoneId: Signal<string | null> = this.selectedZoneIdSignal.asReadonly();
  public readonly zoneOptions: Signal<readonly AdminContextualBlockChildAddZoneOption[]> = this.zoneOptionsSignal.asReadonly();
  public readonly isLoadingZones: Signal<boolean> = this.isLoadingZonesSignal.asReadonly();
  public readonly isCreating: Signal<boolean> = this.isCreatingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly successKey: Signal<string | null> = this.successKeySignal.asReadonly();
  public readonly createdItemAdminRoute: Signal<readonly string[] | null> = this.createdItemAdminRouteSignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ITEMS_DATA_PORT) private readonly parkItemsApi: AdminContextualBlockChildAddParkItemsDataPort,
    @Inject(ADMIN_CONTEXTUAL_BLOCK_CHILD_ADD_PARK_ZONES_DATA_PORT) private readonly parkZonesApi: AdminContextualBlockChildAddParkZonesDataPort,
    private readonly refreshEvents: AdminContextualBlockRefreshEvents
  ) {
  }

  canAddChild(block: AdminContextualBlockInstance | null): boolean {
    return Boolean(block?.capabilities.includes('targetedChildAdd'));
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.itemNameSignal.set('');
    this.selectedZoneIdSignal.set(null);
    this.zoneOptionsSignal.set([]);
    this.isLoadingZonesSignal.set(false);
    this.isCreatingSignal.set(false);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.createdItemAdminRouteSignal.set(null);

    if (block !== null && this.canAddChild(block)) {
      this.loadZones(block);
    }
  }

  updateItemName(value: string): void {
    this.itemNameSignal.set(value);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.createdItemAdminRouteSignal.set(null);
  }

  updateSelectedZoneId(value: string | null): void {
    const normalizedValue: string | null = this.normalizeOptionalText(value);
    this.selectedZoneIdSignal.set(normalizedValue);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.createdItemAdminRouteSignal.set(null);
  }

  createChild(block: AdminContextualBlockInstance): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.createdItemAdminRouteSignal.set(null);

    if (!this.canAddChild(block)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.addChildUnavailable');
      return;
    }

    const parkId: string | null = this.resolveParkId(block);
    if (!parkId) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.addChildError');
      return;
    }

    const name: string = this.itemNameSignal().trim();
    if (name.length === 0) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.addChildNameRequired');
      return;
    }

    const selectedZone: AdminContextualBlockChildAddZoneOption | null = this.resolveSelectedZone();
    const item: ParkItem = {
      parkId,
      zoneId: selectedZone?.id ?? null,
      name,
      category: DEFAULT_CHILD_CATEGORY,
      type: DEFAULT_CHILD_TYPE,
      subtype: null,
      latitude: selectedZone?.latitude ?? null,
      longitude: selectedZone?.longitude ?? null,
      descriptions: [],
      attractionDetails: null,
      attractionLocations: null,
      isVisible: false,
      adminReviewStatus: DEFAULT_CHILD_REVIEW_STATUS
    };

    this.isCreatingSignal.set(true);
    this.parkItemsApi.createParkItem(item)
      .pipe(finalize((): void => this.isCreatingSignal.set(false)))
      .subscribe({
        next: (createdItem: ParkItem): void => {
          this.itemNameSignal.set('');
          this.successKeySignal.set('admin.contextualBlocks.drawer.addChildSucceeded');
          this.createdItemAdminRouteSignal.set(this.buildCreatedItemRoute(block, parkId, createdItem.id ?? null));
          this.refreshEvents.notifyBlockApplied({
            blockType: block.type,
            entityType: block.entityType,
            entityId: parkId,
            appliedAtUtc: new Date().toISOString()
          });
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.addChildError');
        }
      });
  }

  private loadZones(block: AdminContextualBlockInstance): void {
    const parkId: string | null = this.resolveParkId(block);
    if (!parkId) {
      return;
    }

    this.isLoadingZonesSignal.set(true);
    this.parkZonesApi.getParkZonesByParkId(parkId)
      .pipe(finalize((): void => this.isLoadingZonesSignal.set(false)))
      .subscribe({
        next: (zones: ParkZone[]): void => {
          this.zoneOptionsSignal.set(zones
            .filter((zone: ParkZone) => !!zone.id)
            .map((zone: ParkZone) => ({
              id: zone.id as string,
              label: this.resolveZoneLabel(zone),
              latitude: this.normalizeCoordinate(zone.latitude),
              longitude: this.normalizeCoordinate(zone.longitude)
            })));
        },
        error: (): void => {
          this.zoneOptionsSignal.set([]);
        }
      });
  }

  private resolveParkId(block: AdminContextualBlockInstance): string | null {
    return this.normalizeOptionalText(block.ids['parkId']) ?? this.normalizeOptionalText(block.entityId);
  }

  private resolveSelectedZone(): AdminContextualBlockChildAddZoneOption | null {
    const selectedZoneId: string | null = this.selectedZoneIdSignal();
    if (!selectedZoneId) {
      return null;
    }

    return this.zoneOptionsSignal().find((zone: AdminContextualBlockChildAddZoneOption) => zone.id === selectedZoneId) ?? null;
  }

  private resolveZoneLabel(zone: ParkZone): string {
    return this.normalizeOptionalText(zone.name)
      ?? this.normalizeOptionalText(zone.names?.find((name) => !!this.normalizeOptionalText(name.value))?.value)
      ?? zone.id
      ?? 'Zone';
  }

  private buildCreatedItemRoute(block: AdminContextualBlockInstance, parkId: string, itemId: string | null): readonly string[] | null {
    if (!itemId) {
      return null;
    }

    const languageCode: string = this.normalizeOptionalText(block.adminRoute?.[1]) ?? 'en';
    return ['/', languageCode, 'admin', 'parks', 'edit', parkId, 'items', itemId];
  }

  private normalizeOptionalText(value: string | null | undefined): string | null {
    const normalized: string = String(value ?? '').trim();
    return normalized.length > 0 ? normalized : null;
  }

  private normalizeCoordinate(value: number | null | undefined): number | null {
    return value != null && Number.isFinite(value) ? value : null;
  }
}
