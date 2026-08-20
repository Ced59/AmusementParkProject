import { signal, Signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { AboutStateFacade } from '@features/public/about/state/about-state.facade';
import { AboutComponent } from './about.component';

interface AboutStateFacadeStub {
  readonly visibleParkCount: Signal<number | null>;
  readonly loadVisibleParkCount: ReturnType<typeof vi.fn>;
}

describe('AboutComponent', () => {
  let component: AboutComponent;
  let fixture: ComponentFixture<AboutComponent>;
  let stateFacade: AboutStateFacadeStub;
  let visibleParkCountSignal: WritableSignal<number | null>;
  let languageRouteSnapshot: { paramMap: ParamMap };

  beforeEach(async () => {
    visibleParkCountSignal = signal<number | null>(47);
    languageRouteSnapshot = { paramMap: convertToParamMap({ lang: 'fr' }) };
    const activatedRouteStub = {
      snapshot: { paramMap: convertToParamMap({}) },
      parent: {
        snapshot: languageRouteSnapshot,
        parent: null
      }
    } as unknown as ActivatedRoute;

    stateFacade = {
      visibleParkCount: visibleParkCountSignal.asReadonly(),
      loadVisibleParkCount: vi.fn().mockName('AboutStateFacade.loadVisibleParkCount')
    };

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AboutComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ActivatedRoute, useValue: activatedRouteStub }
      ]
    }).overrideComponent(AboutComponent, {
      set: {
        providers: [{ provide: AboutStateFacade, useValue: stateFacade }]
      }
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      aboutPage: {
        seo: {
          title: 'À propos',
          description: 'Description'
        },
        ticket: {
          parks: {
            one: '{{count}} parc visible',
            few: '{{count}} parcs visibles',
            many: '{{count}} parcs visibles',
            other: '{{count}} parcs visibles'
          },
          parksUnavailable: 'Parcs visibles'
        },
        highlights: {
          parks: {
            one: '{{count}} parc à explorer',
            few: '{{count}} parcs à explorer',
            many: '{{count}} parcs à explorer',
            other: '{{count}} parcs à explorer'
          },
          parksUnavailable: 'Des parcs à explorer'
        }
      }
    });
    await firstValueFrom(translateService.use('fr'));

    fixture = TestBed.createComponent(AboutComponent);
    component = fixture.componentInstance;
  });

  it('should create and request the visible park count', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(stateFacade.loadVisibleParkCount).toHaveBeenCalledOnce();
  });

  it('should present the discovery journey in three concise steps', () => {
    fixture.detectChanges();

    const stepCards: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.about-step-card');

    expect(stepCards).toHaveLength(3);
  });

  it('should preserve the route language in all navigation links', () => {
    fixture.detectChanges();

    const parksLinks: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('a[appUiButton="primary"]');
    const homeLink: HTMLAnchorElement | null = fixture.nativeElement.querySelector('.about-final-cta__actions a[appUiButton="ghost"]');

    expect(Array.from(parksLinks).map((link: HTMLAnchorElement): string | null => link.getAttribute('href')))
      .toEqual(['/fr/parks', '/fr/parks']);
    expect(homeLink?.getAttribute('href')).toBe('/fr/home');
  });

  it('should display the dynamic visible park count with locale formatting', () => {
    fixture.detectChanges();

    const parksStat: HTMLElement | null = fixture.nativeElement.querySelector('.about-ticket__meta span:first-child');
    const formattedCount: string = new Intl.NumberFormat('fr').format(47);

    expect(parksStat?.textContent).toContain(`${formattedCount} parcs visibles`);
  });

  it('should apply Polish plural rules to the dynamic park count', async () => {
    const translateService: TranslateService = TestBed.inject(TranslateService);
    languageRouteSnapshot.paramMap = convertToParamMap({ lang: 'pl' });
    visibleParkCountSignal.set(162);
    translateService.setTranslation('pl', {
      aboutPage: {
        seo: {
          title: 'O AmusementPark',
          description: 'Opis'
        },
        ticket: {
          parks: {
            one: '{{count}} widoczny park',
            few: '{{count}} widoczne parki',
            many: '{{count}} widocznych parków',
            other: '{{count}} widocznego parku'
          }
        },
        highlights: {
          parks: {
            one: '{{count}} park do odkrycia',
            few: '{{count}} parki do odkrycia',
            many: '{{count}} parków do odkrycia',
            other: '{{count}} parku do odkrycia'
          }
        }
      }
    });
    await firstValueFrom(translateService.use('pl'));

    fixture.detectChanges();

    const parksStat: HTMLElement | null = fixture.nativeElement.querySelector('.about-ticket__meta span:first-child');
    expect(parksStat?.textContent).toContain('162 widoczne parki');
  });
});
