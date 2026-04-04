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
import { PaginatorModule } from 'primeng/paginator';

import { CaptainCoasterDataService } from '../../../../services/admin/data/captain-coaster-data.service';
import {
  CaptainCoasterComparisonPagedResponse,
  CaptainCoasterComparisonResultResponse,
  CaptainCoasterSessionResponse,
  CaptainCoasterStatusResponse,
  ComparisonFilters,
  DataSourceSummary
} from '../../../../models/admin/data/data-management.models';

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
    PaginatorModule
  ]
})
export class AdminDataComponent implements OnInit, OnDestroy {

  // -----------------------------------------------------------------------
  // Sources list
  // -----------------------------------------------------------------------
  protected readonly dataSources = signal<DataSourceSummary[]>([]);
  protected readonly selectedSourceKey = signal<string | null>(null);

  // -----------------------------------------------------------------------
  // Captain Coaster — état global
  // -----------------------------------------------------------------------
  protected readonly ccStatus = signal<CaptainCoasterStatusResponse | null>(null);
  protected readonly ccSession = signal<CaptainCoasterSessionResponse | null>(null);
  protected readonly ccIsBusy = signal<boolean>(false);
  protected readonly ccErrorMessage = signal<string>('');
  protected readonly ccSuccessMessage = signal<string>('');
  protected readonly ccParksFile = signal<File | null>(null);
  protected readonly ccCoastersFile = signal<File | null>(null);

  protected readonly ccIsSessionRunning = computed(() => {
    const session: CaptainCoasterSessionResponse | null = this.ccSession();
    return session !== null && session.status !== 'Completed' && session.status !== 'Failed';
  });

  protected readonly ccCanImport = computed(() =>
    this.ccParksFile() !== null &&
    this.ccCoastersFile() !== null &&
    !this.ccIsBusy() &&
    !this.ccIsSessionRunning()
  );

  // -----------------------------------------------------------------------
  // Captain Coaster — comparaison (chargée à la demande uniquement)
  // -----------------------------------------------------------------------
  protected readonly ccComparisonLoaded = signal<boolean>(false);
  protected readonly ccPagedResult = signal<CaptainCoasterComparisonPagedResponse | null>(null);
  protected readonly ccIsLoadingPage = signal<boolean>(false);

  // Pagination
  protected readonly ccCurrentPage = signal<number>(0);
  protected readonly ccPageSize = signal<number>(50);

  // Filtres
  protected readonly ccFilters = signal<ComparisonFilters>({
    entityType: null,
    changeType: null,
    isApplied: null
  });

  // Sélection — utilise un Set pour O(1) lookup
  private selectedIdsSet = new Set<string>();
  protected readonly ccSelectedCount = signal<number>(0);
  protected readonly ccAllPageSelected = signal<boolean>(false);

  // Computed depuis le résultat paginé (pas de scan côté client)
  protected readonly ccSessionUpdated = computed(() => this.ccPagedResult()?.sessionUpdatedCount ?? 0);
  protected readonly ccSessionMissing = computed(() => this.ccPagedResult()?.sessionMissingCount ?? 0);
  protected readonly ccSessionApplied = computed(() => this.ccPagedResult()?.sessionAppliedCount ?? 0);
  protected readonly ccTotalCount = computed(() => this.ccPagedResult()?.totalCount ?? 0);
  protected readonly ccCurrentItems = computed(() => this.ccPagedResult()?.items ?? []);

  // Dropdown options
  protected readonly entityTypeOptions = [
    { label: 'Tous les types', value: null },
    { label: 'Parc', value: 'Park' },
    { label: 'Coaster', value: 'Coaster' }
  ];

  protected readonly changeTypeOptions = [
    { label: 'Tous les changements', value: null },
    { label: 'Nouveaux', value: 'MissingLocal' },
    { label: 'Modifiés', value: 'Updated' }
  ];

  protected readonly appliedOptions = [
    { label: 'Tous', value: null },
    { label: 'Non appliqués', value: false },
    { label: 'Déjà appliqués', value: true }
  ];

  private pollingSubscription: Subscription | null = null;

  constructor(
    private readonly captainCoasterDataService: CaptainCoasterDataService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    await this.refreshSourcesTableAsync();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  // -----------------------------------------------------------------------
  // Navigation sources
  // -----------------------------------------------------------------------

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
  }

  // -----------------------------------------------------------------------
  // Import : sélection de fichiers
  // -----------------------------------------------------------------------

  protected onParksFileSelected(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    this.ccParksFile.set(input.files?.[0] ?? null);
  }

  protected onCoastersFileSelected(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    this.ccCoastersFile.set(input.files?.[0] ?? null);
  }

  // -----------------------------------------------------------------------
  // Import : démarrage
  // -----------------------------------------------------------------------

  protected async startImportAsync(): Promise<void> {
    const parksFile: File | null = this.ccParksFile();
    const coastersFile: File | null = this.ccCoastersFile();
    if (parksFile === null || coastersFile === null) {
      this.ccErrorMessage.set('Veuillez sélectionner les deux fichiers JSON.');
      return;
    }

    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');
    this.ccSuccessMessage.set('');
    this.ccComparisonLoaded.set(false);
    this.ccPagedResult.set(null);
    this.resetSelection();

    try {
      const session: CaptainCoasterSessionResponse = await firstValueFrom(
        this.captainCoasterDataService.importFromFiles(parksFile, coastersFile)
      );
      this.ccSession.set(session);
      this.ccSuccessMessage.set('Import démarré. Suivez la progression dans l\'onglet "Progression".');
      this.startPolling(session.id);
    } catch (error: unknown) {
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  // -----------------------------------------------------------------------
  // Comparaison : chargement à la demande
  // -----------------------------------------------------------------------

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
    await this.fetchPageAsync(
      this.ccSession()?.id ?? null,
      this.ccFilters(),
      0,
      this.ccPageSize()
    );
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

  // -----------------------------------------------------------------------
  // Sélection — O(1) avec Set
  // -----------------------------------------------------------------------

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
      items.filter(item => !item.isApplied).forEach(item => this.selectedIdsSet.add(item.id));
    } else {
      items.forEach(item => this.selectedIdsSet.delete(item.id));
    }
    this.syncSelectionSignals();
  }

  // -----------------------------------------------------------------------
  // Apply
  // -----------------------------------------------------------------------

  protected async applySelectionAsync(): Promise<void> {
    if (this.selectedIdsSet.size === 0) {
      this.ccErrorMessage.set('Sélectionnez au moins une ligne.');
      return;
    }

    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');
    this.ccSuccessMessage.set('');

    try {
      const response: { appliedCount: number } = await firstValueFrom(
        this.captainCoasterDataService.applySelectedIds(Array.from(this.selectedIdsSet))
      );
      this.ccSuccessMessage.set(`${response.appliedCount} changement(s) appliqué(s).`);
      this.resetSelection();
      await this.fetchPageAsync(
        this.ccSession()?.id ?? null,
        this.ccFilters(),
        this.ccCurrentPage(),
        this.ccPageSize()
      );
      await this.refreshStatusAsync();
    } catch (error: unknown) {
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  protected async applyAllAsync(entityTypeFilter: string | null = null, changeTypeFilter: string | null = null): Promise<void> {
    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');
    this.ccSuccessMessage.set('');

    try {
      const response: { appliedCount: number } = await firstValueFrom(
        this.captainCoasterDataService.applyAll(
          this.ccSession()?.id ?? null,
          entityTypeFilter,
          changeTypeFilter
        )
      );
      this.ccSuccessMessage.set(`${response.appliedCount} changement(s) appliqué(s) au total.`);
      this.resetSelection();
      await this.fetchPageAsync(
        this.ccSession()?.id ?? null,
        this.ccFilters(),
        this.ccCurrentPage(),
        this.ccPageSize()
      );
      await this.refreshStatusAsync();
    } catch (error: unknown) {
      this.ccErrorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.ccIsBusy.set(false);
    }
  }

  // -----------------------------------------------------------------------
  // Helpers template
  // -----------------------------------------------------------------------

  protected getChangeTypeSeverity(changeType: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (changeType === 'Updated') { return 'warn'; }
    if (changeType === 'MissingLocal') { return 'info'; }
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

  // -----------------------------------------------------------------------
  // Private
  // -----------------------------------------------------------------------

  private async loadCaptainCoasterInitAsync(): Promise<void> {
    this.ccIsBusy.set(true);
    this.ccErrorMessage.set('');

    try {
      const status: CaptainCoasterStatusResponse = await firstValueFrom(
        this.captainCoasterDataService.getStatus()
      );
      this.ccStatus.set(status);

      try {
        const session: CaptainCoasterSessionResponse = await firstValueFrom(
          this.captainCoasterDataService.getLatestSession()
        );
        this.ccSession.set(session);
        if (session.status !== 'Completed' && session.status !== 'Failed') {
          this.startPolling(session.id);
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
        this.captainCoasterDataService.getComparisonResults(sessionId, filters, page, pageSize)
      );
      this.ccPagedResult.set(result);
      this.cdr.markForCheck();
    } finally {
      this.ccIsLoadingPage.set(false);
    }
  }

  private async refreshSourcesTableAsync(): Promise<void> {
    try {
      const status: CaptainCoasterStatusResponse = await firstValueFrom(
        this.captainCoasterDataService.getStatus()
      );
      this.dataSources.set([{
        key: 'captain-coaster',
        label: 'Captain Coaster',
        description: 'Données de coasters et parcs depuis le scraper Captain Coaster (JSON)',
        icon: 'pi pi-cloud-download',
        isEnabled: status.isEnabled,
        lastImportUtc: status.lastSuccessfulImportUtc,
        totalSessions: status.totalSessionsCount,
        statusLabel: status.lastSuccessfulImportUtc ? 'Actif' : 'Jamais importé'
      }]);
    } catch {
      this.dataSources.set([{
        key: 'captain-coaster',
        label: 'Captain Coaster',
        description: 'Données de coasters et parcs depuis le scraper Captain Coaster (JSON)',
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
      this.captainCoasterDataService.getStatus()
    );
    this.ccStatus.set(status);
    await this.refreshSourcesTableAsync();
  }

  private startPolling(sessionId: string): void {
    this.stopPolling();
    this.pollingSubscription = interval(3000).pipe(
      switchMap(() =>
        this.captainCoasterDataService.getLatestSession().pipe(catchError(() => of(null)))
      )
    ).subscribe((session: CaptainCoasterSessionResponse | null) => {
      if (session === null) { return; }
      this.ccSession.set(session);
      this.cdr.markForCheck();

      if (session.status === 'Completed' || session.status === 'Failed') {
        this.stopPolling();
        void this.refreshStatusAsync();
        if (session.status === 'Completed') {
          this.ccSuccessMessage.set(
            `Import terminé : ${session.parksFetched} parcs, ${session.coastersFetched} coasters, ` +
            `${session.comparisonResults} différences. Cliquez sur "Charger les résultats" pour les consulter.`
          );
        } else {
          this.ccErrorMessage.set(`L'import a échoué : ${session.message}`);
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
  }

  private resetSelection(): void {
    this.selectedIdsSet.clear();
    this.syncSelectionSignals();
  }

  private syncSelectionSignals(): void {
    this.ccSelectedCount.set(this.selectedIdsSet.size);
    const items: CaptainCoasterComparisonResultResponse[] = this.ccCurrentItems();
    const unapplied: CaptainCoasterComparisonResultResponse[] = items.filter(item => !item.isApplied);
    const allPageSelected: boolean = unapplied.length > 0 && unapplied.every(item => this.selectedIdsSet.has(item.id));
    this.ccAllPageSelected.set(allPageSelected);
    this.cdr.markForCheck();
  }

  private extractErrorMessage(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const payload: Record<string, unknown> = (error as Record<string, unknown>)['error'] as Record<string, unknown>;
      if (typeof payload?.['message'] === 'string') { return payload['message']; }
    }
    return 'Une erreur est survenue.';
  }
}
