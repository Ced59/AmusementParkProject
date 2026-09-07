import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  SharedVisitRecap,
  SharedVisitRecapContent,
  SharedVisitRecapItem
} from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { UiButtonDirective } from '@ui/primitives';
import { SharedVisitRecapStateFacade } from '../state/shared-visit-recap-state.facade';

@Component({
  selector: 'app-shared-visit-recap-page',
  templateUrl: './shared-visit-recap-page.component.html',
  styleUrl: './shared-visit-recap-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SharedVisitRecapStateFacade],
  imports: [PublicSharePanelComponent, RouterLink, TranslateModule, UiButtonDirective]
})
export class SharedVisitRecapPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly shareId = signal<string>('');
  protected readonly result: Signal<SharedVisitRecap | null> = this.facade.recap;
  protected readonly loading: Signal<boolean> = this.facade.loading;
  protected readonly notFound: Signal<boolean> = this.facade.notFound;
  protected readonly error: Signal<boolean> = this.facade.error;
  protected readonly parkRoute: Signal<string[] | null> = computed(() => {
    const recap: SharedVisitRecapContent | undefined = this.result()?.visitRecap;
    return recap
      ? buildPublicParkRouteCommands({
        language: this.currentLang(),
        parkId: recap.parkId,
        parkName: recap.parkName
      })
      : null;
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: SharedVisitRecapStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const currentResult: SharedVisitRecap | null = this.result();
      if (currentResult) {
        this.applySeo(currentResult.visitRecap);
      }

      if (this.notFound()) {
        this.ssrHttpStatusService.setNotFound();
        this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);
      }
    });
  }

  ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    const shareId: string = this.route.snapshot.paramMap.get('shareId')?.trim() ?? '';
    this.currentLang.set(language);
    this.shareId.set(shareId);
    this.seoService.applyRouteDefaults(this.router.url);

    if (shareId.length === 0) {
      this.ssrHttpStatusService.setNotFound();
      this.seoService.applyNotFoundSeo(language, this.router.url);
      return;
    }

    this.facade.load(shareId);
    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((lang: string): void => {
      this.currentLang.set(lang);
      const currentResult: SharedVisitRecap | null = this.result();
      if (currentResult) {
        this.applySeo(currentResult.visitRecap);
      }
    });
  }

  protected formatDate(recap: SharedVisitRecapContent): string {
    if (!recap.date) {
      return '';
    }

    const date: Date = new Date(Date.UTC(recap.date.year, (recap.date.month ?? 1) - 1, recap.date.day ?? 1));
    const options: Intl.DateTimeFormatOptions = recap.date.precision === 'Year'
      ? { year: 'numeric', timeZone: 'UTC' }
      : recap.date.precision === 'Month'
        ? { month: 'long', year: 'numeric', timeZone: 'UTC' }
        : { day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC' };
    return new Intl.DateTimeFormat(this.currentLang(), options).format(date);
  }

  protected formatRating(value: number | null | undefined): string {
    return value == null
      ? '–'
      : new Intl.NumberFormat(this.currentLang(), {
        minimumFractionDigits: 1,
        maximumFractionDigits: 1
      }).format(value);
  }

  protected itemCategoryKey(item: SharedVisitRecapItem): string {
    return item.category ? `ratings.categories.${item.category}` : 'visitRecapShare.preview.unknownCategory';
  }

  private applySeo(recap: SharedVisitRecapContent): void {
    const parkName: string = recap.parkName?.trim()
      || (this.translateService.instant('visitRecapShare.preview.unknownPark') as string);
    const params: Record<string, string> = { park: parkName };
    const title: string = this.translateService.instant('visitRecapShare.public.seoTitle', params);
    const description: string = this.translateService.instant('visitRecapShare.public.seoDescription', params);
    const currentUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(this.router.url);
    const homePath: string = `/${this.currentLang()}/home`;
    const parkCommands: string[] | null = this.parkRoute();
    const parkPath: string | null = parkCommands ? this.routePath(parkCommands) : null;
    const breadcrumbItems: Array<Record<string, unknown>> = [
      {
        '@type': 'ListItem',
        position: 1,
        name: this.translateService.instant('visitRecapShare.public.breadcrumbHome'),
        item: this.canonicalUrlService.buildAbsoluteUrl(homePath)
      }
    ];
    if (parkPath) {
      breadcrumbItems.push({
        '@type': 'ListItem',
        position: 2,
        name: parkName,
        item: this.canonicalUrlService.buildAbsoluteUrl(parkPath)
      });
    }
    breadcrumbItems.push({
      '@type': 'ListItem',
      position: breadcrumbItems.length + 1,
      name: this.translateService.instant('visitRecapShare.public.breadcrumbRecap'),
      item: currentUrl
    });
    this.seoService.applySharedVisitRecapSeo(
      title,
      description,
      this.router.url,
      parkName,
      [{ '@context': 'https://schema.org', '@type': 'BreadcrumbList', itemListElement: breadcrumbItems }]
    );
  }

  private routePath(commands: string[]): string {
    return `/${commands.filter((value: string): boolean => value !== '/').join('/')}`;
  }
}
