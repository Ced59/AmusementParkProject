import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AdminReviewStatus, ADMIN_REVIEW_STATUSES } from '@app/models/admin/admin-review-status';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { StandaloneAttraction, StandaloneAttractionMigrationRequest } from '@app/models/standalone-attractions/standalone-attraction';
import { StandaloneAttractionListFilters, StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';
import { PaginationContract } from '@shared/models/contracts';

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
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly selectedIds = signal<Set<string>>(new Set<string>());
  protected readonly selected = signal<StandaloneAttraction | null>(null);
  protected readonly draft = signal<StandaloneAttraction>(this.createEmptyAttraction());
  protected readonly pagination = signal<PaginationContract>({
    currentPage: 1,
    itemsPerPage: 20,
    totalItems: 0,
    totalPages: 1
  });
  protected readonly selectedCount = computed<number>(() => this.selectedIds().size);
  protected readonly canExport = computed<boolean>(() => !!this.draft().id);

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

  constructor(
    private readonly apiService: StandaloneAttractionsApiService,
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
      const result = await firstValueFrom(this.apiService.getPage(page, this.pageSize, this.buildFilters()));
      this.rows.set(result.items);
      this.pagination.set(result.pagination);
      this.syncSelectedAfterReload(result.items);
    } catch {
      this.error.set(this.t('actionFailed'));
    } finally {
      this.loading.set(false);
    }
  }

  protected async applyFilters(): Promise<void> {
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
    this.message.set(null);
    this.error.set(null);
  }

  protected newAttraction(): void {
    const empty: StandaloneAttraction = this.createEmptyAttraction();
    this.selected.set(null);
    this.draft.set(empty);
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
      this.message.set(this.t('saved'));
      await this.load(this.pagination().currentPage || 1);
    } catch {
      this.error.set(this.t('actionFailed'));
    } finally {
      this.loading.set(false);
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
      await this.load(1);
    } catch {
      this.error.set(this.t('actionFailed'));
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

  protected exportDraft(): void {
    const id: string | null | undefined = this.draft().id;

    if (!id || typeof window === 'undefined') {
      return;
    }

    window.open(this.apiService.buildExportUrl(id), '_blank', 'noopener');
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
    } catch {
      this.error.set(this.t('actionFailed'));
    } finally {
      this.loading.set(false);
    }
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

  private createEmptyAttraction(): StandaloneAttraction {
    return {
      name: '',
      countryCode: null,
      type: 'Attraction',
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
