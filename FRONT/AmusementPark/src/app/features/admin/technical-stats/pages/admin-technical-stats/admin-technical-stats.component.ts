import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectionStrategy, Component, OnInit, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { PrimeTemplate } from 'primeng/api';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import {
  TechnicalStatsCount,
  TechnicalStatsRobotFamily,
  TechnicalStatsSnapshot
} from '@app/models/admin/technical-stats/technical-stats.models';
import { AdminTechnicalStatsFacade } from '../../state/admin-technical-stats.facade';

type TechnicalStatsLanguage = 'fr' | 'en';

const COPY: Record<TechnicalStatsLanguage, Record<string, string>> = {
  fr: {
    nav: 'Stats techniques',
    kicker: 'Observabilite SSR',
    title: 'Stats techniques',
    subtitle: 'Suivi du cache SSR applicatif, des robots, du rendu et des purges sur la fenetre retenue.',
    refresh: 'Rafraichir',
    loading: 'Chargement des stats techniques...',
    errorTitle: 'Stats indisponibles',
    errorMessage: 'Le serveur SSR ne repond pas encore aux stats techniques internes.',
    unavailableMissingSettings: 'La configuration interne SSR du back est incomplete : URL interne ou token partage absent.',
    unavailableForbidden: 'Le serveur SSR repond 403 : le token partage entre back et front SSR ne correspond pas.',
    unavailableNotFound: 'Le serveur SSR repond 404 : endpoint interne absent ou token SSR non configure cote front.',
    unavailableTimeout: 'Le serveur SSR ne repond pas dans le delai attendu.',
    unavailableRequestFailed: 'Le back ne joint pas le service front SSR sur le reseau Docker interne.',
    unavailableEmpty: 'Le serveur SSR a repondu sans snapshot exploitable.',
    unavailableUnexpected: 'Le provider SSR a rencontre une erreur inattendue.',
    lastRefresh: 'Snapshot genere',
    startedAt: 'Fenetre commencee',
    uptime: 'Duree fenetre',
    build: 'Version',
    hitRate: 'Hit rate',
    hitRateHelp: 'Part des pages HTML servies directement depuis le cache SSR, y compris le cache stale encore utilisable.',
    robotHitRate: 'Hit rate robots',
    robotHitRateHelp: 'Part des pages demandees par les robots identifiables et servies depuis le cache.',
    pageResponses: 'Pages vues SSR',
    pageResponsesHelp: 'Nombre de reponses HTML traitees par le serveur SSR depuis le dernier demarrage.',
    renders: 'Rendus SSR',
    rendersHelp: 'Nombre de rendus Angular SSR reels executes apres un cache miss ou un warmup.',
    cacheStatusTitle: 'Repartition des statuts cache',
    robotsTitle: 'Robots detectes',
    noData: 'Aucune donnee pour le moment.',
    storageTitle: 'Stockage cache',
    memory: 'Memoire',
    disk: 'Disque',
    technicalStatsStorage: 'Stats persistantes',
    technicalStatsStorageHelp: 'Buckets journaliers conserves dans le volume SSR. Les fichiers plus vieux que la retention sont purges automatiquement.',
    seoDocs: 'SEO docs',
    diskWrites: 'Ecritures disque',
    assetMisses: 'Assets 404',
    renderingTitle: 'Rendu SSR',
    activeQueued: 'Actifs / file',
    concurrency: 'Concurrence',
    averageRender: 'Rendu moyen',
    maxRender: 'Rendu max',
    slowRenders: 'Rendus lents',
    queueFull: 'File pleine',
    activeQueuedHelp: 'Rendus SSR en cours et rendus en attente dans la file interne.',
    concurrencyHelp: 'Maximum configure de rendus simultanes et maximum de rendus en file avant fallback CSR.',
    averageRenderHelp: 'Duree moyenne des rendus Angular SSR termines depuis le demarrage du processus.',
    maxRenderHelp: 'Rendu Angular SSR le plus lent observe depuis le demarrage du processus.',
    slowRendersHelp: 'Rendus qui depassent le seuil lent configure.',
    queueFullHelp: 'Requetes qui n ont pas pu entrer dans la file de rendu SSR et ont recu le shell CSR.',
    refreshTitle: 'Refresh cible',
    refreshEnabled: 'Actif',
    refreshPending: 'En attente',
    refreshDone: 'Reussis',
    refreshFailed: 'Echecs',
    invalidationTitle: 'Invalidations',
    invalidationRequests: 'Requetes',
    invalidationTargeted: 'Ciblees',
    invalidationCleared: 'Entrees purgees',
    invalidationLast: 'Derniere purge',
    configTitle: 'Configuration',
    pageTtl: 'TTL pages',
    staleTtl: 'Stale TTL',
    htmlLimit: 'Limite HTML',
    browserCache: 'Cache-Control pages',
    retentionTitle: 'Retention stats',
    retentionHelp: 'Nombre de jours de buckets SSR conserves sur disque. Par defaut : 15 jours.',
    retentionDays: 'Jours conserves',
    persistence: 'Persistance',
    flushInterval: 'Flush stats',
    lastFlush: 'Dernier flush',
    lastCleanup: 'Derniere purge',
    save: 'Enregistrer',
    saveError: 'Impossible de sauvegarder le reglage pour le moment.',
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
    unavailableUnexpected: 'The SSR provider hit an unexpected error.',
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
    CommonModule,
    FormsModule,
    ButtonDirective,
    Card,
    PrimeTemplate,
    Tag,
    Tooltip
  ]
})
export class AdminTechnicalStatsComponent implements OnInit {
  protected readonly state = this.facade.state;
  protected readonly loading = this.facade.loading;
  protected readonly stats = this.facade.stats;
  protected readonly settingsSaving = this.facade.settingsSaving;
  protected readonly settingsError = this.facade.settingsError;
  protected readonly hitRatePercent = this.facade.hitRatePercent;
  protected readonly robotHitRatePercent = this.facade.robotHitRatePercent;
  protected readonly hasUnavailableStats = computed(() => (this.state().kind === 'error' || this.stats()?.isAvailable === false) && !this.loading());
  protected readonly retentionDaysDraft = signal<number | null>(null);

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

  protected saveRetentionDays(event: Event, stats: TechnicalStatsSnapshot): void {
    event.preventDefault();
    const retentionDays = this.clampRetentionDays(this.currentRetentionDays(stats));
    this.retentionDaysDraft.set(retentionDays);
    this.facade.updateSettings({ persistenceRetentionDays: retentionDays });
  }

  protected setRetentionDaysDraft(value: number | string): void {
    this.retentionDaysDraft.set(this.clampRetentionDays(value));
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

  protected gaugeBackground(percent: number): string {
    const clampedPercent: number = this.clampPercent(percent);
    const degrees: number = Math.round((clampedPercent / 100) * 360);
    return `conic-gradient(#22c55e 0deg ${degrees}deg, #38bdf8 ${degrees}deg ${Math.min(360, degrees + 24)}deg, rgba(148, 163, 184, 0.2) ${Math.min(360, degrees + 24)}deg 360deg)`;
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

  protected trackMetric(_: number, item: { readonly label: string }): string {
    return item.label;
  }

  protected get currentLanguage(): TechnicalStatsLanguage {
    return this.router.url.split('/')[1] === 'fr' ? 'fr' : 'en';
  }

  protected renderingMetrics(stats: TechnicalStatsSnapshot): Array<{ readonly label: string; readonly value: string; readonly help: string }> {
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
}
