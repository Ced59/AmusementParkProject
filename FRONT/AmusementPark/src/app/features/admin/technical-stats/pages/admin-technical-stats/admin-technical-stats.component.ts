import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';

import {
  TechnicalStatsCount,
  TechnicalStatsRobotFamily,
  TechnicalStatsSnapshot
} from '@app/models/admin/technical-stats/technical-stats.models';
import { AdminTechnicalStatsFacade } from '../../state/admin-technical-stats.facade';

type TechnicalStatsLanguage = 'fr' | 'en';
type TechnicalStatsTab = 'summary' | 'cache' | 'rendering' | 'seo' | 'config';
type RobotFamilyFilter = 'all' | 'google' | 'bing' | 'yandex' | 'other';

const MAX_DISTRIBUTION_ROWS = 12;

interface TechnicalStatsMetricRow {
  readonly label: string;
  readonly value: string;
  readonly help: string;
}

interface TechnicalStatsTabOption {
  readonly key: TechnicalStatsTab;
  readonly labelKey: string;
}

interface RobotFamilyFilterOption {
  readonly key: RobotFamilyFilter;
  readonly labelKey: string;
}

const COPY: Record<TechnicalStatsLanguage, Record<string, string>> = {
  fr: {
    nav: 'Stats techniques',
    kicker: 'Observabilité SSR',
    title: 'Stats techniques',
    subtitle: 'Suivi du cache SSR applicatif, des robots, du rendu et des purges sur la fenêtre retenue.',
    refresh: 'Rafraîchir',
    loading: 'Chargement des stats techniques...',
    errorTitle: 'Stats indisponibles',
    errorMessage: 'Le serveur SSR ne répond pas encore aux stats techniques internes.',
    unavailableMissingSettings: 'La configuration interne SSR du back est incomplète : URL interne ou token partagé absent.',
    unavailableForbidden: 'Le serveur SSR répond 403 : le token partagé entre back et front SSR ne correspond pas.',
    unavailableNotFound: 'Le serveur SSR répond 404 : endpoint interne absent ou token SSR non configuré côté front.',
    unavailableTimeout: 'Le serveur SSR ne répond pas dans le délai attendu.',
    unavailableRequestFailed: 'Le back ne joint pas le service front SSR sur le réseau Docker interne.',
    unavailableEmpty: 'Le serveur SSR a répondu sans snapshot exploitable.',
    unavailableInvalidJson: 'Le serveur SSR a répondu avec un snapshot technique illisible.',
    unavailableUnexpected: 'Le provider SSR a rencontré une erreur inattendue.',
    tabSummary: 'Résumé',
    tabCache: 'Cache',
    tabRendering: 'Rendu',
    tabSeo: 'SEO robots',
    tabConfig: 'Config',
    lastRefresh: 'Snapshot généré',
    startedAt: 'Fenêtre commencée',
    uptime: 'Durée fenêtre',
    build: 'Version',
    hitRate: 'Hit rate',
    hitRateHelp: 'Part des pages HTML servies directement depuis le cache SSR, y compris le cache stale encore utilisable.',
    robotHitRate: 'Hit rate robots',
    robotHitRateHelp: 'Part des pages demandées par les robots identifiables et servies depuis le cache.',
    pageResponses: 'Pages vues SSR',
    pageResponsesHelp: 'Nombre de réponses HTML traitées par le serveur SSR depuis le dernier démarrage.',
    renders: 'Rendus SSR',
    rendersHelp: 'Nombre de rendus Angular SSR réels exécutés après un cache miss ou un warmup.',
    cacheStatusTitle: 'Répartition des statuts cache',
    robotsTitle: 'Robots détectés',
    noData: 'Aucune donnée pour le moment.',
    storageTitle: 'Stockage cache',
    memory: 'Mémoire',
    disk: 'Disque',
    technicalStatsStorage: 'Stats persistantes',
    technicalStatsStorageHelp: 'Buckets journaliers conservés dans le volume SSR. Les fichiers plus vieux que la rétention sont purgés automatiquement.',
    seoDocs: 'SEO docs',
    diskWrites: 'Écritures disque',
    assetMisses: 'Assets 404',
    renderingTitle: 'Rendu SSR',
    activeQueued: 'Actifs / file',
    concurrency: 'Concurrence',
    averageRender: 'Rendu moyen',
    maxRender: 'Rendu max',
    slowRenders: 'Rendus lents',
    queueFull: 'File pleine',
    activeQueuedHelp: 'Rendus SSR en cours et rendus en attente dans la file interne.',
    concurrencyHelp: 'Maximum configuré de rendus simultanés et maximum de rendus en file avant fallback CSR.',
    averageRenderHelp: 'Durée moyenne des rendus Angular SSR terminés depuis le démarrage du processus.',
    maxRenderHelp: 'Rendu Angular SSR le plus lent observé depuis le démarrage du processus.',
    slowRendersHelp: 'Rendus qui dépassent le seuil lent configuré.',
    queueFullHelp: 'Requêtes qui n’ont pas pu entrer dans la file de rendu SSR et ont reçu le shell CSR.',
    refreshTitle: 'Refresh ciblé',
    refreshEnabled: 'Actif',
    refreshPending: 'En attente',
    refreshDone: 'Réussis',
    refreshFailed: 'Échecs',
    invalidationTitle: 'Invalidations',
    invalidationRequests: 'Requêtes',
    invalidationTargeted: 'Ciblées',
    invalidationCleared: 'Entrées purgées',
    invalidationLast: 'Dernière purge',
    configTitle: 'Configuration',
    pageTtl: 'TTL pages',
    staleTtl: 'Stale TTL',
    htmlLimit: 'Limite HTML',
    browserCache: 'Cache-Control pages',
    retentionTitle: 'Retention stats',
    retentionHelp: 'Nombre de jours de buckets SSR conservés sur disque. Par défaut : 15 jours.',
    retentionDays: 'Jours conservés',
    persistence: 'Persistance',
    flushInterval: 'Flush stats',
    lastFlush: 'Dernier flush',
    lastCleanup: 'Dernière purge',
    save: 'Enregistrer',
    saveError: 'Impossible de sauvegarder le réglage pour le moment.',
    seoTitle: 'Stats techniques SEO',
    seoSubtitle: 'Contrôle rapide de ce que les robots reçoivent : HTML prêt, no-JS sûr, fallback bloqué et cache robots.',
    seoHtmlReady: 'HTML SEO-ready',
    seoHtmlReadyHelp: 'Part des réponses HTML dont le titre, la description, la canonical et le contenu serveur sont exploitables.',
    seoRobotHtmlReady: 'HTML robots prêt',
    seoRobotHtmlReadyHelp: 'Part des réponses HTML servies à des robots qui sont validées SEO-ready.',
    robotNoJs: 'No-JS robots',
    robotNoJsHelp: 'Réponses robots où les scripts ont été retirés après validation SEO-ready.',
    robotBlocked: 'HTML robots bloqué',
    robotBlockedHelp: 'Réponses robots où le retrait JS a été refusé parce que le HTML n’était pas SEO-ready.',
    robotUnavailable: 'SSR robot indisponible',
    robotUnavailableHelp: 'Réponses 503 envoyées aux robots quand un 200 CSR vide aurait été dangereux.',
    robotFallbackBlocked: 'Fallback non autorisé',
    robotFallbackBlockedHelp: 'Réponses robots où le fallback CSR a été conservé avec scripts parce que le no-JS n’était pas autorisé.',
    seoDocsHitRate: 'Hit rate documents SEO',
    seoDocsHitRateHelp: 'Part des robots.txt et sitemaps servis depuis le cache de documents SEO.',
    seoRobotFilters: 'Filtrer les robots',
    allRobots: 'Tous',
    googleRobots: 'Google',
    bingRobots: 'Bing',
    yandexRobots: 'Yandex',
    otherRobots: 'Autres',
    robotFamily: 'Robot',
    robotGroup: 'Groupe',
    robotRequests: 'Requêtes',
    robotCache: 'Cache',
    robotSeoReady: 'SEO-ready',
    robotNoJsShort: 'No-JS',
    robotBlockedShort: 'Bloqués',
    robotUnavailableShort: '503',
    groupGoogle: 'Google',
    groupBing: 'Bing',
    groupYandex: 'Yandex',
    groupOther: 'Autres',
    yes: 'Oui',
    no: 'Non',
    never: 'Jamais'
  },
  en: {
    nav: 'Technical stats',
    kicker: 'SSR observability',
    title: 'Technical stats',
    subtitle: 'Application SSR cache, robots, rendering and purge metrics over the retained window.',
    refresh: 'Refresh',
    loading: 'Loading technical stats...',
    errorTitle: 'Stats unavailable',
    errorMessage: 'The SSR server is not answering internal technical stats yet.',
    unavailableMissingSettings: 'The backend SSR internal configuration is incomplete: missing internal URL or shared token.',
    unavailableForbidden: 'The SSR server returned 403: the shared backend/front SSR token does not match.',
    unavailableNotFound: 'The SSR server returned 404: internal endpoint missing or SSR token not configured on the frontend.',
    unavailableTimeout: 'The SSR server did not answer within the expected timeout.',
    unavailableRequestFailed: 'The backend cannot reach the frontend SSR service on the internal Docker network.',
    unavailableEmpty: 'The SSR server answered without a usable snapshot.',
    unavailableInvalidJson: 'The SSR server returned an unreadable technical snapshot.',
    unavailableUnexpected: 'The SSR provider hit an unexpected error.',
    tabSummary: 'Summary',
    tabCache: 'Cache',
    tabRendering: 'Rendering',
    tabSeo: 'SEO robots',
    tabConfig: 'Config',
    lastRefresh: 'Snapshot generated',
    startedAt: 'Window started',
    uptime: 'Window duration',
    build: 'Version',
    hitRate: 'Hit rate',
    hitRateHelp: 'Share of HTML pages served directly from the SSR cache, including still-usable stale cache.',
    robotHitRate: 'Robot hit rate',
    robotHitRateHelp: 'Share of pages requested by identifiable robots and served from cache.',
    pageResponses: 'SSR page views',
    pageResponsesHelp: 'Number of HTML responses handled by the SSR server since the last process start.',
    renders: 'SSR renders',
    rendersHelp: 'Number of real Angular SSR renders executed after a cache miss or warmup.',
    cacheStatusTitle: 'Cache status split',
    robotsTitle: 'Detected robots',
    noData: 'No data yet.',
    storageTitle: 'Cache storage',
    memory: 'Memory',
    disk: 'Disk',
    technicalStatsStorage: 'Persistent stats',
    technicalStatsStorageHelp: 'Daily buckets kept in the SSR volume. Files older than the retention window are purged automatically.',
    seoDocs: 'SEO docs',
    diskWrites: 'Disk writes',
    assetMisses: 'Asset 404s',
    renderingTitle: 'SSR rendering',
    activeQueued: 'Active / queued',
    concurrency: 'Concurrency',
    averageRender: 'Average render',
    maxRender: 'Max render',
    slowRenders: 'Slow renders',
    queueFull: 'Queue full',
    activeQueuedHelp: 'Active SSR render jobs and pending jobs currently waiting in the in-process queue.',
    concurrencyHelp: 'Configured maximum concurrent renders and maximum queued renders before CSR fallback.',
    averageRenderHelp: 'Average duration of completed Angular SSR renders since process start.',
    maxRenderHelp: 'Slowest Angular SSR render observed since process start.',
    slowRendersHelp: 'Renders that exceed the configured slow render threshold.',
    queueFullHelp: 'Requests that could not enter the SSR render queue and received the CSR shell instead.',
    refreshTitle: 'Targeted refresh',
    refreshEnabled: 'Enabled',
    refreshPending: 'Pending',
    refreshDone: 'Succeeded',
    refreshFailed: 'Failed',
    invalidationTitle: 'Invalidations',
    invalidationRequests: 'Requests',
    invalidationTargeted: 'Targeted',
    invalidationCleared: 'Cleared entries',
    invalidationLast: 'Last purge',
    configTitle: 'Configuration',
    pageTtl: 'Page TTL',
    staleTtl: 'Stale TTL',
    htmlLimit: 'HTML limit',
    browserCache: 'Page Cache-Control',
    retentionTitle: 'Stats retention',
    retentionHelp: 'Number of SSR bucket days kept on disk. Default: 15 days.',
    retentionDays: 'Days kept',
    persistence: 'Persistence',
    flushInterval: 'Stats flush',
    lastFlush: 'Last flush',
    lastCleanup: 'Last cleanup',
    save: 'Save',
    saveError: 'Unable to save this setting right now.',
    seoTitle: 'Technical SEO stats',
    seoSubtitle: 'Fast check of what robots receive: ready HTML, safe no-JS, blocked fallback and robot cache.',
    seoHtmlReady: 'SEO-ready HTML',
    seoHtmlReadyHelp: 'Share of HTML responses whose title, description, canonical and server content are usable.',
    seoRobotHtmlReady: 'Robot-ready HTML',
    seoRobotHtmlReadyHelp: 'Share of HTML responses served to robots that passed the SEO-ready guard.',
    robotNoJs: 'Robot no-JS',
    robotNoJsHelp: 'Robot responses where scripts were removed after SEO-ready validation.',
    robotBlocked: 'Robot HTML blocked',
    robotBlockedHelp: 'Robot responses where JS removal was refused because the HTML was not SEO-ready.',
    robotUnavailable: 'Robot SSR unavailable',
    robotUnavailableHelp: '503 responses sent to robots when a blank CSR 200 would be dangerous.',
    robotFallbackBlocked: 'Fallback not allowed',
    robotFallbackBlockedHelp: 'Robot responses where CSR fallback kept scripts because no-JS was not allowed.',
    seoDocsHitRate: 'SEO documents hit rate',
    seoDocsHitRateHelp: 'Share of robots.txt and sitemap responses served from the SEO document cache.',
    seoRobotFilters: 'Filter robots',
    allRobots: 'All',
    googleRobots: 'Google',
    bingRobots: 'Bing',
    yandexRobots: 'Yandex',
    otherRobots: 'Other',
    robotFamily: 'Robot',
    robotGroup: 'Group',
    robotRequests: 'Requests',
    robotCache: 'Cache',
    robotSeoReady: 'SEO-ready',
    robotNoJsShort: 'No-JS',
    robotBlockedShort: 'Blocked',
    robotUnavailableShort: '503',
    groupGoogle: 'Google',
    groupBing: 'Bing',
    groupYandex: 'Yandex',
    groupOther: 'Other',
    yes: 'Yes',
    no: 'No',
    never: 'Never'
  }
};

const STATUS_LABELS: Record<TechnicalStatsLanguage, Record<string, string>> = {
  fr: {
    HIT: 'Cache direct',
    MISS: 'Rendu puis cache',
    WARMED: 'Warmup rempli',
    'WARMUP-HIT': 'Warmup cache',
    STALE: 'Cache stale',
    'WARMUP-STALE': 'Warmup stale',
    'SSR-UNCACHED': 'SSR non cache',
    'SSR-BOT-UNAVAILABLE': 'Bot SSR indisponible',
    'CSR-CACHE-MISS-FALLBACK': 'Fallback CSR',
    'CSR-OVERLOAD-FALLBACK': 'Fallback surcharge',
    'CSR-WARMUP-SKIPPED': 'Warmup ignore'
  },
  en: {
    HIT: 'Direct cache',
    MISS: 'Rendered then cached',
    WARMED: 'Warmup filled',
    'WARMUP-HIT': 'Warmup cache',
    STALE: 'Stale cache',
    'WARMUP-STALE': 'Warmup stale',
    'SSR-UNCACHED': 'Uncached SSR',
    'SSR-BOT-UNAVAILABLE': 'Bot SSR unavailable',
    'CSR-CACHE-MISS-FALLBACK': 'CSR fallback',
    'CSR-OVERLOAD-FALLBACK': 'Overload fallback',
    'CSR-WARMUP-SKIPPED': 'Warmup skipped'
  }
};

@Component({
  selector: 'app-admin-technical-stats',
  templateUrl: './admin-technical-stats.component.html',
  styleUrl: './admin-technical-stats.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminTechnicalStatsFacade],
  imports: [
    CommonModule
  ]
})
export class AdminTechnicalStatsComponent implements OnInit {
  protected readonly tabOptions: readonly TechnicalStatsTabOption[] = [
    { key: 'summary', labelKey: 'tabSummary' },
    { key: 'seo', labelKey: 'tabSeo' },
    { key: 'cache', labelKey: 'tabCache' },
    { key: 'rendering', labelKey: 'tabRendering' },
    { key: 'config', labelKey: 'tabConfig' }
  ];
  protected readonly robotFilterOptions: readonly RobotFamilyFilterOption[] = [
    { key: 'all', labelKey: 'allRobots' },
    { key: 'bing', labelKey: 'bingRobots' },
    { key: 'google', labelKey: 'googleRobots' },
    { key: 'yandex', labelKey: 'yandexRobots' },
    { key: 'other', labelKey: 'otherRobots' }
  ];
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly stats = this.facade.stats;
  protected readonly settingsSaving = this.facade.settingsSaving;
  protected readonly settingsError = this.facade.settingsError;
  protected readonly hitRatePercent = this.facade.hitRatePercent;
  protected readonly robotHitRatePercent = this.facade.robotHitRatePercent;
  protected readonly hasUnavailableStats = computed(() => (this.state().kind === 'error' || this.stats()?.isAvailable === false) && !this.loading());
  protected readonly retentionDaysDraft = signal<number | null>(null);
  protected readonly activeTab = signal<TechnicalStatsTab>('summary');
  protected readonly robotFamilyFilter = signal<RobotFamilyFilter>('all');
  protected readonly visibleStatuses = computed(() => this.activeTab() === 'cache' ? this.limitRows(this.stats()?.cache.statuses ?? []) : []);
  protected readonly visibleRobotFamilies = computed(() => this.activeTab() === 'cache' ? this.limitRows(this.stats()?.cache.robotFamilies ?? []) : []);
  protected readonly visibleSeoRobotFamilies = computed(() => {
    if (this.activeTab() !== 'seo') {
      return [];
    }

    const stats: TechnicalStatsSnapshot | null = this.stats();
    const filter: RobotFamilyFilter = this.robotFamilyFilter();
    const families: TechnicalStatsRobotFamily[] = stats?.cache.robotFamilies ?? [];
    if (filter === 'all') {
      return this.limitRows(families);
    }

    return this.limitRows(families.filter((family: TechnicalStatsRobotFamily): boolean => family.category === filter));
  });
  protected readonly renderingMetricRows = computed((): TechnicalStatsMetricRow[] => {
    if (this.activeTab() !== 'rendering') {
      return [];
    }

    const stats: TechnicalStatsSnapshot | null = this.stats();
    if (stats === null) {
      return [];
    }

    return [
      {
        label: this.t('activeQueued'),
        value: `${stats.rendering.activeRenders} / ${stats.rendering.queuedRenders}`,
        help: this.t('activeQueuedHelp')
      },
      {
        label: this.t('concurrency'),
        value: `${stats.rendering.maxConcurrency} / ${stats.rendering.maxQueueEntries}`,
        help: this.t('concurrencyHelp')
      },
      {
        label: this.t('averageRender'),
        value: this.formatMilliseconds(stats.rendering.averageRenderMilliseconds),
        help: this.t('averageRenderHelp')
      },
      {
        label: this.t('maxRender'),
        value: this.formatMilliseconds(stats.rendering.maxRenderMilliseconds),
        help: this.t('maxRenderHelp')
      },
      {
        label: this.t('slowRenders'),
        value: `${stats.rendering.slowRenders}`,
        help: `${this.t('slowRendersHelp')} (${stats.rendering.slowRenderThresholdMilliseconds} ms).`
      },
      {
        label: this.t('queueFull'),
        value: `${stats.rendering.queueFullRejections}`,
        help: this.t('queueFullHelp')
      }
    ];
  });
  protected readonly seoMetricRows = computed((): TechnicalStatsMetricRow[] => {
    if (this.activeTab() !== 'seo') {
      return [];
    }

    const stats: TechnicalStatsSnapshot | null = this.stats();
    if (stats === null) {
      return [];
    }

    return [
      {
        label: this.t('seoHtmlReady'),
        value: `${stats.seo.seoReadyRatePercent.toFixed(1)}% (${stats.seo.seoReadyHtmlResponses} / ${stats.seo.htmlResponses})`,
        help: this.t('seoHtmlReadyHelp')
      },
      {
        label: this.t('seoRobotHtmlReady'),
        value: `${stats.seo.robotSeoReadyRatePercent.toFixed(1)}% (${stats.seo.robotSeoReadyHtmlResponses} / ${stats.seo.robotHtmlResponses})`,
        help: this.t('seoRobotHtmlReadyHelp')
      },
      {
        label: this.t('robotNoJs'),
        value: `${stats.seo.robotNoJsHtmlResponses}`,
        help: this.t('robotNoJsHelp')
      },
      {
        label: this.t('robotBlocked'),
        value: `${stats.seo.robotHtmlBlockedNotSeoReady}`,
        help: this.t('robotBlockedHelp')
      },
      {
        label: this.t('robotUnavailable'),
        value: `${stats.seo.robotSsrUnavailableResponses}`,
        help: this.t('robotUnavailableHelp')
      },
      {
        label: this.t('robotFallbackBlocked'),
        value: `${stats.seo.robotHtmlNotAllowed}`,
        help: this.t('robotFallbackBlockedHelp')
      },
      {
        label: this.t('seoDocsHitRate'),
        value: `${stats.seo.seoDocumentHitRatePercent.toFixed(1)}% (${stats.seo.seoDocumentHits} / ${stats.seo.seoDocumentRequests})`,
        help: this.t('seoDocsHitRateHelp')
      },
      {
        label: this.t('queueFull'),
        value: `${stats.seo.queueFullRejections}`,
        help: this.t('queueFullHelp')
      }
    ];
  });

  constructor(
    private readonly facade: AdminTechnicalStatsFacade,
    private readonly router: Router
  ) {
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected refresh(): void {
    this.facade.load();
  }

  protected setActiveTab(tab: TechnicalStatsTab): void {
    this.activeTab.set(tab);
  }

  protected setRobotFamilyFilter(filter: RobotFamilyFilter): void {
    this.robotFamilyFilter.set(filter);
  }

  protected saveRetentionDays(event: Event, stats: TechnicalStatsSnapshot): void {
    event.preventDefault();
    const retentionDays = this.clampRetentionDays(this.currentRetentionDays(stats));
    this.retentionDaysDraft.set(retentionDays);
    this.facade.updateSettings({ persistenceRetentionDays: retentionDays });
  }

  protected setRetentionDaysDraft(value: number | string): void {
    this.retentionDaysDraft.set(this.clampRetentionDays(value));
  }

  protected readInputValue(event: Event): string {
    return event.target instanceof HTMLInputElement ? event.target.value : '';
  }

  protected currentRetentionDays(stats: TechnicalStatsSnapshot): number {
    return this.retentionDaysDraft() ?? stats.config.technicalStatsPersistenceRetentionDays;
  }

  protected t(key: string): string {
    return COPY[this.currentLanguage][key] ?? COPY.en[key] ?? key;
  }

  protected statusLabel(status: TechnicalStatsCount): string {
    return STATUS_LABELS[this.currentLanguage][status.key] ?? STATUS_LABELS.en[status.key] ?? status.key;
  }

  protected robotCategoryLabel(category: string): string {
    switch (category) {
      case 'google':
        return this.t('groupGoogle');
      case 'bing':
        return this.t('groupBing');
      case 'yandex':
        return this.t('groupYandex');
      default:
        return this.t('groupOther');
    }
  }

  protected barWidth(percent: number): number {
    const clampedPercent: number = this.clampPercent(percent);
    return clampedPercent > 0 ? Math.max(4, clampedPercent) : 0;
  }

  protected storagePercent(value: number, total: number): number {
    return this.clampPercent(total > 0 ? (value / total) * 100 : 0);
  }

  protected formatBytes(bytes: number): string {
    if (bytes <= 0) {
      return '0 B';
    }

    const units: string[] = ['B', 'KB', 'MB', 'GB'];
    const exponent: number = Math.min(units.length - 1, Math.floor(Math.log(bytes) / Math.log(1024)));
    const value: number = bytes / Math.pow(1024, exponent);
    return `${new Intl.NumberFormat(this.currentLanguage, { maximumFractionDigits: exponent === 0 ? 0 : 1 }).format(value)} ${units[exponent]}`;
  }

  protected formatDuration(seconds: number): string {
    const safeSeconds: number = Math.max(0, Math.round(seconds));
    const days: number = Math.floor(safeSeconds / 86400);
    const hours: number = Math.floor((safeSeconds % 86400) / 3600);
    const minutes: number = Math.floor((safeSeconds % 3600) / 60);

    if (days > 0) {
      return `${days}d ${hours}h`;
    }

    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    }

    return `${minutes}m`;
  }

  protected formatMilliseconds(value: number): string {
    return `${Math.max(0, Math.round(value))} ms`;
  }

  protected boolLabel(value: boolean): string {
    return value ? this.t('yes') : this.t('no');
  }

  protected unavailableMessage(stats: TechnicalStatsSnapshot | null): string {
    const reason: string = stats?.unavailableReason?.trim() ?? '';
    if (reason.length === 0) {
      return this.t('errorMessage');
    }

    switch (reason) {
      case 'missing-settings':
        return this.t('unavailableMissingSettings');
      case 'http-403':
        return this.t('unavailableForbidden');
      case 'http-404':
        return this.t('unavailableNotFound');
      case 'timeout':
        return this.t('unavailableTimeout');
      case 'request-failed':
        return this.t('unavailableRequestFailed');
      case 'empty-response':
        return this.t('unavailableEmpty');
      case 'invalid-json':
        return this.t('unavailableInvalidJson');
      case 'unexpected-error':
        return this.t('unavailableUnexpected');
      default:
        return `${this.t('errorMessage')} (${reason})`;
    }
  }

  protected trackStatus(_: number, item: TechnicalStatsCount): string {
    return item.key;
  }

  protected trackRobot(_: number, item: TechnicalStatsRobotFamily): string {
    return item.key;
  }

  protected trackMetric(_: number, item: TechnicalStatsMetricRow): string {
    return item.label;
  }

  protected trackTab(_: number, item: TechnicalStatsTabOption): string {
    return item.key;
  }

  protected trackRobotFilter(_: number, item: RobotFamilyFilterOption): string {
    return item.key;
  }

  protected get currentLanguage(): TechnicalStatsLanguage {
    return this.router.url.split('/')[1] === 'fr' ? 'fr' : 'en';
  }

  private clampPercent(value: number): number {
    if (!Number.isFinite(value)) {
      return 0;
    }

    return Math.min(100, Math.max(0, value));
  }

  private clampRetentionDays(value: number | string): number {
    const parsed = Number(value);
    if (!Number.isFinite(parsed)) {
      return 15;
    }

    return Math.min(365, Math.max(1, Math.round(parsed)));
  }

  private limitRows<TRow>(rows: TRow[]): TRow[] {
    return rows.length > MAX_DISTRIBUTION_ROWS ? rows.slice(0, MAX_DISTRIBUTION_ROWS) : rows;
  }
}
