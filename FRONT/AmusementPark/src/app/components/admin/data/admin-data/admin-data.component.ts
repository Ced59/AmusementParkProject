import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
  computed,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, interval, of, Subscription } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Checkbox } from 'primeng/checkbox';
import { ProgressBar } from 'primeng/progressbar';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { TabsModule } from 'primeng/tabs';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';

import { DataSourcesApiService } from '@data-access/admin/data-sources-api.service';
import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterDuplicateResolutionRequest,
  CaptainCoasterExternalVariantResponse,
  CaptainCoasterFieldResolutionRequest,
  CaptainCoasterSessionResponse,
  CaptainCoasterSettingsResponse,
  CaptainCoasterStatusResponse,
  ComparisonFilters,
  DataSourceSummary,
  StartCaptainCoasterImportRequest,
  UpdateCaptainCoasterSettingsRequest
} from '@app/models/admin/data/data-management.models';

interface DuplicateResolutionState {
  strategy: 'SelectVariant' | 'Merge';
  selectedExternalVariantId: string | null;
  fieldSelections: Record<string, string>;
}

@Component({
  selector: 'app-admin-data',
  templateUrl: './admin-data.component.html',
  styleUrl: './admin-data.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    ButtonDirective,
    Card,
    Checkbox,
    ProgressBar,
    SelectModule,
    TableModule,
    Tag,
    TabsModule,
    PaginationComponent
  ]
})
export class AdminDataComponent implements OnInit, OnDestroy {
  protected readonly dataSources = signal<DataSourceSummary[]>([]);
  protected readonly selectedSourceKey = signal<string | null>(null);

  protected readonly ccStatus = signal<CaptainCoasterStatusResponse | null>(null);
  protected readonly ccSettings = signal<CaptainCoasterSettingsResponse | null>(null);
  protected readonly ccSession = signal<CaptainCoasterSessionResponse | null>(null);
  protected readonly ccIsBusy = signal<boolean>(false);
  protected readonly ccErrorMessage = signal<string>('');
  protected readonly ccSuccessMessage = signal<string>('');

  protected readonly ccImportKind = signal<'sitemap' | 'manual-urls'>('sitemap');
  protected readonly ccManualUrlsText = signal<string>('');
  protected readonly ccStartAtStep = signal<string>('DiscoverUrls');
  protected readonly ccResumeLatestSession = signal<boolean>(false);

  protected readonly ccIsSessionRunning = computed(() => {
    const session: CaptainCoasterSessionResponse | null = this.ccSession();
    return session !== null && session.status !== 'Completed' && session.status !== 'Failed';
  });

  protected readonly ccManualUrlCount = computed(() => this.parseManualUrls().length);

  protected readonly ccCanImport = computed(() => {
    if (this.ccIsBusy() || this.ccIsSessionRunning()) {
      return false;
    }

    if (this.ccImportKind() === 'manual-urls') {
      return this.ccManualUrlCount() > 0;
    }

    return true;
  });

  protected readonly ccComparisonLoaded = signal<boolean>(false);
  protected readonly ccPagedResult = signal<CaptainCoasterComparisonPagedResponse | null>(null);
  protected readonly ccIsLoadingPage = signal<boolean>(false);

  protected readonly ccCurrentPage = signal<number>(0);
  protected readonly ccPageSize = signal<number>(50);

  protected readonly ccFilters = signal<ComparisonFilters>({
    entityType: null,
    changeType: null,
    isApplied: null
  });

  private selectedIdsSet: Set<string> = new Set<string>();
  protected readonly ccSelectedCount = signal<number>(0);
  protected readonly ccAllPageSelected = signal<boolean>(false);

  protected readonly ccSessionUpdated = computed(() => this.ccPagedResult()?.sessionUpdatedCount ?? 0);
  protected readonly ccSessionMissing = computed(() => this.ccPagedResult()?.sessionMissingCount ?? 0);
  protected readonly ccSessionDuplicate = computed(() => this.ccPagedResult()?.sessionDuplicateCount ?? 0);
  protected readonly ccSessionApplied = computed(() => this.ccPagedResult()?.sessionAppliedCount ?? 0);
  protected readonly ccTotalCount = computed(() => this.ccPagedResult()?.totalCount ?? 0);
  protected readonly ccCurrentItems = computed(() => this.ccPagedResult()?.items ?? []);

  protected readonly entityTypeOptions = [
    { label: 'Tous les types', value: null },
    { label: 'Parc', value: 'Park' },
    { label: 'Coaster', value: 'Coaster' }
  ];

  protected readonly changeTypeOptions = [
    { label: 'Tous les changements', value: null },
    { label: 'Nouveaux', value: 'MissingLocal' },
    { label: 'Modifiés', value: 'Updated' },
    { label: 'Doublons externes', value: 'DuplicateExternal' }
  ];

  protected readonly appliedOptions = [
    { label: 'Tous', value: null },
    { label: 'Non appliqués', value: false },
    { label: 'Déjà appliqués', value: true }
  ];

  protected readonly scrapingStepOptions = [
    { label: 'Découverte des URLs', value: 'DiscoverUrls' },
    { label: 'Analyse des fiches coaster', value: 'FetchCoasters' },
    { label: 'Coordonnées des parcs', value: 'EnrichParkCoordinates' },
    { label: 'Construction de la comparaison', value: 'BuildComparison' }
  ];


  protected readonly scrapingThrottleFields = [
    {
      key: 'delayBetweenRequestsMs',
      type: 'number',
      label: 'Pause entre deux téléchargements (ms)',
      placeholder: '0',
      hint: 'Temps d’attente volontaire entre deux requêtes HTTP. 0 = débit maximal.'
    },
    {
      key: 'httpTimeoutSeconds',
      type: 'number',
      label: 'Timeout HTTP par page (s)',
      placeholder: '30',
      hint: 'Temps maximum d’attente pour une page avant de la considérer en échec.'
    },
    {
      key: 'maxRetryCount',
      type: 'number',
      label: 'Nombre maximum de tentatives',
      placeholder: '1',
      hint: 'Nombre d’essais par URL avant abandon.'
    },
    {
      key: 'maxConcurrentRequests',
      type: 'number',
      label: 'Téléchargements parallèles maximum',
      placeholder: '4',
      hint: 'Parallélisme borné pour accélérer le scraping sans saturer le serveur ni Mongo.'
    },
    {
      key: 'coasterWriteBatchSize',
      type: 'number',
      label: 'Taille des lots d’écriture Mongo',
      placeholder: '50',
      hint: 'Nombre de coasters regroupés avant écriture en base.'
    },
    {
      key: 'progressSaveInterval',
      type: 'number',
      label: 'Fréquence de sauvegarde de progression',
      placeholder: '25',
      hint: 'Nombre d’éléments traités entre deux sauvegardes/logs de session.'
    },
    {
      key: 'skipCoasterCount',
      type: 'number',
      label: 'Nombre d’URLs à ignorer au départ',
      placeholder: '0',
      hint: 'Utile pour reprendre un import ciblé ou tester plus vite.'
    },
    {
      key: 'maxCoasterCount',
      type: 'number',
      label: 'Nombre maximum d’URLs à traiter',
      placeholder: 'Vide = aucune limite',
      hint: 'Permet de limiter volontairement le volume traité pendant un lancement.'
    }
  ];

  protected readonly scrapingSelectorFields = [
    {
      key: 'mapMarkersAttributeName',
      label: 'Attribut HTML contenant les marqueurs de la carte',
      placeholder: 'data-map-markers-value',
      hint: 'Utilisé pour récupérer les coordonnées des parcs depuis la page carte Captain Coaster.'
    },
    {
      key: 'coasterTitleXPath',
      label: 'XPath du titre principal de la fiche coaster',
      placeholder: '//h1',
      hint: 'Sélecteur du nom principal affiché sur la page coaster.'
    },
    {
      key: 'characteristicsItemXPath',
      label: 'XPath du bloc des caractéristiques techniques',
      placeholder: "//div[contains(@class,'list-group-item')]",
      hint: 'Bloc racine répété contenant les informations techniques comme hauteur, vitesse, etc.'
    },
    {
      key: 'characteristicLabelXPath',
      label: 'XPath du libellé d’une caractéristique',
      placeholder: './/label',
      hint: 'Sélecteur du texte de gauche : hauteur, vitesse, inversions, etc.'
    },
    {
      key: 'characteristicValueXPath',
      label: 'XPath de la valeur d’une caractéristique',
      placeholder: ".//div[contains(@class,'pull-right')]",
      hint: 'Sélecteur de la valeur correspondant au libellé technique.'
    },
    {
      key: 'topMetricXPath',
      label: 'XPath des métriques mises en avant en haut de page',
      placeholder: "//button[contains(@class,'btn-float-lg')]//div[contains(@class,'text-bold')]",
      hint: 'Bloc utilisé pour certaines valeurs affichées dans les boutons de synthèse.'
    }
  ];

  private pollingSubscription: Subscription | null = null;
  private currentPollingMode: 'import' | 'apply' | null = null;
  private readonly duplicateResolutionStateByResultId: Map<string, DuplicateResolutionState> = new Map<string, DuplicateResolutionState>();

  constructor(
    private readonly dataSourcesApiService: DataSourcesApiService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    await this.refreshSourcesTableAsync();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  protected async selectSource(key: string): Promise<void> {
    this.selectedSourceKey.set(key);
    if (key === 'captain-coaster') {
      await this.loadCaptainCoasterInitAsync();
    }
  }

  protected backToSources(): void {
    this.stopPolling();
    this.selectedSourceKey.set(null);
    this.ccComparisonLoaded.set(false);
    this.ccPagedResult.set(null);
    this.resetSelection();
    this.duplicateResolutionStateByResultId.clear();
  }

  protected setImportKind(value: 'sitemap' | 'manual-urls'): void {
    this.ccImportKind.set(value);
    if (value === 'sitemap' && this.ccStartAtStep() === 'FetchCoasters') {
      this.ccStartAtStep.set('DiscoverUrls');
    }
  }

  protected setManualUrlsText(value: string): void {
    this.ccManualUrlsText.set(value);
  }

  protected setStartAtStep(value: string): void {
    this.ccStartAtStep.set(value);
  }

  protected setResumeLatestSession(value: boolean): void {
    this.ccResumeLatestSession.set(value);
  }

  protected getSettingValue(key: string): string {
    return this.ccSettings()?.options?.[key] ?? '';
  }

  protected setSettingValue(key: string, value: string): void {
    const current: CaptainCoasterSettingsResponse | null = this.ccSettings();
    if (current === null) {
      return;
    }

    this.ccSettings.set({
      ...current,
      options: {
        ...current.options,
        [key]: value
      }
    });
  }

  protected setSettingsEnabled(value: boolean): void {
    const current: CaptainCoasterSettingsResponse | null = this.ccSettings();
    if (current === null) {
      return;
    }

    this.ccSettings.set({
      ...current,
      isEnabled: value
    });
  }

  protected async saveSettingsAsync(showSuccessMessage: boolean = true): Promise<void> {
    const settings: CaptainCoasterSettingsResponse | null = this.ccSettings();
    if (settings === null) {
      return;
    }

    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');

    try {
      const request: UpdateCaptainCoasterSettingsRequest = {
        isEnabled: settings.isEnabled,
        options: settings.options
      };
      const updated: CaptainCoasterSettingsResponse = await firstValueFrom(
        this.dataSourcesApiService.updateSettings(request)
      );
      this.ccSettings.set(updated);
      if (showSuccessMessage) {
        this.ccSuccessMessage.set('Paramètres enregistrés.');
      }
      await this.refreshStatusAsync();
    } catch (error: unknown) {
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  protected async startImportAsync(): Promise<void> {
    if (!this.ccCanImport()) {
      this.ccErrorMessage.set('Complétez les paramètres requis avant de lancer le pipeline.');
      return;
    }

    const settings: CaptainCoasterSettingsResponse | null = this.ccSettings();
    if (settings === null) {
      this.ccErrorMessage.set('Les paramètres de la source ne sont pas encore chargés.');
      return;
    }

    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');
    this.ccSuccessMessage.set('');
    this.ccComparisonLoaded.set(false);
    this.ccPagedResult.set(null);
    this.resetSelection();
    this.duplicateResolutionStateByResultId.clear();

    try {
      const saveRequest: UpdateCaptainCoasterSettingsRequest = {
        isEnabled: settings.isEnabled,
        options: settings.options
      };
      const updatedSettings: CaptainCoasterSettingsResponse = await firstValueFrom(
        this.dataSourcesApiService.updateSettings(saveRequest)
      );
      this.ccSettings.set(updatedSettings);

      const request: StartCaptainCoasterImportRequest = {
        importKind: this.ccImportKind(),
        urls: this.parseManualUrls(),
        options: {
          startAtStep: this.ccStartAtStep()
        },
        resumeSessionId: this.resolveResumeSessionId()
      };

      const session: CaptainCoasterSessionResponse = await firstValueFrom(
        this.dataSourcesApiService.startImport(request)
      );
      this.ccSession.set(session);
      this.ccSuccessMessage.set('Pipeline démarré. Suivez la progression dans l\'onglet "Progression".');
      this.startPolling(session.id, 'import');
    } catch (error: unknown) {
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  protected async loadComparisonAsync(): Promise<void> {
    const sessionId: string | null = this.ccSession()?.id ?? null;
    this.ccCurrentPage.set(0);
    this.resetSelection();
    await this.fetchPageAsync(sessionId, this.ccFilters(), 0, this.ccPageSize());
    this.ccComparisonLoaded.set(true);
  }

  protected async onPageChange(event: { first?: number; rows?: number }): Promise<void> {
    const first: number = event.first ?? 0;
    const rows: number = event.rows ?? this.ccPageSize();
    const newPage: number = Math.floor(first / rows);
    const newPageSize: number = rows;
    this.ccCurrentPage.set(newPage);
    this.ccPageSize.set(newPageSize);
    this.resetSelection();
    await this.fetchPageAsync(this.ccSession()?.id ?? null, this.ccFilters(), newPage, newPageSize);
  }

  protected async onFilterChange(): Promise<void> {
    this.ccCurrentPage.set(0);
    this.resetSelection();
    await this.fetchPageAsync(this.ccSession()?.id ?? null, this.ccFilters(), 0, this.ccPageSize());
  }

  protected setEntityTypeFilter(value: string | null): void {
    const current: ComparisonFilters = this.ccFilters();
    this.ccFilters.set({ ...current, entityType: value });
    void this.onFilterChange();
  }

  protected setChangeTypeFilter(value: string | null): void {
    const current: ComparisonFilters = this.ccFilters();
    this.ccFilters.set({ ...current, changeType: value });
    void this.onFilterChange();
  }

  protected setAppliedFilter(value: boolean | null): void {
    const current: ComparisonFilters = this.ccFilters();
    this.ccFilters.set({ ...current, isApplied: value });
    void this.onFilterChange();
  }

  protected toggleSelection(id: string, checked: boolean): void {
    if (checked) {
      this.selectedIdsSet.add(id);
    } else {
      this.selectedIdsSet.delete(id);
    }
    this.syncSelectionSignals();
  }

  protected isSelected(id: string): boolean {
    return this.selectedIdsSet.has(id);
  }

  protected toggleSelectPage(checked: boolean): void {
    const items: CaptainCoasterComparisonResultResponse[] = this.ccCurrentItems();
    if (checked) {
      items.filter((item: CaptainCoasterComparisonResultResponse) => !item.isApplied)
        .forEach((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.add(item.id));
    } else {
      items.forEach((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.delete(item.id));
    }
    this.syncSelectionSignals();
  }

  protected async applySelectionAsync(): Promise<void> {
    if (this.selectedIdsSet.size === 0) {
      this.ccErrorMessage.set('Sélectionnez au moins une ligne.');
      return;
    }

    const sessionId: string | null = this.ccSession()?.id ?? null;
    if (sessionId === null) {
      this.ccErrorMessage.set('Aucune session Captain Coaster sélectionnée.');
      return;
    }

    const selectedRows: CaptainCoasterComparisonResultResponse[] = this.ccCurrentItems()
      .filter((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.has(item.id));
    const duplicateResolutions: CaptainCoasterDuplicateResolutionRequest[] = this.buildDuplicateResolutions(selectedRows);
    if (selectedRows.some((item: CaptainCoasterComparisonResultResponse) => item.requiresManualResolution) && duplicateResolutions.length === 0) {
      this.ccErrorMessage.set('Résolvez les doublons sélectionnés avant de lancer l’application.');
      return;
    }

    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');
    this.ccSuccessMessage.set('');
    this.startPolling(sessionId, 'apply');

    try {
      const response: { appliedCount: number } = await firstValueFrom(
        this.dataSourcesApiService.applySelectedIds(Array.from(this.selectedIdsSet), duplicateResolutions)
      );
      this.ccSuccessMessage.set(`${response.appliedCount} changement(s) appliqué(s).`);
      this.resetSelection();
      await this.fetchPageAsync(
        sessionId,
        this.ccFilters(),
        this.ccCurrentPage(),
        this.ccPageSize()
      );
      await this.refreshStatusAsync();
    } catch (error: unknown) {
      this.stopPolling();
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  protected async applyAllAsync(entityTypeFilter: string | null = null, changeTypeFilter: string | null = null): Promise<void> {
    if (changeTypeFilter === 'DuplicateExternal') {
      this.ccErrorMessage.set('Les doublons externes nécessitent une résolution manuelle ligne par ligne.');
      return;
    }

    const sessionId: string | null = this.ccSession()?.id ?? null;
    if (sessionId === null) {
      this.ccErrorMessage.set('Aucune session Captain Coaster sélectionnée.');
      return;
    }

    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');
    this.ccSuccessMessage.set('');
    this.startPolling(sessionId, 'apply');

    try {
      const response: { appliedCount: number } = await firstValueFrom(
        this.dataSourcesApiService.applyAll(
          sessionId,
          entityTypeFilter,
          changeTypeFilter
        )
      );
      this.ccSuccessMessage.set(`${response.appliedCount} changement(s) appliqué(s) au total. Les doublons externes restent à arbitrer manuellement.`);
      this.resetSelection();
      await this.fetchPageAsync(
        sessionId,
        this.ccFilters(),
        this.ccCurrentPage(),
        this.ccPageSize()
      );
      await this.refreshStatusAsync();
    } catch (error: unknown) {
      this.stopPolling();
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  protected getChangeTypeSeverity(changeType: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (changeType === 'Updated') { return 'warn'; }
    if (changeType === 'MissingLocal') { return 'info'; }
    if (changeType === 'DuplicateExternal') { return 'danger'; }
    return 'secondary';
  }

  protected getSessionStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (status === 'Completed') { return 'success'; }
    if (status === 'Failed') { return 'danger'; }
    return 'info';
  }

  protected trackById(_index: number, item: CaptainCoasterComparisonResultResponse): string {
    return item.id;
  }

  protected isDuplicateRow(item: CaptainCoasterComparisonResultResponse): boolean {
    return item.requiresManualResolution || item.hasExternalDuplicates;
  }

  protected getDuplicateState(item: CaptainCoasterComparisonResultResponse): DuplicateResolutionState {
    this.ensureDuplicateResolutionState(item);
    return this.duplicateResolutionStateByResultId.get(item.id)!;
  }

  protected setDuplicateStrategy(item: CaptainCoasterComparisonResultResponse, strategy: 'SelectVariant' | 'Merge'): void {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    state.strategy = strategy;
    if (state.selectedExternalVariantId === null) {
      state.selectedExternalVariantId = this.getSuggestedVariantId(item);
    }
    this.cdr.markForCheck();
  }

  protected setSelectedVariant(item: CaptainCoasterComparisonResultResponse, value: string | null): void {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    state.selectedExternalVariantId = value;

    if (value !== null) {
      const fields: string[] = this.getMergeEligibleFields(item);
      for (const field of fields) {
        if (!(field in state.fieldSelections)) {
          state.fieldSelections[field] = `variant:${value}`;
        }
      }
    }

    this.cdr.markForCheck();
  }

  protected getMergeEligibleFields(item: CaptainCoasterComparisonResultResponse): string[] {
    const fields: Set<string> = new Set<string>();
    for (const variant of item.externalVariants) {
      for (const change of variant.changes) {
        if (this.isMergeEligibleField(change.field)) {
          fields.add(change.field);
        }
      }
    }

    const order: string[] = [
      'name',
      'countryCode',
      'parkName',
      'manufacturer',
      'model',
      'sourceUrl',
      'status',
      'materialType',
      'seatingType',
      'launchType',
      'restraintType',
      'isLaunched',
      'openingDate',
      'closingDate',
      'heightInMeters',
      'lengthInMeters',
      'speedInKmH',
      'inversionCount'
    ];

    return Array.from(fields).sort((left: string, right: string) => {
      const leftIndex: number = order.indexOf(left);
      const rightIndex: number = order.indexOf(right);
      const safeLeftIndex: number = leftIndex === -1 ? 999 : leftIndex;
      const safeRightIndex: number = rightIndex === -1 ? 999 : rightIndex;
      if (safeLeftIndex !== safeRightIndex) {
        return safeLeftIndex - safeRightIndex;
      }
      return left.localeCompare(right);
    });
  }

  protected getMergeSelection(item: CaptainCoasterComparisonResultResponse, field: string): string {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    return state.fieldSelections[field] ?? `variant:${this.getSuggestedVariantId(item) ?? ''}`;
  }

  protected setMergeSelection(item: CaptainCoasterComparisonResultResponse, field: string, value: string): void {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    state.fieldSelections[field] = value;
    this.cdr.markForCheck();
  }

  protected getMergeOptions(item: CaptainCoasterComparisonResultResponse, field: string): { label: string; value: string }[] {
    const options: { label: string; value: string }[] = [{ label: 'Conserver la valeur locale', value: 'local' }];
    for (const variant of item.externalVariants) {
      const preview: string = this.getExternalFieldValue(variant, field) ?? '∅';
      options.push({
        label: `${variant.displayLabel} → ${preview}`,
        value: `variant:${variant.externalVariantId}`
      });
    }
    return options;
  }

  protected getResolutionSummary(item: CaptainCoasterComparisonResultResponse): string {
    const state: DuplicateResolutionState = this.getDuplicateState(item);
    if (state.strategy === 'SelectVariant') {
      const variant: CaptainCoasterExternalVariantResponse | undefined = item.externalVariants
        .find((entry: CaptainCoasterExternalVariantResponse) => entry.externalVariantId === state.selectedExternalVariantId);
      return variant?.displayLabel ?? 'Aucune variante sélectionnée';
    }

    return `Fusion (${this.getMergeEligibleFields(item).length} champ(s))`;
  }

  private async loadCaptainCoasterInitAsync(): Promise<void> {
    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');

    try {
      const status: CaptainCoasterStatusResponse = await firstValueFrom(
        this.dataSourcesApiService.getStatus()
      );
      const settings: CaptainCoasterSettingsResponse = await firstValueFrom(
        this.dataSourcesApiService.getSettings()
      );
      this.ccStatus.set(status);
      this.ccSettings.set(settings);

      try {
        const session: CaptainCoasterSessionResponse | null = await firstValueFrom(
          this.dataSourcesApiService.getLatestSession()
        );
        this.ccSession.set(session);
        if (session !== null) {
          this.ccImportKind.set(session.importKind === 'manual-urls' ? 'manual-urls' : 'sitemap');
          this.ccStartAtStep.set(session.availableSteps?.[0] ?? 'DiscoverUrls');
        }
        if (session !== null && session.status !== 'Completed' && session.status !== 'Failed') {
          this.startPolling(session.id, 'import');
        }
      } catch {
        this.ccSession.set(null);
      }
    } catch (error: unknown) {
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  private async fetchPageAsync(
    sessionId: string | null,
    filters: ComparisonFilters,
    page: number,
    pageSize: number
  ): Promise<void> {
    this.ccIsLoadingPage.set(true);
    try {
      const result: CaptainCoasterComparisonPagedResponse = await firstValueFrom(
        this.dataSourcesApiService.getComparisonResults(sessionId, filters, page, pageSize)
      );
      this.ccPagedResult.set(result);
      this.ensureDuplicateResolutionStates(result.items);
      this.cdr.markForCheck();
    } finally {
      this.ccIsLoadingPage.set(false);
    }
  }

  private async refreshSourcesTableAsync(): Promise<void> {
    try {
      const sources: DataSourceSummary[] = await firstValueFrom(
        this.dataSourcesApiService.listSources()
      );
      this.dataSources.set(sources);
    } catch {
      this.dataSources.set([{
        key: 'captain-coaster',
        label: 'Captain Coaster',
        description: 'Acquisition automatisée Captain Coaster via sitemap, URLs ciblées et pipeline de comparaison.',
        icon: 'pi pi-cloud-download',
        isEnabled: false,
        lastImportUtc: null,
        totalSessions: 0,
        statusLabel: 'Indisponible'
      }]);
    }
  }

  private async refreshStatusAsync(): Promise<void> {
    const status: CaptainCoasterStatusResponse = await firstValueFrom(
      this.dataSourcesApiService.getStatus()
    );
    this.ccStatus.set(status);

    try {
      const session: CaptainCoasterSessionResponse | null = await firstValueFrom(
        this.dataSourcesApiService.getLatestSession()
      );
      this.ccSession.set(session);
    } catch {
      // no-op
    }

    await this.refreshSourcesTableAsync();
  }

  private startPolling(sessionId: string, mode: 'import' | 'apply'): void {
    this.stopPolling();
    this.currentPollingMode = mode;
    this.pollingSubscription = interval(3000).pipe(
      switchMap(() =>
        this.dataSourcesApiService.getSessionById(sessionId).pipe(catchError(() => of(null)))
      )
    ).subscribe((session: CaptainCoasterSessionResponse | null) => {
      if (session === null || session.id !== sessionId) {
        return;
      }
      this.ccSession.set(session);
      this.cdr.markForCheck();

      if (session.status === 'Completed' || session.status === 'Failed') {
        const completedMode: 'import' | 'apply' | null = this.currentPollingMode;
        this.stopPolling();
        void this.refreshStatusAsync();
        if (session.status === 'Completed') {
          if (completedMode === 'apply' || session.lastCompletedStep === 'ApplyComparison') {
            this.ccSuccessMessage.set(session.message || `Application métier terminée : ${session.appliedChanges} changement(s) appliqué(s).`);
          } else {
            this.ccSuccessMessage.set(
              `Pipeline terminé : ${session.discoveredItems} URL(s) découvertes, ${session.processedItems} fiche(s) traitée(s), ` +
              `${session.comparisonResults} différence(s), dont ${session.duplicateConflicts} conflit(s) à arbitrer.`
            );
          }
        } else {
          this.ccErrorMessage.set(completedMode === 'apply'
            ? `L'application métier a échoué : ${session.message}`
            : `Le pipeline a échoué : ${session.message}`);
        }
        this.cdr.markForCheck();
      }
    });
  }

  private stopPolling(): void {
    if (this.pollingSubscription !== null) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }

    this.currentPollingMode = null;
  }

  private resetSelection(): void {
    this.selectedIdsSet.clear();
    this.syncSelectionSignals();
  }

  private syncSelectionSignals(): void {
    this.ccSelectedCount.set(this.selectedIdsSet.size);
    const items: CaptainCoasterComparisonResultResponse[] = this.ccCurrentItems();
    const unapplied: CaptainCoasterComparisonResultResponse[] = items.filter((item: CaptainCoasterComparisonResultResponse) => !item.isApplied);
    const allPageSelected: boolean = unapplied.length > 0 && unapplied.every((item: CaptainCoasterComparisonResultResponse) => this.selectedIdsSet.has(item.id));
    this.ccAllPageSelected.set(allPageSelected);
    this.cdr.markForCheck();
  }

  private ensureDuplicateResolutionStates(items: CaptainCoasterComparisonResultResponse[]): void {
    for (const item of items) {
      this.ensureDuplicateResolutionState(item);
    }
  }

  private ensureDuplicateResolutionState(item: CaptainCoasterComparisonResultResponse): void {
    if (!item.requiresManualResolution) {
      return;
    }
    if (this.duplicateResolutionStateByResultId.has(item.id)) {
      return;
    }

    const suggestedVariantId: string | null = this.getSuggestedVariantId(item);
    const fieldSelections: Record<string, string> = {};
    for (const field of this.getMergeEligibleFields(item)) {
      fieldSelections[field] = suggestedVariantId === null ? 'local' : `variant:${suggestedVariantId}`;
    }

    this.duplicateResolutionStateByResultId.set(item.id, {
      strategy: 'SelectVariant',
      selectedExternalVariantId: suggestedVariantId,
      fieldSelections
    });
  }

  private getSuggestedVariantId(item: CaptainCoasterComparisonResultResponse): string | null {
    return item.externalVariants.find((variant: CaptainCoasterExternalVariantResponse) => variant.isSuggested)?.externalVariantId
      ?? item.externalVariants[0]?.externalVariantId
      ?? null;
  }

  private isMergeEligibleField(field: string): boolean {
    return field !== 'duplicateVariants'
      && field !== 'externalId'
      && field !== 'externalSource'
      && field !== 'heightInFeet'
      && field !== 'lengthInFeet'
      && field !== 'speedInMph';
  }

  private getExternalFieldValue(variant: CaptainCoasterExternalVariantResponse, field: string): string | null {
    const change: { externalValue: string | null } | undefined = variant.changes.find((item) => item.field === field);
    return change?.externalValue ?? null;
  }

  private buildDuplicateResolutions(items: CaptainCoasterComparisonResultResponse[]): CaptainCoasterDuplicateResolutionRequest[] {
    const resolutions: CaptainCoasterDuplicateResolutionRequest[] = [];

    for (const item of items) {
      if (!item.requiresManualResolution) {
        continue;
      }

      const state: DuplicateResolutionState | undefined = this.duplicateResolutionStateByResultId.get(item.id);
      if (!state) {
        continue;
      }

      if (state.strategy === 'SelectVariant') {
        if (state.selectedExternalVariantId === null) {
          continue;
        }

        resolutions.push({
          comparisonResultId: item.id,
          strategy: 'SelectVariant',
          selectedExternalVariantId: state.selectedExternalVariantId,
          fieldResolutions: []
        });
        continue;
      }

      const fieldResolutions: CaptainCoasterFieldResolutionRequest[] = this.getMergeEligibleFields(item)
        .map((field: string) => this.buildFieldResolution(field, state.fieldSelections[field] ?? 'local'));

      resolutions.push({
        comparisonResultId: item.id,
        strategy: 'Merge',
        selectedExternalVariantId: state.selectedExternalVariantId,
        fieldResolutions
      });
    }

    return resolutions;
  }

  private buildFieldResolution(field: string, rawSelection: string): CaptainCoasterFieldResolutionRequest {
    if (rawSelection === 'local') {
      return {
        field,
        sourceType: 'Local',
        externalVariantId: null
      };
    }

    const variantId: string = rawSelection.replace('variant:', '');
    return {
      field,
      sourceType: 'Variant',
      externalVariantId: variantId
    };
  }

  private resolveResumeSessionId(): string | null {
    const session: CaptainCoasterSessionResponse | null = this.ccSession();
    if (!this.ccResumeLatestSession() || session === null || !session.canResume) {
      return null;
    }

    return session.id;
  }

  private parseManualUrls(): string[] {
    const content: string = this.ccManualUrlsText();
    return content
      .split(/\r?\n/g)
      .map((item: string) => item.trim())
      .filter((item: string) => item.length > 0);
  }

  private extractErrorMessage(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const payload: unknown = (error as { error: unknown }).error;
      if (typeof payload === 'string' && payload.trim().length > 0) {
        return payload;
      }
      if (typeof payload === 'object' && payload !== null && 'message' in payload) {
        const message: unknown = (payload as { message?: unknown }).message;
        if (typeof message === 'string' && message.trim().length > 0) {
          return message;
        }
      }
    }
    return 'Une erreur est survenue.';
  }
}
