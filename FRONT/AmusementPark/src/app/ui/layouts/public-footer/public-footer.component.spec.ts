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
          sloganData: 'Rigorous data',
          sloganFun: 'for even more fun'
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
    const logoImage: HTMLImageElement | null = brandLink?.querySelector('.app-public-footer__logo-mark') ?? null;
    const wordmark: HTMLElement | null = brandLink?.querySelector('.app-brand-wordmark') ?? null;
    const base: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__base') ?? null;
    const dot: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__dot') ?? null;
    const fun: HTMLElement | null = wordmark?.querySelector('.app-brand-wordmark__fun-text') ?? null;

    expect(brandLink?.getAttribute('aria-label')).toBe('AMUSEMENT-PARKS.fun');
    expect(logoImage?.getAttribute('src')).toBe('/assets/general-icon/logo-amusementpark.png');
    expect(logoImage?.getAttribute('alt')).toBe('AMUSEMENT-PARKS.fun');
    expect(base?.textContent).toBe('AMUSEMENT-PARKS');
    expect(dot?.textContent).toBe('.');
    expect(fun?.textContent).toBe('fun');
  });

  it('renders the footer slogan as a synchronized data and fun phrase', () => {
    const slogan: HTMLElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-footer__slogan');
    const dataLine: HTMLElement | null = slogan?.querySelector('.app-public-footer__slogan-base') ?? null;
    const funLine: HTMLElement | null = slogan?.querySelector('.app-public-footer__slogan-fun') ?? null;

    expect(dataLine?.textContent?.trim()).toBe('Rigorous data');
    expect(funLine?.textContent?.trim()).toBe('for even more fun');
  });

  it('keeps the footer link columns after the full-width brand block', () => {
    const layout: HTMLElement | null = (fixture.nativeElement as HTMLElement).querySelector('.app-public-footer__inner');
    const children: Element[] = Array.from(layout?.children ?? []);
    const brandIndex: number = children.findIndex((child: Element) => child.classList.contains('app-public-footer__brand'));
    const columnIndexes: number[] = children
      .map((child: Element, index: number) => child.classList.contains('app-public-footer__column') ? index : -1)
      .filter((index: number) => index >= 0);

    expect(brandIndex).toBe(0);
    expect(columnIndexes).toEqual([1, 2, 3]);
  });

  it('marks the current footer language link for accessible active styling', () => {
    const activeLanguageLink: HTMLAnchorElement | null = (fixture.nativeElement as HTMLElement)
      .querySelector('.app-public-footer__language-link--active');

    expect(activeLanguageLink).not.toBeNull();
    expect(activeLanguageLink?.textContent).toContain('English');
    expect(activeLanguageLink?.getAttribute('aria-current')).toBe('page');
    expect(activeLanguageLink?.querySelector('img')?.getAttribute('alt')).toBe('');
    expect(activeLanguageLink?.querySelector('img')?.getAttribute('aria-hidden')).toBe('true');
  });
});
