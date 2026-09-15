import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, effect, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { Subject } from 'rxjs';
import { debounceTime, filter, skip } from 'rxjs/operators';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PaginationContract } from '@shared/models/contracts';
import { LocalizedPluralPipe } from '@shared/pipes';
import { resolveLocalizedText, stripHtml } from '@shared/utils/localization/localized-text.helpers';
import { buildPublicParkReferenceRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
import { PUBLIC_MANUFACTURERS_PAGE_SIZE, PublicDirectoryLocation, buildPublicDirectoryPagePath, resolvePublicDirectoryLocation } from '@shared/utils/routing/public-directory-location';
import { resolvePaginationLabels } from '@shared/utils/pagination/pagination-labels';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicManufacturerGroup, PublicManufacturersStateFacade } from '../state/public-manufacturers-state.facade';

@Component({
  selector: 'app-manufacturers-page',
  templateUrl: './manufacturers-page.component.html',
  styleUrls: ['./manufacturers-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicManufacturersStateFacade],
  imports: [
    RouterLink,
    TranslateModule,
    ImageDisplayComponent,
    PaginationComponent,
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective,
    LocalizedPluralPipe
  ]
})
export class ManufacturersPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly errorKey: Signal<string | null> = this.stateFacade.errorKey;
  protected readonly searchTerm: Signal<string> = this.stateFacade.searchTerm;
  protected readonly filteredManufacturers: Signal<AttractionManufacturer[]> = this.stateFacade.filteredManufacturers;
  protected readonly groupedManufacturers: Signal<PublicManufacturerGroup[]> = this.stateFacade.groupedManufacturers;
  protected readonly pagination: Signal<PaginationContract | null> = this.stateFacade.pagination;
  protected readonly currentPage: Signal<number> = this.stateFacade.currentPage;
  protected readonly pageSize: Signal<number> = this.stateFacade.pageSize;
  protected readonly totalCount: Signal<number> = this.stateFacade.totalCount;

  private readonly location = signal<PublicDirectoryLocation>({ page: 1, isValid: true, isIndexable: true });
  protected readonly pageLabel = computed(() => `${resolvePaginationLabels(this.currentLang()).page} ${this.currentPage()}`);
  protected readonly pageHref = computed<((page: number) => string) | null>(() => {
    const language: string = this.currentLang();
    return !this.searchTerm() && this.pageSize() === PUBLIC_MANUFACTURERS_PAGE_SIZE
      && this.location().isIndexable && this.stateFacade.resolvedPage() === this.location().page
      ? (page: number): string => buildPublicDirectoryPagePath(language, 'manufacturers', page + 1)
      : null;
  });
  private searchGeneration = 0;
  private activeLanguage: string | null = null;
  private readonly searchInputSubject = new Subject<number>();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: PublicManufacturersStateFacade,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
    effect(() => this.applyResolvedPageSeo());
  }

  ngOnInit(): void {
    this.location.set(resolvePublicDirectoryLocation(this.route.snapshot.queryParamMap));
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.applyLanguage(initialLanguage);
    this.watchRouteLanguageChanges();
    this.router.events.pipe(filter(event => event instanceof NavigationEnd), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyResolvedPageSeo());
    this.route.queryParamMap.pipe(skip(1), takeUntilDestroyed(this.destroyRef)).subscribe((params: ParamMap) => {
      this.location.set(resolvePublicDirectoryLocation(params));
      const navigationInfo: unknown = this.router.getCurrentNavigation()?.extras.info;
      if (navigationInfo && typeof navigationInfo === 'object' && 'manufacturersPreserveFilters' in navigationInfo
        && navigationInfo.manufacturersPreserveFilters === true) {
        return;
      }
      ++this.searchGeneration;
      this.stateFacade.clearSearch();
      this.loadLocation();
    });

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.applyLanguage(language);
    });

    this.searchInputSubject.pipe(
      debounceTime(250),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((generation: number): void => {
      if (generation !== this.searchGeneration) {
        return;
      }
      this.stateFacade.load(1, this.pageSize());
    });

    this.loadLocation();
  }

  onSearchInput(event: Event): void {
    const input: HTMLInputElement | null = event.target instanceof HTMLInputElement ? event.target : null;
    const value: string = input?.value ?? '';
    this.stateFacade.updateSearchTerm(value);
    this.clearPageQueryForClientState();
    this.searchInputSubject.next(++this.searchGeneration);
  }

  clearSearch(): void {
    this.stateFacade.clearSearch();
    this.clearPageQueryForClientState();
    this.searchInputSubject.next(++this.searchGeneration);
  }

  onPageChanged(event: { page?: number; rows?: number }): void {
    const page: number = (event.page ?? 0) + 1;
    const rows: number = event.rows ?? this.pageSize();
    ++this.searchGeneration;
    if (rows === PUBLIC_MANUFACTURERS_PAGE_SIZE && !this.searchTerm() && this.location().isIndexable) {
      if (this.location().page === page) {
        this.stateFacade.load(page, rows);
      } else {
        void this.router.navigate([], { relativeTo: this.route, queryParams: page > 1 ? { page } : {} });
      }
      return;
    }
    this.clearPageQueryForClientState();
    this.stateFacade.setPage(page, rows);
  }

  protected manufacturerRoute(manufacturer: AttractionManufacturer): string[] | null {
    return buildPublicParkReferenceRouteCommands({
      language: this.currentLang(),
      referenceId: manufacturer.id,
      referenceName: manufacturer.name,
      kind: 'manufacturer'
    });
  }

  protected biographyPreview(manufacturer: AttractionManufacturer): string | null {
    const text: string = stripHtml(resolveLocalizedText(manufacturer.biography, this.currentLang(), ''));
    if (!text) {
      return null;
    }

    return text.length > 180 ? `${text.slice(0, 177).trim()}...` : text;
  }

  protected locationLine(manufacturer: AttractionManufacturer): string | null {
    const parts: string[] = [
      manufacturer.contactDetails?.city ?? null,
      manufacturer.contactDetails?.countryCode ?? null
    ].filter((value: string | null): value is string => Boolean(value));

    return parts.length > 0 ? parts.join(', ') : null;
  }

  protected activityYears(manufacturer: AttractionManufacturer): string | null {
    if (!manufacturer.foundedYear && !manufacturer.closedYear) {
      return null;
    }

    return `${manufacturer.foundedYear ?? '?'} - ${manufacturer.closedYear ?? '...'}`;
  }

  protected websiteHost(manufacturer: AttractionManufacturer): string | null {
    const websiteUrl: string | null | undefined = manufacturer.contactDetails?.websiteUrl;
    if (!websiteUrl) {
      return null;
    }

    try {
      return new URL(websiteUrl).hostname.replace(/^www\./i, '');
    } catch {
      return websiteUrl.replace(/^https?:\/\//i, '').replace(/^www\./i, '').split('/')[0] || websiteUrl;
    }
  }

  protected manufacturerImageId(manufacturer: AttractionManufacturer): string | null {
    return manufacturer.mainImageId ?? manufacturer.currentLogoImageId ?? null;
  }

  private watchRouteLanguageChanges(): void {
    const languageRoute: ActivatedRoute | null = findNearestLanguageActivatedRoute(this.route);

    languageRoute?.paramMap.pipe(
      skip(1),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((params: ParamMap): void => {
      this.applyLanguage(resolveLanguageFromParamMap(params, this.currentLang()));
    });
  }

  private applyLanguage(language: string): void {
    if (this.activeLanguage === language) {
      return;
    }

    this.activeLanguage = language;
    this.currentLang.set(language);
    this.applyResolvedPageSeo();
  }

  private loadLocation(): void {
    this.seoService.applyManufacturersListSeo(this.currentLang(), this.router.url);
    if (!this.location().isValid) {
      this.stateFacade.rejectInvalidPage();
      return;
    }
    this.stateFacade.load(this.location().page, PUBLIC_MANUFACTURERS_PAGE_SIZE);
  }

  private clearPageQueryForClientState(): void {
    if (this.location().page === 1 && this.location().isValid) {
      return;
    }
    this.location.update(location => ({ ...location, page: 1, isValid: true }));
    void this.router.navigate([], {
      relativeTo: this.route, queryParams: { page: null }, queryParamsHandling: 'merge', replaceUrl: true,
      info: { manufacturersPreserveFilters: true }
    });
  }

  private applyResolvedPageSeo(): void {
    const language: string = this.currentLang();
    const path: string = this.router.url.split(/[?#]/)[0];
    if (path !== `/${language}/manufacturers` && path !== `/${language}/manufacturers/`) {
      return;
    }
    const actual = resolvePublicDirectoryLocation(this.router.parseUrl(this.router.url).queryParamMap);
    const resolved: number | null = this.stateFacade.resolvedPage();
    const page: number | null = actual.isValid && !this.searchTerm() && this.pageSize() === PUBLIC_MANUFACTURERS_PAGE_SIZE
      && resolved === actual.page ? actual.page : null;
    this.seoService.applyManufacturersListSeo(language, this.router.url, page);
  }

}
