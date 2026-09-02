import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, Observable, of } from 'rxjs';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { TranslationService } from '@app/services/translation.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { registerSupportedAngularLocales } from '@core/i18n/supported-angular-locales';
import { JsonLdService } from '@core/seo/json-ld.service';
import { SeoService } from '@core/seo/seo.service';
import { RATING_METHODOLOGY_PORT, RatingMethodologyPort } from '../state/rating-methodology-state-data.ports';
import { RatingMethodologyPageComponent } from './rating-methodology-page.component';

class FakeRatingMethodologyPort implements RatingMethodologyPort {
  getCurrentMethodology(_options?: AnonymousHttpOptions): Observable<RatingMethodology> {
    return of(createMethodology());
  }

  getMethodology(_version: string, _options?: AnonymousHttpOptions): Observable<RatingMethodology> {
    return of(createMethodology());
  }

  getMethodologyHistory(_options?: AnonymousHttpOptions): Observable<RatingMethodology[]> {
    return of([createMethodology()]);
  }
}

describe('RatingMethodologyPageComponent', () => {
  let fixture: ComponentFixture<RatingMethodologyPageComponent>;
  let paramMapSubject: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  beforeEach(async () => {
    registerSupportedAngularLocales();
    paramMapSubject = new BehaviorSubject(convertToParamMap({ lang: 'fr' }));
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, RatingMethodologyPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: RATING_METHODOLOGY_PORT, useClass: FakeRatingMethodologyPort },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) },
            parent: null,
            paramMap: paramMapSubject
          }
        },
        { provide: TranslationService, useValue: { getCurrentLang: (): string => 'fr', languageChanged: of('fr') } },
        { provide: SeoService, useValue: { applyRouteDefaults: vi.fn(), applyNotFoundSeo: vi.fn() } },
        { provide: JsonLdService, useValue: { replaceJsonLdByType: vi.fn() } }
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      ratings: {
        methodology: {
          title: 'Comment les classements sont calculés',
          breadcrumb: { home: 'Accueil', rankings: 'Classements', methodology: 'Méthodologie' },
          intro: 'Les mêmes règles publiques rendent les résultats comparables.',
          measures: { title: 'Ce que le classement mesure', text: 'Une appréciation consolidée.' },
          doesNotMeasure: { title: 'Ce qu’il ne mesure pas', text: 'Ni la sécurité ni la popularité brute.' },
          contributors: { title: 'Contributeurs et observations', text: 'Un compte distinct forme un contributeur unique.' },
          scale: { title: 'Échelle des notes', text: 'De {{minimum}} à {{maximum}} par {{step}}.' },
          bayesian: { title: 'Pourquoi un score bayésien ?', simple: 'Référence {{priorMean}} pour {{priorWeight}} notes.', formulaAccessible: 'Formule expliquée avec {{priorMean}} et {{priorWeight}}.' },
          parkScore: { title: 'Score composé', text: '{{directWeight}} et {{itemWeight}}.' },
          evidence: { title: 'Niveaux de preuve', text: 'Le texte complète la couleur.', tableLabel: 'Niveaux', level: 'Niveau', contributors: 'Contributeurs', rankingEffect: 'Effet', provisional: 'Provisoire', eligible: 'Admissible', established: 'Établi', strong: 'Fort', provisionalEffect: 'Sans rang', eligibleEffect: 'Classé', establishedEffect: 'Consolidé', strongEffect: 'Robuste', publication: '{{minimumEntries}} entrées.', rolloutPending: 'Activation prochaine : règles bientôt appliquées.' },
          ties: { title: 'Égalités', text: '{{epsilon}}.' },
          lifecycle: { title: 'Cycle de vie', text: 'Cibles actives.' },
          moderation: { title: 'Modération', text: 'Contrôles.' },
          recalculation: { title: 'Recalcul', text: 'Publication cohérente.' },
          history: { title: 'Historique', intro: 'Versions.', reason: 'Pourquoi', effect: 'Effet', previous: 'Précédente', none: 'Première', recomputation: 'Les positions peuvent évoluer.' },
          report: { title: 'Signaler une erreur', text: 'Contacte-nous.', action: 'Contact' },
          versions: { 'ratings-2026-01': { summary: 'Première méthode.', reason: 'Comparer.', effect: 'Stabiliser.' } }
        }
      }
    });
    translateService.use('fr');
    fixture = TestBed.createComponent(RatingMethodologyPageComponent);
  });

  it('renders the critical SSR explanation and every methodology section', () => {
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;

    expect(root.querySelector('h1')?.textContent).toContain('Comment les classements sont calculés');
    expect(root.textContent).toContain('Les mêmes règles publiques rendent les résultats comparables.');
    expect(root.querySelectorAll('section[id]')).toHaveLength(13);
    expect(root.querySelector('#scale')?.textContent).toContain('De 0,5 à 5 par 0,5');
    expect(root.querySelector('#bayesian-score code')?.textContent).toContain('3,5 × 10');
    expect(root.querySelector('#ties')?.textContent).toContain('0,0001');
    expect(root.querySelectorAll('tbody tr')).toHaveLength(4);
    expect(root.querySelector('.methodology-table-wrap')?.getAttribute('tabindex')).toBe('0');
    expect(root.querySelector('.sr-only')?.textContent).toContain('Formule expliquée');
    expect(root.textContent).toContain('31 août 2026');
    expect(root.textContent).toContain('Activation prochaine');
  });

  it('allows every wide section to shrink inside a narrow viewport', () => {
    const styles: string = (
      RatingMethodologyPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');
    const shrinkableChildrenRule: RegExp = new RegExp(
      String.raw`\.methodology-page[^,{]*>\s*\*[^,{]*,\s*` +
      String.raw`\.methodology-grid[^,{]*>\s*\*[^,{]*,\s*` +
      String.raw`\.methodology-history[^,{]*>\s*\*[^,{]*,\s*` +
      String.raw`\.methodology-history[^,{]*dl[^,{]*>\s*\*[^{]*\{[^}]*min-width:\s*0`,
    );
    const scrollContainerRule: RegExp = new RegExp(
      String.raw`\.methodology-table-wrap[^{]*\{[^}]*max-width:\s*100%[^}]*` +
      String.raw`min-width:\s*0[^}]*overflow-x:\s*auto`,
    );

    expect(styles).toContain('width: 100%');
    expect(styles).toMatch(shrinkableChildrenRule);
    expect(styles).toMatch(scrollContainerRule);
  });

  it('exposes visible, clickable parent breadcrumbs and the version history', () => {
    fixture.detectChanges();
    const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('.methodology-breadcrumb a');

    expect(links).toHaveLength(2);
    expect(links[0]?.getAttribute('href')).toBe('/fr/home');
    expect(links[1]?.getAttribute('href')).toBe('/fr/rankings');
    expect(fixture.nativeElement.querySelector('.methodology-history a')?.getAttribute('href'))
      .toBe('/fr/rankings/methodology/ratings-2026-01');
  });

  it('refreshes route SEO when Angular reuses the component for another methodology version', () => {
    const seoService: SeoService = TestBed.inject(SeoService);
    fixture.detectChanges();
    const callsBeforeNavigation: number = vi.mocked(seoService.applyRouteDefaults).mock.calls.length;

    paramMapSubject.next(convertToParamMap({ version: 'ratings-2025-01' }));

    expect(seoService.applyRouteDefaults).toHaveBeenCalledTimes(callsBeforeNavigation + 1);
  });

  it('adds a clickable methodology parent and contextual version breadcrumb on a historical route', () => {
    const jsonLdService: JsonLdService = TestBed.inject(JsonLdService);
    fixture.detectChanges();

    paramMapSubject.next(convertToParamMap({ version: 'ratings-2025-01' }));
    fixture.detectChanges();
    const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('.methodology-breadcrumb a');

    expect(links).toHaveLength(3);
    expect(links[2]?.getAttribute('href')).toBe('/fr/rankings/methodology');
    expect(fixture.nativeElement.querySelector('[aria-current="page"]')?.textContent)
      .toContain('ratings-2025-01');
    expect(jsonLdService.replaceJsonLdByType).toHaveBeenLastCalledWith(
      'BreadcrumbList',
      expect.objectContaining({
        itemListElement: expect.arrayContaining([
          expect.objectContaining({ position: 3, name: 'Méthodologie' }),
          expect.objectContaining({ position: 4, name: 'ratings-2025-01' })
        ])
      })
    );
  });
});

function createMethodology(): RatingMethodology {
  return {
    version: 'ratings-2026-01', effectiveDate: '2026-08-31', isCurrent: true, previousVersion: null,
    ratingScale: { minimum: 0.5, maximum: 5, step: 0.5 }, bayesian: { priorMean: 3.5, priorWeight: 10 },
    parkComposition: { directRatingWeight: 0.7, itemRatingWeight: 0.3, balancesItemCategoriesEqually: true, minimumEligibleItems: 5, minimumItemsPerCategory: 2, minimumCategories: 2 },
    evidenceThresholds: { provisional: 3, eligible: 10, established: 30, strong: 100 },
    publicationRules: { minimumEligibleEntries: 3, scoreTieEpsilon: 0.0001, rankingConvention: 'competition' }
  };
}
