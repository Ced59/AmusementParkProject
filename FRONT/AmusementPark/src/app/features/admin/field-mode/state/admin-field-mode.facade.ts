import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, catchError, firstValueFrom, forkJoin, map, of, switchMap } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { PagedResult } from '@shared/models/contracts';
import { ImageUploadSecurityService } from '@shared/utils/security';

import {
  ADMIN_FIELD_MODE_GEOLOCATION_PORT,
  ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT,
  ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT,
  ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT,
  ADMIN_FIELD_MODE_PROCESSED_STATUS_PORT,
  AdminFieldModeGeolocationPort,
  AdminFieldModeImagesApiServicePort,
  AdminFieldModeParkItemsApiServicePort,
  AdminFieldModeParksApiServicePort,
  AdminFieldModeProcessedStatusPort
} from './admin-field-mode-data.ports';
import { readAdminFieldModeSelectedParkId, writeAdminFieldModeSelectedParkId } from './admin-field-mode-storage';
import {
  ADMIN_FIELD_MODE_GPS_MAX_AGE_MS,
  ADMIN_FIELD_MODE_LOCATION_OPTIONS,
  ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS,
  ADMIN_FIELD_MODE_SELECTED_PARK_STORAGE_KEY,
  AdminFieldModeFilter,
  AdminFieldModeGpsStatus,
  AdminFieldModeItemRow,
  AdminFieldModeLocationKey,
  AdminFieldModeParkOption,
  AdminFieldModePhotoCategoryOption,
  AdminFieldModePosition,
  AdminFieldModeProcessedFilter
} from '../models/admin-field-mode.model';

@Injectable()
export class AdminFieldModeFacade {
  private readonly parksSignal = signal<Park[]>([]);
  private readonly rowsSignal = signal<AdminFieldModeItemRow[]>([]);
  private readonly selectedParkIdSignal = signal<string | null>(null);
  private readonly selectedItemSignal = signal<ParkItem | null>(null);
  private readonly parkSearchSignal = signal('');
  private readonly searchSignal = signal('');
  private readonly filterSignal = signal<AdminFieldModeFilter>('all');
  private readonly processedFilterSignal = signal<AdminFieldModeProcessedFilter>('unprocessed');
  private readonly loadingSignal = signal(false);
  private readonly savingSignal = signal(false);
  private readonly uploadingSignal = signal(false);
  private readonly gpsStatusSignal = signal<AdminFieldModeGpsStatus>('idle');
  private readonly gpsErrorSignal = signal<string | null>(null);
  private readonly currentPositionSignal = signal<AdminFieldModePosition | null>(null);
  private readonly selectedPhotoCategorySlugSignal = signal<string>(ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS[0].slug);
  private readonly selectedPhotoFileSignal = signal<File | null>(null);
  private readonly selectedPhotoDescriptionSignal = signal('');
  private readonly selectedLocationKeySignal = signal<AdminFieldModeLocationKey>('general');
  private readonly messageKeySignal = signal<string | null>(null);
  private readonly localProcessedOverrides = new Map<string, boolean>();
  private parkSearchRequestId = 0;
  private itemLoadRequestId = 0;

  public readonly parks: Signal<Park[]> = this.parksSignal.asReadonly();
  public readonly rows: Signal<AdminFieldModeItemRow[]> = this.rowsSignal.asReadonly();
  public readonly selectedParkId: Signal<string | null> = this.selectedParkIdSignal.asReadonly();
  public readonly selectedItem: Signal<ParkItem | null> = this.selectedItemSignal.asReadonly();
  public readonly parkSearch: Signal<string> = this.parkSearchSignal.asReadonly();
  public readonly search: Signal<string> = this.searchSignal.asReadonly();
  public readonly filter: Signal<AdminFieldModeFilter> = this.filterSignal.asReadonly();
  public readonly processedFilter: Signal<AdminFieldModeProcessedFilter> = this.processedFilterSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly uploading: Signal<boolean> = this.uploadingSignal.asReadonly();
  public readonly gpsStatus: Signal<AdminFieldModeGpsStatus> = this.gpsStatusSignal.asReadonly();
  public readonly gpsError: Signal<string | null> = this.gpsErrorSignal.asReadonly();
  public readonly currentPosition: Signal<AdminFieldModePosition | null> = this.currentPositionSignal.asReadonly();
  public readonly selectedPhotoCategorySlug: Signal<string> = this.selectedPhotoCategorySlugSignal.asReadonly();
  public readonly selectedPhotoFile: Signal<File | null> = this.selectedPhotoFileSignal.asReadonly();
  public readonly selectedPhotoDescription: Signal<string> = this.selectedPhotoDescriptionSignal.asReadonly();
  public readonly selectedLocationKey: Signal<AdminFieldModeLocationKey> = this.selectedLocationKeySignal.asReadonly();
  public readonly messageKey: Signal<string | null> = this.messageKeySignal.asReadonly();
  public readonly photoCategoryOptions = signal(ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS).asReadonly();
  public readonly locationOptions = signal(ADMIN_FIELD_MODE_LOCATION_OPTIONS).asReadonly();
  public readonly parkOptions: Signal<AdminFieldModeParkOption[]> = computed(() => this.parksSignal()
    .filter((park: Park) => !!park.id)
    .map((park: Park) => ({ label: park.name ?? 'Unnamed park', value: park.id ?? '' })));
  public readonly filteredParkOptions: Signal<AdminFieldModeParkOption[]> = computed(() => this.parkOptions().slice(0, 30));
  public readonly selectedPark: Signal<Park | null> = computed(() => this.parksSignal().find((park: Park) => park.id === this.selectedParkIdSignal()) ?? null);
  public readonly selectedParkLabel: Signal<string | null> = computed(() => this.selectedPark()?.name ?? null);
  public readonly showParkSearch: Signal<boolean> = computed(() => !this.selectedParkIdSignal());
  public readonly filteredRows: Signal<AdminFieldModeItemRow[]> = computed(() => this.filterRows());
  public readonly totalItemCount: Signal<number> = computed(() => this.rowsSignal().length);
  public readonly processedItemCount: Signal<number> = computed(() => this.rowsSignal().filter((row: AdminFieldModeItemRow) => row.isProcessed).length);
  public readonly unprocessedItemCount: Signal<number> = computed(() => this.rowsSignal().filter((row: AdminFieldModeItemRow) => !row.isProcessed).length);
  public readonly missingPhotosCount: Signal<number> = computed(() => this.rowsSignal().filter((row: AdminFieldModeItemRow) => (row.photoCount ?? 0) === 0).length);
  public readonly missingGeneralLocationCount: Signal<number> = computed(() => this.rowsSignal().filter((row: AdminFieldModeItemRow) => !row.hasGeneralLocation).length);
  public readonly missingPreciseLocationCount: Signal<number> = computed(() => this.rowsSignal().filter((row: AdminFieldModeItemRow) => !row.hasAnyPreciseLocation).length);
  public readonly gpsReady: Signal<boolean> = computed(() => this.isPositionFresh(this.currentPositionSignal()));

  constructor(
    @Inject(ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT) private readonly parksApiService: AdminFieldModeParksApiServicePort,
    @Inject(ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminFieldModeParkItemsApiServicePort,
    @Inject(ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: AdminFieldModeImagesApiServicePort,
    @Inject(ADMIN_FIELD_MODE_GEOLOCATION_PORT) private readonly geolocationService: AdminFieldModeGeolocationPort,
    @Inject(ADMIN_FIELD_MODE_PROCESSED_STATUS_PORT) private readonly processedStatusService: AdminFieldModeProcessedStatusPort,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  initialize(routeItemId: string | null = null): void {
    const selectedParkId: string | null = readAdminFieldModeSelectedParkId(ADMIN_FIELD_MODE_SELECTED_PARK_STORAGE_KEY);
    this.selectedParkIdSignal.set(selectedParkId);
    this.loadingSignal.set(true);
    this.loadParkOptions('');

    if (selectedParkId) {
      this.ensureSelectedParkAvailable(selectedParkId);
      this.loadItems(selectedParkId, routeItemId);
      return;
    }

    if (routeItemId) {
      this.loadItemRouteContext(routeItemId);
      return;
    }

    this.loadingSignal.set(false);
  }

  setParkSearch(value: string): void {
    this.parkSearchSignal.set(value);
    this.loadParkOptions(value);
  }

  setSearch(value: string): void {
    this.searchSignal.set(value);
  }

  setFilter(value: AdminFieldModeFilter): void {
    this.filterSignal.set(value);
  }

  setProcessedFilter(value: AdminFieldModeProcessedFilter): void {
    this.processedFilterSignal.set(value);
  }

  selectPark(parkId: string | null): void {
    this.selectedParkIdSignal.set(parkId);
    this.selectedItemSignal.set(null);
    this.rowsSignal.set([]);
    this.parkSearchSignal.set('');
    this.localProcessedOverrides.clear();
    writeAdminFieldModeSelectedParkId(ADMIN_FIELD_MODE_SELECTED_PARK_STORAGE_KEY, parkId);
    if (parkId) {
      this.ensureSelectedParkAvailable(parkId);
      this.loadItems(parkId);
      return;
    }

    this.loadingSignal.set(false);
  }

  clearSelectedPark(): void {
    this.selectPark(null);
  }

  selectItem(itemId: string | null): void {
    if (!itemId) {
      this.selectedItemSignal.set(null);
      return;
    }

    const item: ParkItem | undefined = this.rowsSignal().find((row: AdminFieldModeItemRow) => row.item.id === itemId)?.item;
    this.selectedItemSignal.set(item ?? null);

    this.parkItemsApiService.getParkItemById(itemId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (loadedItem: ParkItem) => {
        this.selectedItemSignal.set(loadedItem);
        this.replaceRowItem(loadedItem);
      },
      error: () => undefined
    });
  }

  async toggleProcessed(row: AdminFieldModeItemRow): Promise<boolean> {
    return this.setProcessed(row, !row.isProcessed);
  }

  async setProcessed(row: AdminFieldModeItemRow, isProcessed: boolean): Promise<boolean> {
    const parkId: string | null = this.selectedParkIdSignal();
    const itemId: string | undefined = row.item.id;
    if (!parkId || !itemId) {
      return false;
    }

    const previousProcessed = row.isProcessed;
    this.localProcessedOverrides.set(itemId, isProcessed);
    this.setRowProcessed(itemId, isProcessed);

    try {
      const savedProcessed: boolean = await firstValueFrom(this.processedStatusService.setProcessed(parkId, itemId, isProcessed));
      this.localProcessedOverrides.set(itemId, savedProcessed);
      this.setRowProcessed(itemId, savedProcessed);
      this.messageKeySignal.set(savedProcessed ? 'admin.fieldMode.messages.itemProcessed' : 'admin.fieldMode.messages.itemUnprocessed');
      return true;
    } catch {
      this.localProcessedOverrides.set(itemId, previousProcessed);
      this.setRowProcessed(itemId, previousProcessed);
      this.messageKeySignal.set('admin.fieldMode.messages.itemProcessedFailed');
      return false;
    }
  }

  applySavedItem(item: ParkItem): void {
    if (!item.id) {
      return;
    }

    this.selectedItemSignal.set(item);
    this.replaceRowItem(item);
  }

  incrementPhotoCount(itemId: string, count: number): void {
    if (!itemId || count <= 0) {
      return;
    }

    this.rowsSignal.set(this.rowsSignal().map((row: AdminFieldModeItemRow) => {
      if (row.item.id !== itemId) {
        return row;
      }

      const currentCount: number = row.photoCount ?? 0;
      return {
        ...row,
        photoCount: currentCount + count
      };
    }));
  }

  setSelectedPhotoCategorySlug(slug: string): void {
    const known: boolean = ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS.some((option: AdminFieldModePhotoCategoryOption) => option.slug === slug);
    this.selectedPhotoCategorySlugSignal.set(known ? slug : ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS[0].slug);
  }

  setSelectedPhotoDescription(value: string): void {
    this.selectedPhotoDescriptionSignal.set(value);
  }

  setSelectedLocationKey(value: AdminFieldModeLocationKey): void {
    const known: boolean = ADMIN_FIELD_MODE_LOCATION_OPTIONS.some((option) => option.key === value);
    this.selectedLocationKeySignal.set(known ? value : 'general');
  }

  async capturePosition(): Promise<void> {
    await this.requestFreshPosition();
  }

  selectPhotoFile(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const file: File | null = input.files?.[0] ?? null;
    input.value = '';
    const validation = this.imageUploadSecurityService.validateImageFile(file);
    if (!validation.isValid || !file) {
      this.selectedPhotoFileSignal.set(null);
      this.messageKeySignal.set(validation.errorKey ?? 'admin.fieldMode.messages.invalidImage');
      return;
    }
    if (!this.gpsReady()) {
      this.selectedPhotoFileSignal.set(null);
      this.messageKeySignal.set('admin.fieldMode.messages.positionRequired');
      return;
    }
    this.selectedPhotoFileSignal.set(file);
    this.messageKeySignal.set(null);
  }

  private loadParkOptions(query: string): void {
    const normalizedQuery: string = query.trim();
    const requestId: number = ++this.parkSearchRequestId;
    const request$: Observable<{ data?: Park[] }> = normalizedQuery.length >= 2
      ? this.parksApiService.searchParks(normalizedQuery, 1, 30)
      : this.parksApiService.getParksPaginated(1, 30);

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { data?: Park[] }) => {
        if (requestId !== this.parkSearchRequestId) {
          return;
        }
        this.parksSignal.set(this.mergeWithSelectedPark(response.data ?? []));
      },
      error: () => {
        if (requestId === this.parkSearchRequestId) {
          this.parksSignal.set(this.mergeWithSelectedPark([]));
        }
      }
    });
  }

  private ensureSelectedParkAvailable(parkId: string): void {
    if (this.parksSignal().some((park: Park) => park.id === parkId)) {
      return;
    }

    this.parksApiService.getParkById(parkId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (park: Park) => this.parksSignal.set(this.mergeUniqueParks([park, ...this.parksSignal()])),
      error: () => undefined
    });
  }

  private mergeWithSelectedPark(parks: Park[]): Park[] {
    const selectedParkId: string | null = this.selectedParkIdSignal();
    const selectedPark: Park | undefined = selectedParkId
      ? this.parksSignal().find((park: Park) => park.id === selectedParkId)
      : undefined;

    return this.mergeUniqueParks(selectedPark ? [selectedPark, ...parks] : parks);
  }

  private mergeUniqueParks(parks: Park[]): Park[] {
    const seenIds = new Set<string>();
    return parks.filter((park: Park) => {
      if (!park.id || seenIds.has(park.id)) {
        return false;
      }
      seenIds.add(park.id);
      return true;
    });
  }

  private loadItems(parkId: string, routeItemId: string | null = null): void {
    const requestId: number = ++this.itemLoadRequestId;
    this.loadingSignal.set(true);
    this.getAdminRows(parkId).pipe(
      switchMap((adminRows: ParkItemAdminRow[]) => adminRows.length > 0
        ? this.buildRowsFromAdminRows(adminRows)
        : this.getFallbackItems(parkId).pipe(switchMap((items: ParkItem[]) => this.buildRowsFromItems(items)))),
      switchMap((rows: AdminFieldModeItemRow[]) => this.applyProcessedStatuses(parkId, rows))
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rows: AdminFieldModeItemRow[]) => {
        if (requestId !== this.itemLoadRequestId) {
          return;
        }
        this.rowsSignal.set(rows.sort((left: AdminFieldModeItemRow, right: AdminFieldModeItemRow) => left.item.name.localeCompare(right.item.name)));
        this.selectItem(routeItemId);
        this.loadingSignal.set(false);
      },
      error: () => {
        if (requestId !== this.itemLoadRequestId) {
          return;
        }
        this.rowsSignal.set([]);
        this.loadingSignal.set(false);
      }
    });
  }

  private getAdminRows(parkId: string): Observable<ParkItemAdminRow[]> {
    return this.parkItemsApiService.getParkItemsPaginated(1, 500, parkId, null, null, { sortBy: 'name', sortDirection: 'asc' }, { closedFilter: 'all' }).pipe(
      map((page) => page.data ?? []),
      catchError(() => of([]))
    );
  }

  private getFallbackItems(parkId: string): Observable<ParkItem[]> {
    return this.parkItemsApiService.getParkItemsByParkIdPage(parkId, 1, 500, { closedFilter: 'all' }, { closedFilter: 'all' }).pipe(
      map((page: PagedResult<ParkItem>) => page.items ?? []),
      switchMap((items: ParkItem[]) => items.length > 0
        ? of(items)
        : this.parkItemsApiService.getParkItemsByParkId(parkId, { closedFilter: 'all' })),
      catchError(() => this.parkItemsApiService.getParkItemsByParkId(parkId, { closedFilter: 'all' }).pipe(catchError(() => of([]))))
    );
  }

  private loadItemRouteContext(itemId: string): void {
    this.loadingSignal.set(true);
    this.parkItemsApiService.getParkItemById(itemId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (item: ParkItem) => {
        this.selectedParkIdSignal.set(item.parkId);
        writeAdminFieldModeSelectedParkId(ADMIN_FIELD_MODE_SELECTED_PARK_STORAGE_KEY, item.parkId);
        this.ensureSelectedParkAvailable(item.parkId);
        this.loadItems(item.parkId, item.id ?? itemId);
      },
      error: () => {
        this.loadingSignal.set(false);
        this.messageKeySignal.set('admin.fieldMode.messages.itemLoadFailed');
      }
    });
  }

  private buildRowsFromAdminRows(adminRows: ParkItemAdminRow[]): Observable<AdminFieldModeItemRow[]> {
    if (adminRows.length === 0) {
      return of([]);
    }

    return forkJoin(adminRows.map((adminRow: ParkItemAdminRow) => this.buildRowFromAdminRow(adminRow)));
  }

  private buildRowsFromItems(items: ParkItem[]): Observable<AdminFieldModeItemRow[]> {
    if (items.length === 0) {
      return of([]);
    }

    return forkJoin(items.map((item: ParkItem) => this.buildRow(item)));
  }

  private buildRowFromAdminRow(adminRow: ParkItemAdminRow): Observable<AdminFieldModeItemRow> {
    return this.parkItemsApiService.getParkItemById(adminRow.id).pipe(
      catchError(() => of(this.toFallbackParkItem(adminRow))),
      switchMap((item: ParkItem) => this.buildRow(item))
    );
  }

  private buildRow(item: ParkItem): Observable<AdminFieldModeItemRow> {
    if (!item.id) {
      return of(this.toRow(item, null, this.resolveProcessedState(item.id, false)));
    }
    return this.imagesApiService.getImagesPage(ImageOwnerType.PARK_ITEM, item.id, ImageCategory.PARK_ITEM, 1, 1).pipe(
      map((page: PagedResult<ImageDto>) => this.toRow(item, page.pagination.totalItems, this.resolveProcessedState(item.id, false))),
      catchError(() => of(this.toRow(item, null, this.resolveProcessedState(item.id, false))))
    );
  }

  private applyProcessedStatuses(parkId: string, rows: AdminFieldModeItemRow[]): Observable<AdminFieldModeItemRow[]> {
    return this.processedStatusService.getProcessedItemIds(parkId).pipe(
      map((processedIds: Set<string>) => rows.map((row: AdminFieldModeItemRow) => ({
        ...row,
        isProcessed: this.resolveProcessedState(row.item.id, !!row.item.id && processedIds.has(row.item.id))
      }))),
      catchError(() => of(rows.map((row: AdminFieldModeItemRow) => ({
        ...row,
        isProcessed: this.resolveProcessedState(row.item.id, this.getCurrentRowProcessedState(row.item.id) ?? row.isProcessed)
      }))))
    );
  }

  private toFallbackParkItem(adminRow: ParkItemAdminRow): ParkItem {
    return {
      id: adminRow.id,
      parkId: adminRow.parkId,
      zoneId: adminRow.zoneId ?? null,
      name: adminRow.name,
      category: adminRow.category,
      type: adminRow.type,
      latitude: null,
      longitude: null,
      descriptions: [],
      isVisible: adminRow.isVisible,
      adminReviewStatus: adminRow.adminReviewStatus
    };
  }

  private replaceRowItem(item: ParkItem): void {
    this.rowsSignal.set(this.rowsSignal().map((row: AdminFieldModeItemRow) => row.item.id === item.id
      ? this.toRow(item, row.photoCount, this.resolveProcessedState(item.id, row.isProcessed))
      : row));
  }

  private setRowProcessed(itemId: string, isProcessed: boolean): void {
    this.rowsSignal.set(this.rowsSignal().map((row: AdminFieldModeItemRow) => row.item.id === itemId
      ? { ...row, isProcessed }
      : row));
  }

  private resolveProcessedState(itemId: string | null | undefined, fallback: boolean): boolean {
    if (!itemId || !this.localProcessedOverrides.has(itemId)) {
      return fallback;
    }

    return this.localProcessedOverrides.get(itemId) ?? fallback;
  }

  private getCurrentRowProcessedState(itemId: string | null | undefined): boolean | null {
    if (!itemId) {
      return null;
    }

    return this.rowsSignal().find((row: AdminFieldModeItemRow) => row.item.id === itemId)?.isProcessed ?? null;
  }

  private toRow(item: ParkItem, photoCount: number | null, isProcessed: boolean): AdminFieldModeItemRow {
    const preciseLocationCount: number = this.countPreciseLocations(item);
    return { item, photoCount, hasGeneralLocation: item.latitude !== null && item.latitude !== undefined && item.longitude !== null && item.longitude !== undefined, preciseLocationCount, hasAnyPreciseLocation: preciseLocationCount > 0, isProcessed };
  }

  private countPreciseLocations(item: ParkItem): number {
    const locations: AttractionLocations | null | undefined = item.attractionLocations;
    const points: Array<AttractionLocationPoint | null | undefined> = [locations?.entrance, locations?.exit, locations?.fastPassEntrance, locations?.reducedMobilityEntrance];
    return points.filter((point: AttractionLocationPoint | null | undefined) => point?.latitude !== null && point?.latitude !== undefined && point?.longitude !== null && point?.longitude !== undefined).length;
  }

  private filterRows(): AdminFieldModeItemRow[] {
    const searchTerm: string = this.searchSignal().trim().toLowerCase();
    const processedFilter: AdminFieldModeProcessedFilter = this.processedFilterSignal();
    return this.rowsSignal().filter((row: AdminFieldModeItemRow) => {
      if (processedFilter === 'processed' && !row.isProcessed) {
        return false;
      }
      if (processedFilter === 'unprocessed' && row.isProcessed) {
        return false;
      }
      if (searchTerm && !row.item.name.toLowerCase().includes(searchTerm)) {
        return false;
      }
      if (this.filterSignal() === 'missingPhotos') {
        return (row.photoCount ?? 0) === 0;
      }
      if (this.filterSignal() === 'missingGeneralLocation') {
        return !row.hasGeneralLocation;
      }
      if (this.filterSignal() === 'missingPreciseLocation') {
        return !row.hasAnyPreciseLocation;
      }
      return true;
    });
  }

  private async requestFreshPosition(): Promise<AdminFieldModePosition> {
    this.gpsStatusSignal.set('checking');
    this.gpsErrorSignal.set(null);
    try {
      const position: GeolocationPosition = await this.geolocationService.getCurrentPosition({ enableHighAccuracy: true, maximumAge: 0, timeout: 15000 });
      const currentPosition: AdminFieldModePosition = { latitude: position.coords.latitude, longitude: position.coords.longitude, accuracy: Number.isFinite(position.coords.accuracy) ? position.coords.accuracy : null, capturedAt: Date.now() };
      this.currentPositionSignal.set(currentPosition);
      this.gpsStatusSignal.set('ready');
      return currentPosition;
    } catch (error: unknown) {
      this.gpsStatusSignal.set('error');
      this.gpsErrorSignal.set('admin.fieldMode.messages.positionUnavailable');
      throw error;
    }
  }

  private isPositionFresh(position: AdminFieldModePosition | null): boolean {
    return !!position && Date.now() - position.capturedAt <= ADMIN_FIELD_MODE_GPS_MAX_AGE_MS;
  }
}
