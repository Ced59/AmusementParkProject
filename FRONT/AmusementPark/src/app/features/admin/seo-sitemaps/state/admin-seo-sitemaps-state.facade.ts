import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, switchMap, takeWhile } from 'rxjs/operators';
import { forkJoin, timer } from 'rxjs';

import {
  GenerateSeoSitemapRequest,
  SeoSitemapGenerationHistory,
  SeoSitemapGenerationResult,
  SeoSitemapOverview,
  SeoSitemapSettings,
  SeoSsrPrerenderProgress,
  UpdateSeoSitemapSettingsRequest
} from '@app/models/admin/seo/seo-sitemap.models';
import { SeoSsrPrerenderService } from '@app/data-access/admin/seo-ssr-prerender.service';
import { PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  ADMIN_SEO_SITEMAPS_STATE__PORT,
  AdminSeoSitemapsStatePort
} from './admin-seo-sitemaps-state-data.ports';
interface AdminSeoSitemapsViewModel {
  overview: SeoSitemapOverview | null;
  history: SeoSitemapGenerationHistory[];
  pagination: PaginationContract | null;
  lastGeneration: SeoSitemapGenerationResult | null;
}

@Injectable()
export class AdminSeoSitemapsStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminSeoSitemapsViewModel>();
  private readonly currentPageSignal = signal(1);
  private readonly pageSizeSignal = signal(10);
  private readonly savingSettingsSignal = signal(false);
  private readonly generatingSignal = signal(false);
  private readonly prerenderingSignal = signal(false);
  private readonly prerenderProgressSignal = signal<SeoSsrPrerenderProgress | null>(null);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly overview: Signal<SeoSitemapOverview | null> = computed(() => this.screenStateStore.data()?.overview ?? null);
  public readonly history: Signal<SeoSitemapGenerationHistory[]> = computed(() => this.screenStateStore.data()?.history ?? []);
  public readonly pagination: Signal<PaginationContract | null> = computed(() => this.screenStateStore.data()?.pagination ?? null);
  public readonly totalRecords = computed(() => this.pagination()?.totalItems ?? this.history().length);
  public readonly currentPage = this.currentPageSignal.asReadonly();
  public readonly pageSize = this.pageSizeSignal.asReadonly();
  public readonly savingSettings = this.savingSettingsSignal.asReadonly();
  public readonly generating = this.generatingSignal.asReadonly();
  public readonly prerendering = this.prerenderingSignal.asReadonly();
  public readonly prerenderProgress = this.prerenderProgressSignal.asReadonly();
  public readonly lastGeneration: Signal<SeoSitemapGenerationResult | null> = computed(() => this.screenStateStore.data()?.lastGeneration ?? null);

  constructor(
    @Inject(ADMIN_SEO_SITEMAPS_STATE__PORT) private readonly apiService: AdminSeoSitemapsStatePort,
    private readonly destroyRef: DestroyRef,
    private readonly prerenderService: SeoSsrPrerenderService
  ) {
  }

  load(page: number = this.currentPageSignal(), size: number = this.pageSizeSignal()): void {
    const previousData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data();
    this.currentPageSignal.set(page);
    this.pageSizeSignal.set(size);
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      overview: this.apiService.getOverview(),
      history: this.apiService.getHistory(page, size)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => {
        const viewModel: AdminSeoSitemapsViewModel = {
          overview: response.overview,
          history: response.history.data ?? [],
          pagination: response.history.pagination ?? null,
          lastGeneration: previousData?.lastGeneration ?? null
        };

        this.screenStateStore.setReady(viewModel);
      },
      error: (error: unknown) => {
        console.error('Error loading SEO sitemap admin data', error);
        this.screenStateStore.setError('admin.seoSitemaps.loadError', previousData);
      }
    });
  }

  saveSettings(request: UpdateSeoSitemapSettingsRequest): void {
    const previousData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data();
    this.savingSettingsSignal.set(true);

    this.apiService.updateSettings(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (settings: SeoSitemapSettings) => {
        this.savingSettingsSignal.set(false);
        const currentData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data() ?? previousData;
        if (currentData?.overview) {
          this.screenStateStore.setReady({
            ...currentData,
            overview: {
              ...currentData.overview,
              settings
            }
          });
        }
        this.load(this.currentPageSignal(), this.pageSizeSignal());
      },
      error: (error: unknown) => {
        console.error('Error saving SEO sitemap settings', error);
        this.savingSettingsSignal.set(false);
        this.screenStateStore.setError('admin.seoSitemaps.saveError', previousData);
      }
    });
  }

  generate(submitToIndexNow: boolean): void {
    const previousData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data();
    const request: GenerateSeoSitemapRequest = { submitToIndexNow };
    this.generatingSignal.set(true);
    this.startRuntimePolling();

    this.apiService.generate(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (generation: SeoSitemapGenerationResult) => {
        this.generatingSignal.set(false);
        const currentData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data() ?? previousData;
        if (currentData) {
          this.screenStateStore.setReady({
            ...currentData,
            lastGeneration: generation
          });
        }
        this.load(1, this.pageSizeSignal());
      },
      error: (error: unknown) => {
        console.error('Error generating SEO sitemap', error);
        this.generatingSignal.set(false);
        this.screenStateStore.setError('admin.seoSitemaps.generateError', previousData);
      }
    });
  }


  generateSeoSsr(submitToIndexNow: boolean): void {
    const previousData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data();
    const sitemapIndexUrl: string | null = previousData?.overview?.sitemapIndexUrl ?? null;

    if (!sitemapIndexUrl) {
      this.screenStateStore.setError('admin.seoSitemaps.prerender.missingSitemap', previousData);
      return;
    }

    const request: GenerateSeoSitemapRequest = { submitToIndexNow };
    this.generatingSignal.set(true);
    this.prerenderingSignal.set(false);
    this.prerenderProgressSignal.set(null);
    this.startRuntimePolling();

    this.apiService.generate(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (generation: SeoSitemapGenerationResult) => {
        this.generatingSignal.set(false);
        const currentData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data() ?? previousData;
        if (currentData) {
          this.screenStateStore.setReady({
            ...currentData,
            lastGeneration: generation
          });
        }

        void this.prerenderSeoPagesAsync(sitemapIndexUrl);
      },
      error: (error: unknown) => {
        console.error('Error generating SEO sitemap before SSR prerender', error);
        this.generatingSignal.set(false);
        this.prerenderingSignal.set(false);
        this.screenStateStore.setError('admin.seoSitemaps.generateError', previousData);
      }
    });
  }

  private async prerenderSeoPagesAsync(sitemapIndexUrl: string): Promise<void> {
    this.prerenderingSignal.set(true);

    try {
      await this.prerenderService.prerenderFromSitemapIndex(
        sitemapIndexUrl,
        (progress: SeoSsrPrerenderProgress): void => this.prerenderProgressSignal.set(progress)
      );
    } catch (error: unknown) {
      console.error('Error warming SSR cache from sitemap', error);
      const currentProgress: SeoSsrPrerenderProgress | null = this.prerenderProgressSignal();
      this.prerenderProgressSignal.set({
        status: 'Failed',
        totalUrlCount: currentProgress?.totalUrlCount ?? 0,
        processedUrlCount: currentProgress?.processedUrlCount ?? 0,
        succeededUrlCount: currentProgress?.succeededUrlCount ?? 0,
        failedUrlCount: (currentProgress?.failedUrlCount ?? 0) + 1,
        currentUrl: currentProgress?.currentUrl ?? null,
        errors: [
          ...(currentProgress?.errors ?? []),
          error instanceof Error ? error.message : 'Unknown SSR prerender error'
        ].slice(-10)
      });
    } finally {
      this.prerenderingSignal.set(false);
      this.load(1, this.pageSizeSignal());
    }
  }
  private startRuntimePolling(): void {
    timer(0, 1500).pipe(
      takeUntilDestroyed(this.destroyRef),
      takeWhile(() => this.generatingSignal(), true),
      filter(() => this.generatingSignal()),
      switchMap(() => this.apiService.getOverview())
    ).subscribe({
      next: (overview: SeoSitemapOverview) => {
        const currentData: AdminSeoSitemapsViewModel | undefined = this.screenStateStore.data();
        if (!currentData) {
          return;
        }

        this.screenStateStore.setReady({
          ...currentData,
          overview
        });
      },
      error: (error: unknown) => {
        console.error('Error polling SEO sitemap runtime', error);
      }
    });
  }

}
