import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { skip } from 'rxjs/operators';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { resolveLocalizedText, stripHtml } from '@shared/utils/localization/localized-text.helpers';
import { buildPublicParkReferenceRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { findNearestLanguageActivatedRoute, resolveLanguageFromActivatedRoute, resolveLanguageFromParamMap } from '@shared/utils/routing/route-language.utils';
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
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class ManufacturersPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly loading: Signal<boolean> = this.stateFacade.loading;
  protected readonly errorKey: Signal<string | null> = this.stateFacade.errorKey;
  protected readonly searchTerm: Signal<string> = this.stateFacade.searchTerm;
  protected readonly filteredManufacturers: Signal<AttractionManufacturer[]> = this.stateFacade.filteredManufacturers;
  protected readonly groupedManufacturers: Signal<PublicManufacturerGroup[]> = this.stateFacade.groupedManufacturers;

  private activeLanguage: string | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly stateFacade: PublicManufacturersStateFacade,
    private readonly translationService: TranslationService,
    private readonly seoService: SeoService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(this.route, this.translationService.getCurrentLang() || 'en');
    this.applyLanguage(initialLanguage);
    this.watchRouteLanguageChanges();

    this.translationService.languageChanged.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((language: string): void => {
      this.applyLanguage(language);
    });

    this.stateFacade.load();
  }

  protected onSearchInput(event: Event): void {
    const input: HTMLInputElement | null = event.target instanceof HTMLInputElement ? event.target : null;
    this.stateFacade.updateSearchTerm(input?.value ?? '');
  }

  protected clearSearch(): void {
    this.stateFacade.clearSearch();
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
    this.seoService.applyRouteDefaults(this.router.url);
  }
}
