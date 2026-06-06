import {
  Injectable,
  OnDestroy,
  Signal,
  computed,
  signal,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom, interval, of, Subscription } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import {
  CaptainCoasterSessionResponse,
  CaptainCoasterSettingsResponse,
  CaptainCoasterStatusResponse,
  StartCaptainCoasterImportRequest,
  UpdateCaptainCoasterSettingsRequest
} from '@app/models/admin/data/data-management.models';
import { AdminDataSourcesFacade } from './admin-data-sources.facade';

import { extractSafeDisplayErrorMessage, sanitizeDisplayMessage } from '@shared/utils/security';

import {
  CAPTAIN_COASTER_PIPELINE_DATA_SOURCES_API_SERVICE_PORT,
  CaptainCoasterPipelineDataSourcesApiServicePort
} from './captain-coaster-pipeline-data.ports';
interface CaptainCoasterSettingField {
  key: string;
  type?: 'text' | 'number';
  label: string;
  placeholder: string;
  hint: string;
}

@Injectable()
export class CaptainCoasterPipelineFacade implements OnDestroy {
  private readonly statusSignal = signal<CaptainCoasterStatusResponse | null>(null);
  private readonly settingsSignal = signal<CaptainCoasterSettingsResponse | null>(null);
  private readonly sessionSignal = signal<CaptainCoasterSessionResponse | null>(null);
  private readonly isBusySignal = signal(false);
  private readonly errorMessageSignal = signal('');
  private readonly successMessageSignal = signal('');
  private readonly comparisonGenerationSignal = signal(0);

  private readonly importKindSignal = signal<'sitemap' | 'manual-urls'>('sitemap');
  private readonly manualUrlsTextSignal = signal('');
  private readonly startAtStepSignal = signal('DiscoverUrls');
  private readonly resumeLatestSessionSignal = signal(false);

  private pollingSubscription: Subscription | null = null;
  private currentPollingMode: 'import' | 'apply' | null = null;

  public readonly status: Signal<CaptainCoasterStatusResponse | null> = this.statusSignal.asReadonly();
  public readonly settings: Signal<CaptainCoasterSettingsResponse | null> = this.settingsSignal.asReadonly();
  public readonly session: Signal<CaptainCoasterSessionResponse | null> = this.sessionSignal.asReadonly();
  public readonly isBusy: Signal<boolean> = this.isBusySignal.asReadonly();
  public readonly errorMessage: Signal<string> = this.errorMessageSignal.asReadonly();
  public readonly successMessage: Signal<string> = this.successMessageSignal.asReadonly();
  public readonly comparisonGeneration: Signal<number> = this.comparisonGenerationSignal.asReadonly();

  public readonly importKind: Signal<'sitemap' | 'manual-urls'> = this.importKindSignal.asReadonly();
  public readonly manualUrlsText: Signal<string> = this.manualUrlsTextSignal.asReadonly();
  public readonly startAtStep: Signal<string> = this.startAtStepSignal.asReadonly();
  public readonly resumeLatestSession: Signal<boolean> = this.resumeLatestSessionSignal.asReadonly();

  public readonly isSessionRunning = computed(() => {
    const session: CaptainCoasterSessionResponse | null = this.sessionSignal();
    return session !== null && session.status !== 'Completed' && session.status !== 'Failed';
  });

  public readonly manualUrlCount = computed(() => this.parseManualUrls().length);
  public readonly canImport = computed(() => {
    if (this.isBusySignal() || this.isSessionRunning()) {
      return false;
    }

    if (this.importKindSignal() === 'manual-urls') {
      return this.manualUrlCount() > 0;
    }

    return true;
  });

  public readonly scrapingStepOptions = [
    { label: 'Découverte des URLs', value: 'DiscoverUrls' },
    { label: 'Analyse des fiches coaster', value: 'FetchCoasters' },
    { label: 'Coordonnées des parcs', value: 'EnrichParkCoordinates' },
    { label: 'Construction de la comparaison', value: 'BuildComparison' }
  ];

  public readonly scrapingThrottleFields: CaptainCoasterSettingField[] = [
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

  public readonly scrapingSelectorFields: CaptainCoasterSettingField[] = [
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

  constructor(
    @Inject(CAPTAIN_COASTER_PIPELINE_DATA_SOURCES_API_SERVICE_PORT) private readonly dataSourcesApiService: CaptainCoasterPipelineDataSourcesApiServicePort,
    private readonly adminDataSourcesFacade: AdminDataSourcesFacade,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  async initializeAsync(): Promise<void> {
    this.isBusySignal.set(true);
    this.errorMessageSignal.set('');

    try {
      const status: CaptainCoasterStatusResponse = await firstValueFrom(this.dataSourcesApiService.getStatus());
      const settings: CaptainCoasterSettingsResponse = await firstValueFrom(this.dataSourcesApiService.getSettings());
      this.statusSignal.set(status);
      this.settingsSignal.set(settings);

      try {
        const session: CaptainCoasterSessionResponse | null = await firstValueFrom(this.dataSourcesApiService.getLatestSession());
        this.sessionSignal.set(session);
        if (session !== null) {
          this.importKindSignal.set(session.importKind === 'manual-urls' ? 'manual-urls' : 'sitemap');
          this.startAtStepSignal.set(session.availableSteps?.[0] ?? 'DiscoverUrls');
        }

        if (session !== null && session.status !== 'Completed' && session.status !== 'Failed') {
          this.startPolling(session.id, 'import');
        }
      } catch {
        this.sessionSignal.set(null);
      }
    } catch (error: unknown) {
      this.errorMessageSignal.set(this.extractErrorMessage(error));
    } finally {
      this.isBusySignal.set(false);
    }
  }

  setImportKind(value: 'sitemap' | 'manual-urls'): void {
    this.importKindSignal.set(value);
    if (value === 'sitemap' && this.startAtStepSignal() == 'FetchCoasters') {
      this.startAtStepSignal.set('DiscoverUrls');
    }
  }

  setManualUrlsText(value: string): void {
    this.manualUrlsTextSignal.set(value);
  }

  setStartAtStep(value: string): void {
    this.startAtStepSignal.set(value);
  }

  setResumeLatestSession(value: boolean): void {
    this.resumeLatestSessionSignal.set(value);
  }

  getSettingValue(key: string): string {
    return this.settingsSignal()?.options?.[key] ?? '';
  }

  setSettingValue(key: string, value: string): void {
    const current: CaptainCoasterSettingsResponse | null = this.settingsSignal();
    if (current === null) {
      return;
    }

    this.settingsSignal.set({
      ...current,
      options: {
        ...current.options,
        [key]: value
      }
    });
  }

  setSettingsEnabled(value: boolean): void {
    const current: CaptainCoasterSettingsResponse | null = this.settingsSignal();
    if (current === null) {
      return;
    }

    this.settingsSignal.set({
      ...current,
      isEnabled: value
    });
  }

  async saveSettingsAsync(showSuccessMessage: boolean = true): Promise<void> {
    const settings: CaptainCoasterSettingsResponse | null = this.settingsSignal();
    if (settings === null) {
      return;
    }

    this.isBusySignal.set(true);
    this.errorMessageSignal.set('');

    try {
      const request: UpdateCaptainCoasterSettingsRequest = {
        isEnabled: settings.isEnabled,
        options: settings.options
      };
      const updatedSettings: CaptainCoasterSettingsResponse = await firstValueFrom(
        this.dataSourcesApiService.updateSettings(request)
      );
      this.settingsSignal.set(updatedSettings);
      if (showSuccessMessage) {
        this.successMessageSignal.set('Paramètres enregistrés.');
      }
      await this.refreshStatusAsync();
    } catch (error: unknown) {
      this.errorMessageSignal.set(this.extractErrorMessage(error));
    } finally {
      this.isBusySignal.set(false);
    }
  }

  async startImportAsync(): Promise<void> {
    if (!this.canImport()) {
      this.errorMessageSignal.set('Complétez les paramètres requis avant de lancer le pipeline.');
      return;
    }

    const settings: CaptainCoasterSettingsResponse | null = this.settingsSignal();
    if (settings === null) {
      this.errorMessageSignal.set('Les paramètres de la source ne sont pas encore chargés.');
      return;
    }

    this.isBusySignal.set(true);
    this.errorMessageSignal.set('');
    this.successMessageSignal.set('');
    this.comparisonGenerationSignal.update((value: number) => value + 1);

    try {
      const saveRequest: UpdateCaptainCoasterSettingsRequest = {
        isEnabled: settings.isEnabled,
        options: settings.options
      };
      const updatedSettings: CaptainCoasterSettingsResponse = await firstValueFrom(
        this.dataSourcesApiService.updateSettings(saveRequest)
      );
      this.settingsSignal.set(updatedSettings);

      const request: StartCaptainCoasterImportRequest = {
        importKind: this.importKindSignal(),
        urls: this.parseManualUrls(),
        options: {
          startAtStep: this.startAtStepSignal()
        },
        resumeSessionId: this.resolveResumeSessionId()
      };

      const session: CaptainCoasterSessionResponse = await firstValueFrom(this.dataSourcesApiService.startImport(request));
      this.sessionSignal.set(session);
      this.successMessageSignal.set('Pipeline démarré. Suivez la progression dans l\'onglet "Progression".');
      this.startPolling(session.id, 'import');
    } catch (error: unknown) {
      this.errorMessageSignal.set(this.extractErrorMessage(error));
    } finally {
      this.isBusySignal.set(false);
    }
  }

  async refreshStatusAsync(): Promise<void> {
    const status: CaptainCoasterStatusResponse = await firstValueFrom(this.dataSourcesApiService.getStatus());
    this.statusSignal.set(status);

    try {
      const session: CaptainCoasterSessionResponse | null = await firstValueFrom(this.dataSourcesApiService.getLatestSession());
      this.sessionSignal.set(session);
    } catch {
      // No latest session available.
    }

    await this.adminDataSourcesFacade.loadSourcesAsync();
  }

  startPolling(sessionId: string, mode: 'import' | 'apply'): void {
    this.stopPolling();
    this.currentPollingMode = mode;
    this.pollingSubscription = interval(3000).pipe(
      switchMap(() => this.dataSourcesApiService.getSessionById(sessionId).pipe(catchError(() => of(null))))
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((session: CaptainCoasterSessionResponse | null) => {
      if (session === null || session.id !== sessionId) {
        return;
      }

      this.sessionSignal.set(session);

      if (session.status === 'Completed' || session.status === 'Failed') {
        const completedMode: 'import' | 'apply' | null = this.currentPollingMode;
        this.stopPolling();
        void this.refreshStatusAsync();

        if (session.status === 'Completed') {
          if (completedMode === 'apply' || session.lastCompletedStep === 'ApplyComparison') {
            this.successMessageSignal.set(
              sanitizeDisplayMessage(session.message, '') || `Application métier terminée : ${session.appliedChanges} changement(s) appliqué(s).`
            );
          } else {
            this.successMessageSignal.set(
              `Pipeline terminé : ${session.discoveredItems} URL(s) découvertes, ${session.processedItems} fiche(s) traitée(s), ` +
              `${session.comparisonResults} différence(s), dont ${session.duplicateConflicts} conflit(s) à arbitrer.`
            );
          }
        } else {
          this.errorMessageSignal.set(completedMode === 'apply'
            ? `L'application métier a échoué : ${sanitizeDisplayMessage(session.message, 'une erreur est survenue')}`
            : `Le pipeline a échoué : ${sanitizeDisplayMessage(session.message, 'une erreur est survenue')}`);
        }
      }
    });
  }

  stopPolling(): void {
    if (this.pollingSubscription !== null) {
      this.pollingSubscription.unsubscribe();
      this.pollingSubscription = null;
    }

    this.currentPollingMode = null;
  }

  setBusy(value: boolean): void {
    this.isBusySignal.set(value);
  }

  clearFeedbackMessages(): void {
    this.errorMessageSignal.set('');
    this.successMessageSignal.set('');
  }

  setErrorMessage(message: string): void {
    this.errorMessageSignal.set(sanitizeDisplayMessage(message));
  }

  setSuccessMessage(message: string): void {
    this.successMessageSignal.set(sanitizeDisplayMessage(message, ''));
  }

  getSessionStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (status === 'Completed') {
      return 'success';
    }

    if (status === 'Failed') {
      return 'danger';
    }

    return 'info';
  }

  private resolveResumeSessionId(): string | null {
    const session: CaptainCoasterSessionResponse | null = this.sessionSignal();
    if (!this.resumeLatestSessionSignal() || session === null || !session.canResume) {
      return null;
    }

    return session.id;
  }

  private parseManualUrls(): string[] {
    return this.manualUrlsTextSignal()
      .split(/\r?\n/g)
      .map((item: string) => item.trim())
      .filter((item: string) => item.length > 0);
  }

  private extractErrorMessage(error: unknown): string {
    return extractSafeDisplayErrorMessage(error);
  }
}
