import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { CanonicalUrlService } from '@core/seo/canonical-url.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { SharedPassportProfileStateFacade } from '../state/shared-passport-profile-state.facade';

@Component({
  selector: 'app-shared-passport-profile-page',
  templateUrl: './shared-passport-profile-page.component.html',
  styleUrl: './shared-passport-profile-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SharedPassportProfileStateFacade],
  imports: [TranslateModule, RouterLink, UiButtonDirective, PublicSharePanelComponent]
})
export class SharedPassportProfilePageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly shareId = signal<string>('');
  protected readonly result: Signal<SharedPassportProfile | null> = this.facade.profile;
  protected readonly notFound: Signal<boolean> = this.facade.notFound;
  protected readonly error: Signal<boolean> = this.facade.error;
  protected readonly avatarUrl: Signal<string | null> = computed((): string | null =>
    this.imagesApiService.resolveImageUrl(this.result()?.passportProfile.avatarUrl)
  );

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: SharedPassportProfileStateFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly canonicalUrlService: CanonicalUrlService,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly imagesApiService: ImagesApiService,
    private readonly destroyRef: DestroyRef
  ) {
    effect((): void => {
      const shared: SharedPassportProfile | null = this.result();
      if (shared) {
        this.applySeo(shared);
      }
      if (this.notFound()) {
        this.ssrHttpStatusService.setNotFound();
        this.seoService.applyNotFoundSeo(this.currentLang(), this.router.url);
      }
    });
  }

  public ngOnInit(): void {
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
      const shared: SharedPassportProfile | null = this.result();
      if (shared) {
        this.applySeo(shared);
      }
    });
  }

  protected formatRating(value: number | null | undefined): string {
    return value == null ? '–' : new Intl.NumberFormat(this.currentLang(), {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(value);
  }

  protected countryName(countryCode: string): string {
    try {
      return new Intl.DisplayNames([this.currentLang()], { type: 'region' }).of(countryCode) ?? countryCode;
    } catch {
      return countryCode;
    }
  }

  private applySeo(shared: SharedPassportProfile): void {
    const displayName: string = shared.passportProfile.displayName
      || this.translateService.instant('passportProfileShare.public.anonymous');
    const params: Record<string, string> = { name: displayName };
    const title: string = this.translateService.instant('passportProfileShare.public.seoTitle', params);
    const description: string = this.translateService.instant('passportProfileShare.public.seoDescription', params);
    const currentUrl: string = this.canonicalUrlService.buildCanonicalFromCurrentUrl(this.router.url);
    const homePath: string = `/${this.currentLang()}/home`;
    const breadcrumbs: unknown[] = [{
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      itemListElement: [
        { '@type': 'ListItem', position: 1, name: this.translateService.instant('passportProfileShare.public.home'), item: this.canonicalUrlService.buildAbsoluteUrl(homePath) },
        { '@type': 'ListItem', position: 2, name: this.translateService.instant('passportProfileShare.public.breadcrumb', params), item: currentUrl }
      ]
    }];
    this.seoService.applySharedVisitRecapSeo(title, description, this.router.url, title, breadcrumbs);
  }
}
