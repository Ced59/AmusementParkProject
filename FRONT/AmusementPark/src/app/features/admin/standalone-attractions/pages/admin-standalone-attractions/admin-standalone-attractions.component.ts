import { CommonModule } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AdminReviewStatus, ADMIN_REVIEW_STATUSES } from '@app/models/admin/admin-review-status';
import { Park } from '@app/models/parks/park';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { StandaloneAttraction, StandaloneAttractionMigrationRequest } from '@app/models/standalone-attractions/standalone-attraction';
import { ParkAdminListFilters } from '@data-access/parks/parks-api-endpoints';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { StandaloneAttractionListFilters, StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';
import { PaginationContract } from '@shared/models/contracts';
import { extractSafeDisplayErrorMessage } from '@shared/utils/security/error-display.helpers';

type StandaloneAttractionSortField = 'created' | 'updated' | 'name' | 'type' | 'countryCode' | 'isVisible' | 'adminReviewStatus';
type StandaloneAttractionSortDirection = 'asc' | 'desc';

interface StandaloneAttractionsAdminCopy {
  title: string;
  subtitle: string;
  total: string;
  search: string;
  searchPlaceholder: string;
  country: string;
  type: string;
  visibility: string;
  review: string;
  all: string;
  visible: string;
  hidden: string;
  apply: string;
  clear: string;
  newAttraction: string;
  save: string;
  exportJson: string;
  migrate: string;
  migrationTitle: string;
  legacyParkSearch: string;
  legacyParkSearchPlaceholder: string;
  legacyParkSearchHint: string;
  searchLegacyParks: string;
  searchLegacyFromFilters: string;
  legacyParkResultCount: string;
  legacyParkEmpty: string;
  legacyParkItems: string;
  selectedLegacyPark: string;
  legacyParkLoaded: string;
  legacyParkId: string;
  legacyParkItemId: string;
  targetStandaloneAttractionId: string;
  retireLegacyPark: string;
  retireLegacyParkItem: string;
  name: string;
  city: string;
  street: string;
  postalCode: string;
  website: string;
  operatorId: string;
  subtype: string;
  latitude: string;
  longitude: string;
  model: string;
  manufacturerId: string;
  status: string;
  lengthMeters: string;
  speedKmh: string;
  durationSeconds: string;
  descriptionFr: string;
  descriptionEn: string;
  legacy: string;
  selected: string;
  bulkVisible: string;
  bulkHidden: string;
  bulkValidated: string;
  noSelection: string;
  empty: string;
  loading: string;
  saved: string;
  migrated: string;
  bulkUpdated: string;
  missingLegacyParkSearch: string;
  missingMigrationPark: string;
  missingName: string;
  actionFailed: string;
}

const COPY: Record<string, StandaloneAttractionsAdminCopy> = {
  fr: {
    title: 'Attractions isolées',
    subtitle: 'Gère les attractions fixes sans parc parent artificiel et migre les anciennes fiches mono-attraction.',
    total: 'fiches',
    search: 'Recherche',
    searchPlaceholder: 'Nom, ville, modèle...',
    country: 'Pays',
    type: 'Type',
    visibility: 'Visibilité',
    review: 'Revue',
    all: 'Tous',
    visible: 'Visible',
    hidden: 'Masqué',
    apply: 'Appliquer',
    clear: 'Réinitialiser',
    newAttraction: 'Nouvelle attraction',
    save: 'Enregistrer',
    exportJson: 'Exporter JSON',
    migrate: 'Migrer',
    migrationTitle: 'Migration depuis un parc legacy',
    legacyParkSearch: 'Parc legacy à migrer',
    legacyParkSearchPlaceholder: 'Bardonecchia ou ID du parc legacy',
    legacyParkSearchHint: 'Cherche l’ancienne fiche parc, puis sélectionne-la pour remplir les champs de migration.',
    searchLegacyParks: 'Chercher les parcs',
    searchLegacyFromFilters: 'Chercher côté parcs legacy',
    legacyParkResultCount: 'parcs trouvés',
    legacyParkEmpty: 'Aucun parc legacy trouvé.',
    legacyParkItems: 'items',
    selectedLegacyPark: 'Parc sélectionné',
    legacyParkLoaded: 'Parc legacy chargé pour migration.',
    legacyParkId: 'ID parc legacy',
    legacyParkItemId: 'ID attraction legacy',
    targetStandaloneAttractionId: 'ID fiche autonome cible',
    retireLegacyPark: 'Retirer le parc legacy',
    retireLegacyParkItem: 'Retirer l’attraction legacy',
    name: 'Nom',
    city: 'Ville',
    street: 'Adresse',
    postalCode: 'Code postal',
    website: 'Site officiel',
    operatorId: 'ID exploitant',
    subtype: 'Sous-type',
    latitude: 'Latitude',
    longitude: 'Longitude',
    model: 'Modèle',
    manufacturerId: 'ID constructeur',
    status: 'Statut technique',
    lengthMeters: 'Longueur (m)',
    speedKmh: 'Vitesse (km/h)',
    durationSeconds: 'Durée (s)',
    descriptionFr: 'Description FR',
    descriptionEn: 'Description EN',
    legacy: 'Legacy',
    selected: 'sélectionnées',
    bulkVisible: 'Rendre visible',
    bulkHidden: 'Masquer',
    bulkValidated: 'Valider',
    noSelection: 'Sélectionne une fiche ou crée-en une.',
    empty: 'Aucune attraction isolée ne correspond aux filtres.',
    loading: 'Chargement...',
    saved: 'Fiche enregistrée.',
    migrated: 'Migration terminée.',
    bulkUpdated: 'Mise à jour groupée terminée.',
    missingLegacyParkSearch: 'Saisis un nom ou un ID de parc legacy à rechercher.',
    missingMigrationPark: 'L’ID du parc legacy est obligatoire.',
    missingName: 'Le nom est obligatoire.',
    actionFailed: 'Action impossible. Vérifie les champs et réessaie.'
  },
  en: {
    title: 'Standalone attractions',
    subtitle: 'Manage fixed attractions without an artificial parent park and migrate older single-attraction park records.',
    total: 'records',
    search: 'Search',
    searchPlaceholder: 'Name, city, model...',
    country: 'Country',
    type: 'Type',
    visibility: 'Visibility',
    review: 'Review',
    all: 'All',
    visible: 'Visible',
    hidden: 'Hidden',
    apply: 'Apply',
    clear: 'Reset',
    newAttraction: 'New attraction',
    save: 'Save',
    exportJson: 'Export JSON',
    migrate: 'Migrate',
    migrationTitle: 'Migration from a legacy park',
    legacyParkSearch: 'Legacy park to migrate',
    legacyParkSearchPlaceholder: 'Bardonecchia or legacy park ID',
    legacyParkSearchHint: 'Search the old park record, then select it to fill the migration fields.',
    searchLegacyParks: 'Search parks',
    searchLegacyFromFilters: 'Search legacy parks',
    legacyParkResultCount: 'parks found',
    legacyParkEmpty: 'No legacy park found.',
    legacyParkItems: 'items',
    selectedLegacyPark: 'Selected park',
    legacyParkLoaded: 'Legacy park loaded for migration.',
    legacyParkId: 'Legacy park ID',
    legacyParkItemId: 'Legacy attraction ID',
    targetStandaloneAttractionId: 'Target standalone ID',
    retireLegacyPark: 'Retire legacy park',
    retireLegacyParkItem: 'Retire legacy attraction',
    name: 'Name',
    city: 'City',
    street: 'Address',
    postalCode: 'Postal code',
    website: 'Official website',
    operatorId: 'Operator ID',
    subtype: 'Subtype',
    latitude: 'Latitude',
    longitude: 'Longitude',
    model: 'Model',
    manufacturerId: 'Manufacturer ID',
    status: 'Technical status',
    lengthMeters: 'Length (m)',
    speedKmh: 'Speed (km/h)',
    durationSeconds: 'Duration (s)',
    descriptionFr: 'French description',
    descriptionEn: 'English description',
    legacy: 'Legacy',
    selected: 'selected',
    bulkVisible: 'Make visible',
    bulkHidden: 'Hide',
    bulkValidated: 'Validate',
    noSelection: 'Select a record or create one.',
    empty: 'No standalone attraction matches the filters.',
    loading: 'Loading...',
    saved: 'Record saved.',
    migrated: 'Migration completed.',
    bulkUpdated: 'Bulk update completed.',
    missingLegacyParkSearch: 'Enter a legacy park name or ID to search.',
    missingMigrationPark: 'The legacy park ID is required.',
    missingName: 'The name is required.',
    actionFailed: 'Action failed. Check the fields and try again.'
  }
};

@Component({
  selector: 'app-admin-standalone-attractions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-standalone-attractions.component.html',
  styleUrl: './admin-standalone-attractions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminStandaloneAttractionsComponent implements OnInit {
  protected readonly adminReviewStatuses: readonly AdminReviewStatus[] = ADMIN_REVIEW_STATUSES;
  protected readonly attractionTypes: readonly ParkItemType[] = ['Attraction', 'RollerCoaster', 'WaterRide', 'FlatRide', 'FamilyRide', 'ThrillRide', 'TransportRide', 'WalkThrough', 'DropTower', 'Other'];
  protected readonly sortFields: readonly StandaloneAttractionSortField[] = ['updated', 'created', 'name', 'type', 'countryCode', 'isVisible', 'adminReviewStatus'];
  protected readonly pageSizes: readonly number[] = [20, 40, 80];

  protected readonly rows = signal<StandaloneAttraction[]>([]);
  protected readonly loading = signal<boolean>(false);
  protected readonly legacyParkLoading = signal<boolean>(false);
  protected readonly hasSearchedLegacyParks = signal<boolean>(false);
  protected readonly legacyParkRows = signal<Park[]>([]);
  protected readonly selectedLegacyPark = signal<Park | null>(null);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly selectedIds = signal<Set<string>>(new Set<string>());
  protected readonly selected = signal<StandaloneAttraction | null>(null);
  protected readonly draft = signal<StandaloneAttraction>(this.createEmptyAttraction());
  protected readonly isExporting = signal<boolean>(false);
  protected readonly pagination = signal<PaginationContract>({
    currentPage: 1,
    itemsPerPage: 20,
    totalItems: 0,
    totalPages: 1
  });
  protected readonly selectedCount = computed<number>(() => this.selectedIds().size);
  protected readonly canExport = computed<boolean>(() => !!this.draft().id && !this.isExporting());

  protected search: string = '';
  protected countryCode: string = '';
  protected isVisibleFilter: '' | 'true' | 'false' = '';
  protected reviewStatusFilter: '' | AdminReviewStatus = '';
  protected typeFilter: '' | ParkItemType = '';
  protected sortBy: StandaloneAttractionSortField = 'updated';
  protected sortDirection: StandaloneAttractionSortDirection = 'desc';
  protected pageSize: number = 20;

  protected migrationLegacyParkId: string = '';
  protected migrationLegacyParkItemId: string = '';
  protected migrationTargetStandaloneAttractionId: string = '';
  protected migrationRetireLegacyPark: boolean = true;
  protected migrationRetireLegacyParkItem: boolean = true;
  protected legacyParkSearch: string = '';

  constructor(
    private readonly apiService: StandaloneAttractionsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    void this.load(1);
  }

  protected get currentLang(): string {
    return this.router.url.split('/')[1] || 'en';
  }

  protected t(key: keyof StandaloneAttractionsAdminCopy): string {
    const language: string = this.currentLang === 'fr' ? 'fr' : 'en';
    return COPY[language][key];
  }

  protected async load(page: number = this.pagination().currentPage || 1): Promise<void> {
    this.loading.set(true);
    this.error.set(null);

    try {
      const result = await firstValueFrom(this.apiService.getAdminPage(page, this.pageSize, this.buildFilters()));
      this.rows.set(result.items);
      this.pagination.set(result.pagination);
      this.syncSelectedAfterReload(result.items);
    } catch (error: unknown) {
      this.setErrorFromUnknown(error);
    } finally {
      this.loading.set(false);
    }
  }

  protected async applyFilters(): Promise<void> {
    const trimmedSearch: string = this.search.trim();

    if (trimmedSearch && !this.legacyParkSearch.trim()) {
      this.legacyParkSearch = trimmedSearch;
    }

    await this.load(1);
  }

  protected async clearFilters(): Promise<void> {
    this.search = '';
    this.countryCode = '';
    this.isVisibleFilter = '';
    this.reviewStatusFilter = '';
    this.typeFilter = '';
    this.sortBy = 'updated';
    this.sortDirection = 'desc';
    await this.load(1);
  }

  protected select(row: StandaloneAttraction): void {
    this.selected.set(row);
    this.draft.set(this.cloneAttraction(row));
    this.syncMigrationTargetFromDraft(row);
    this.message.set(null);
    this.error.set(null);
  }

  protected newAttraction(): void {
    const empty: StandaloneAttraction = this.createEmptyAttraction(true);
    this.selected.set(null);
    this.draft.set(empty);
    this.syncMigrationTargetFromDraft(null);
    this.message.set(null);
    this.error.set(null);
  }

  protected patchDraft(patch: Partial<StandaloneAttraction>): void {
    this.draft.update((current: StandaloneAttraction) => ({
      ...current,
      ...patch
    }));
  }

  protected patchDetails(patch: Partial<NonNullable<StandaloneAttraction['attractionDetails']>>): void {
    this.draft.update((current: StandaloneAttraction) => ({
      ...current,
      attractionDetails: {
        ...(current.attractionDetails ?? {}),
        ...patch
      }
    }));
  }

  protected updateDescription(languageCode: 'fr' | 'en', value: string): void {
    const normalizedValue: string = value.trim();

    this.draft.update((current: StandaloneAttraction) => {
      const descriptions = (current.descriptions ?? []).filter((description) => description.languageCode !== languageCode);

      if (normalizedValue.length > 0) {
        descriptions.push({ languageCode, value: normalizedValue });
      }

      return {
        ...current,
        descriptions
      };
    });
  }

  protected getDescription(languageCode: 'fr' | 'en'): string {
    return this.draft().descriptions?.find((description) => description.languageCode === languageCode)?.value ?? '';
  }

  protected async saveDraft(): Promise<void> {
    const current: StandaloneAttraction = this.draft();

    if (!current.name.trim()) {
      this.error.set(this.t('missingName'));
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const saved: StandaloneAttraction = current.id
        ? await firstValueFrom(this.apiService.update(current.id, current))
        : await firstValueFrom(this.apiService.create(current));

      this.selected.set(saved);
      this.draft.set(this.cloneAttraction(saved));
      this.syncMigrationTargetFromDraft(saved);
      this.message.set(this.t('saved'));
      this.alignFiltersWithSavedAttraction(saved);
      await this.load(1);
    } catch (error: unknown) {
      this.setErrorFromUnknown(error);
    } finally {
      this.loading.set(false);
    }
  }

  protected async searchLegacyParksFromFilters(): Promise<void> {
    const trimmedSearch: string = this.search.trim();

    if (trimmedSearch) {
      this.legacyParkSearch = trimmedSearch;
    }

    await this.searchLegacyParks();
  }

  protected async searchLegacyParks(): Promise<void> {
    const query: string = this.legacyParkSearch.trim() || this.search.trim();

    if (!query) {
      this.error.set(this.t('missingLegacyParkSearch'));
      return;
    }

    this.legacyParkSearch = query;
    this.legacyParkLoading.set(true);
    this.hasSearchedLegacyParks.set(true);
    this.error.set(null);
    this.message.set(null);

    try {
      const parks: Park[] = await this.loadLegacyParks(query);
      this.legacyParkRows.set(parks);

      if (parks.length === 0) {
        this.selectedLegacyPark.set(null);
        return;
      }

      if (parks.length === 1) {
        this.selectLegacyParkForMigration(parks[0], false);
        this.message.set(this.t('legacyParkLoaded'));
      }
    } catch (error: unknown) {
      this.setErrorFromUnknown(error);
    } finally {
      this.legacyParkLoading.set(false);
    }
  }

  protected selectLegacyParkForMigration(park: Park, showMessage: boolean = true): void {
    if (!park.id) {
      return;
    }

    this.selectedLegacyPark.set(park);
    this.migrationLegacyParkId = park.id;

    const currentDraftId: string | null | undefined = this.draft().id;
    if (currentDraftId) {
      this.migrationTargetStandaloneAttractionId = currentDraftId;
    }

    this.seedDraftFromLegacyPark(park);
    this.error.set(null);

    if (showMessage) {
      this.message.set(`${this.t('selectedLegacyPark')}: ${park.name ?? park.id}`);
    }
  }

  protected async migrateFromPark(): Promise<void> {
    const legacyParkId: string = this.migrationLegacyParkId.trim();

    if (!legacyParkId) {
      this.error.set(this.t('missingMigrationPark'));
      return;
    }

    const request: StandaloneAttractionMigrationRequest = {
      legacyParkId,
      legacyParkItemId: this.migrationLegacyParkItemId.trim() || null,
      targetStandaloneAttractionId: this.migrationTargetStandaloneAttractionId.trim() || null,
      retireLegacyPark: this.migrationRetireLegacyPark,
      retireLegacyParkItem: this.migrationRetireLegacyParkItem
    };

    this.loading.set(true);
    this.error.set(null);

    try {
      const migrated: StandaloneAttraction = await firstValueFrom(this.apiService.migrateFromPark(request));
      this.selected.set(migrated);
      this.draft.set(this.cloneAttraction(migrated));
      this.message.set(this.t('migrated'));
      this.alignFiltersWithSavedAttraction(migrated);
      await this.load(1);
    } catch (error: unknown) {
      this.setErrorFromUnknown(error);
    } finally {
      this.loading.set(false);
    }
  }

  protected async bulkSetVisible(isVisible: boolean): Promise<void> {
    await this.runBulkUpdate({ isVisible });
  }

  protected async bulkValidate(): Promise<void> {
    await this.runBulkUpdate({ adminReviewStatus: 'Validated' });
  }

  protected toggleSelected(id: string | null | undefined, checked: boolean): void {
    if (!id) {
      return;
    }

    this.selectedIds.update((current: Set<string>) => {
      const next: Set<string> = new Set<string>(current);

      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }

      return next;
    });
  }

  protected toggleAll(checked: boolean): void {
    if (!checked) {
      this.selectedIds.set(new Set<string>());
      return;
    }

    this.selectedIds.set(new Set<string>(this.rows().map((row: StandaloneAttraction) => row.id).filter((id: string | null | undefined): id is string => !!id)));
  }

  protected isSelected(id: string | null | undefined): boolean {
    return !!id && this.selectedIds().has(id);
  }

  protected isAllPageSelected(): boolean {
    const ids: string[] = this.rows().map((row: StandaloneAttraction) => row.id).filter((id: string | null | undefined): id is string => !!id);
    return ids.length > 0 && ids.every((id: string) => this.selectedIds().has(id));
  }

  protected async changePage(page: number): Promise<void> {
    const totalPages: number = this.pagination().totalPages || 1;
    const targetPage: number = Math.min(Math.max(page, 1), totalPages);
    await this.load(targetPage);
  }

  protected async changePageSize(value: string | number): Promise<void> {
    this.pageSize = Number(value) || 20;
    await this.load(1);
  }

  protected async exportDraft(): Promise<void> {
    const id: string | null | undefined = this.draft().id;

    if (!id) {
      return;
    }

    this.isExporting.set(true);
    this.error.set(null);

    try {
      const response: HttpResponse<Blob> = await firstValueFrom(this.apiService.downloadExport(id));
      if (!response.body) {
        this.error.set(this.t('actionFailed'));
        return;
      }

      this.downloadBlob(response.body, this.resolveDownloadFileName(response));
    } catch (error: unknown) {
      this.setErrorFromUnknown(error);
    } finally {
      this.isExporting.set(false);
    }
  }

  protected toNullableNumber(value: string | number | null | undefined): number | null {
    if (value === null || value === undefined || String(value).trim() === '') {
      return null;
    }

    const numericValue: number = Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  protected trackById(_: number, item: StandaloneAttraction): string {
    return item.id ?? item.name;
  }

  protected trackByParkId(_: number, park: Park): string {
    return park.id ?? park.name ?? `${park.latitude}:${park.longitude}`;
  }

  protected formatLegacyParkSecondary(park: Park): string {
    const parts: string[] = [
      park.countryCode?.trim().toUpperCase() || null,
      park.city?.trim() || null,
      park.type?.trim() || null
    ].filter((part: string | null): part is string => !!part);

    return parts.length > 0 ? parts.join(' · ') : '-';
  }

  protected formatLegacyParkCounts(park: Park): string {
    const totalCount: number | null = park.parkItemsTotalCount ?? null;

    if (totalCount === null) {
      return '-';
    }

    const visibleCount: number = park.parkItemsVisibleCount ?? 0;
    return `${visibleCount}/${totalCount} ${this.t('legacyParkItems')}`;
  }

  private async runBulkUpdate(patch: { isVisible?: boolean; adminReviewStatus?: AdminReviewStatus }): Promise<void> {
    const ids: string[] = Array.from(this.selectedIds());

    if (ids.length === 0) {
      this.error.set(this.t('noSelection'));
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      await firstValueFrom(this.apiService.updateBulkAdministration({
        ids,
        isVisible: patch.isVisible ?? null,
        adminReviewStatus: patch.adminReviewStatus ?? null
      }));
      this.message.set(this.t('bulkUpdated'));
      await this.load(this.pagination().currentPage || 1);
    } catch (error: unknown) {
      this.setErrorFromUnknown(error);
    } finally {
      this.loading.set(false);
    }
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    if (typeof document === 'undefined' || typeof URL === 'undefined') {
      this.error.set(this.t('actionFailed'));
      return;
    }

    const objectUrl: string = URL.createObjectURL(blob);
    const anchor: HTMLAnchorElement = document.createElement('a');
    anchor.href = objectUrl;
    anchor.download = fileName;
    anchor.rel = 'noopener';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(objectUrl);
  }

  private resolveDownloadFileName(response: HttpResponse<Blob>): string {
    const contentDisposition: string = response.headers.get('content-disposition') ?? '';
    const utf8Match: RegExpMatchArray | null = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1].replace(/"/g, ''));
    }

    const fallbackMatch: RegExpMatchArray | null = contentDisposition.match(/filename="?([^";]+)"?/i);
    if (fallbackMatch?.[1]) {
      return fallbackMatch[1];
    }

    return 'standalone-attraction-upsert.json';
  }

  private buildFilters(): StandaloneAttractionListFilters {
    return {
      search: this.search,
      isVisible: this.isVisibleFilter === '' ? null : this.isVisibleFilter === 'true',
      adminReviewStatus: this.reviewStatusFilter || null,
      type: this.typeFilter || null,
      countryCode: this.countryCode,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    };
  }

  private async loadLegacyParks(query: string): Promise<Park[]> {
    if (this.isLikelyIdentifier(query)) {
      const park: Park = await firstValueFrom(this.parksApiService.getParkById(query));
      return [park];
    }

    const response: ParksApiResponse = await firstValueFrom(this.parksApiService.searchParks(
      query,
      1,
      10,
      false,
      null,
      this.buildLegacyParkFilters(),
      {
        closedFilter: 'all',
        sort: { sortBy: 'name', sortDirection: 'asc' }
      }
    ));

    return response.data ?? [];
  }

  private buildLegacyParkFilters(): ParkAdminListFilters | null {
    const countryCode: string = this.countryCode.trim().toUpperCase();

    return countryCode
      ? {
          isVisible: null,
          adminReviewStatus: null,
          type: null,
          audienceClassification: null,
          countryCode,
          hasValidCoordinates: null,
          openingHoursStatus: 'all'
        }
      : null;
  }

  private isLikelyIdentifier(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.trim());
  }

  private setErrorFromUnknown(error: unknown): void {
    this.error.set(extractSafeDisplayErrorMessage(error, this.t('actionFailed')));
  }

  private syncSelectedAfterReload(rows: StandaloneAttraction[]): void {
    const currentId: string | null | undefined = this.selected()?.id;

    if (!currentId) {
      return;
    }

    const refreshed: StandaloneAttraction | undefined = rows.find((row: StandaloneAttraction) => row.id === currentId);

    if (refreshed) {
      this.selected.set(refreshed);
      this.draft.set(this.cloneAttraction(refreshed));
    }
  }

  private syncMigrationTargetFromDraft(attraction: StandaloneAttraction | null): void {
    this.migrationTargetStandaloneAttractionId = attraction?.id ?? '';
  }

  private alignFiltersWithSavedAttraction(attraction: StandaloneAttraction): void {
    const savedCountryCode: string = attraction.countryCode?.trim().toUpperCase() ?? '';
    const currentCountryCode: string = this.countryCode.trim().toUpperCase();

    if (currentCountryCode && currentCountryCode !== savedCountryCode) {
      this.countryCode = savedCountryCode;
    }

    if (this.typeFilter && this.typeFilter !== attraction.type) {
      this.typeFilter = attraction.type;
    }

    if (this.isVisibleFilter && (this.isVisibleFilter === 'true') !== attraction.isVisible) {
      this.isVisibleFilter = '';
    }

    if (this.reviewStatusFilter && this.reviewStatusFilter !== attraction.adminReviewStatus) {
      this.reviewStatusFilter = attraction.adminReviewStatus;
    }

    const normalizedSearch: string = this.search.trim().toLowerCase();
    if (normalizedSearch && !this.savedAttractionMatchesSearch(attraction, normalizedSearch)) {
      this.search = attraction.name;
    }

    this.sortBy = 'updated';
    this.sortDirection = 'desc';
  }

  private savedAttractionMatchesSearch(attraction: StandaloneAttraction, normalizedSearch: string): boolean {
    const searchableValues: string[] = [
      attraction.name,
      attraction.city ?? '',
      attraction.subtype ?? '',
      attraction.attractionDetails?.model ?? '',
      ...(attraction.descriptions ?? []).map((description) => description.value)
    ];

    return searchableValues.some((value: string) => value.toLowerCase().includes(normalizedSearch));
  }

  private seedDraftFromLegacyPark(park: Park): void {
    this.draft.update((current: StandaloneAttraction) => ({
      ...current,
      name: current.name.trim() || park.name || '',
      countryCode: current.countryCode?.trim() || park.countryCode?.trim().toUpperCase() || null,
      city: current.city?.trim() || park.city?.trim() || null,
      latitude: current.latitude ?? park.latitude ?? null,
      longitude: current.longitude ?? park.longitude ?? null,
      legacyParkId: current.legacyParkId?.trim() || park.id || null
    }));
  }

  private createEmptyAttraction(seedFromFilters: boolean = false): StandaloneAttraction {
    const seededCountryCode: string | null = seedFromFilters
      ? this.countryCode?.trim().toUpperCase() || null
      : null;
    const seededName: string = seedFromFilters ? this.search?.trim() || '' : '';
    const seededType: ParkItemType = seedFromFilters && this.typeFilter ? this.typeFilter : 'Attraction';

    return {
      name: seededName,
      countryCode: seededCountryCode,
      type: seededType,
      subtype: null,
      operatorId: null,
      websiteUrl: null,
      street: null,
      city: null,
      postalCode: null,
      latitude: null,
      longitude: null,
      descriptions: [],
      attractionDetails: {},
      attractionLocations: null,
      isVisible: false,
      adminReviewStatus: 'ToReview',
      legacyParkId: null,
      legacyParkItemId: null
    };
  }

  private cloneAttraction(value: StandaloneAttraction): StandaloneAttraction {
    return {
      ...value,
      descriptions: [...(value.descriptions ?? [])],
      attractionDetails: value.attractionDetails ? { ...value.attractionDetails } : {},
      attractionLocations: value.attractionLocations ? { ...value.attractionLocations } : null
    };
  }
}
