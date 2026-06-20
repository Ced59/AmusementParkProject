import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { siteVersion } from '../../../../environments/version.generated';
import { PublicFooterComponent } from './public-footer.component';

describe('PublicFooterComponent', () => {
  let fixture: ComponentFixture<PublicFooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PublicFooterComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('en', {
      footer: {
        ariaLabel: 'Site footer',
        brand: {
          sloganData: 'Reliable data',
          sloganFun: 'for more fun'
        },
        explore: {
          title: 'Explore',
          allParks: 'All parks',
          interactiveMap: 'Interactive map'
        },
        about: {
          title: 'About',
          project: 'The project',
          contact: 'Contact',
          versions: 'Versions',
          privacy: 'Privacy'
        },
        languages: {
          title: 'Languages'
        },
        bottom: {
          copy: 'Made with ❤️ and adrenaline',
          version: 'Version {{version}}'
        }
      }
    });
    translateService.use('en');

    fixture = TestBed.createComponent(PublicFooterComponent);
    fixture.detectChanges();
  });

  it('displays the generated site version', () => {
    const textContent = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(textContent).toContain(`Version ${siteVersion}`);
  });

  it('links the generated site version to the version history page', () => {
    const versionLink: HTMLAnchorElement | null = (fixture.nativeElement as HTMLElement)
      .querySelector('.app-public-footer__badges a');

    expect(versionLink).not.toBeNull();
    expect(versionLink?.getAttribute('href')).toBe('/en/versions');
  });

  it('renders the public brand wordmark with the .fun signature', () => {
    const brandLink: HTMLAnchorElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-footer__logo');
    const wordmark: HTMLElement | null = brandLink?.querySelector('.app-brand-wordmark') ?? null;
    const base: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__base') ?? null;
    const dot: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__dot') ?? null;
    const fun: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__fun-text') ?? null;

    expect(brandLink?.getAttribute('aria-label')).toBe('AMUSEMENT-PARKS.fun');
    expect(base?.textContent).toBe('AMUSEMENT-PARKS');
    expect(dot?.textContent).toBe('.');
    expect(fun?.textContent).toBe('fun');
  });

  it('renders the footer slogan as a synchronized data and fun phrase', () => {
    const slogan: HTMLElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-footer__slogan');
    const dataLine: HTMLElement | null = slogan?.querySelector('.app-public-footer__slogan-base') ?? null;
    const funLine: HTMLElement | null = slogan?.querySelector('.app-public-footer__slogan-fun') ?? null;

    expect(dataLine?.textContent?.trim()).toBe('Reliable data');
    expect(funLine?.textContent?.trim()).toBe('for more fun');
  });

  it('marks the current footer language link for accessible active styling', () => {
    const activeLanguageLink: HTMLAnchorElement | null = (fixture.nativeElement as HTMLElement)
      .querySelector('.app-public-footer__language-link--active');

    expect(activeLanguageLink).not.toBeNull();
    expect(activeLanguageLink?.textContent).toContain('English');
    expect(activeLanguageLink?.getAttribute('aria-current')).toBe('page');
  });
});
