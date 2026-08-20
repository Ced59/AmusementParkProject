import { signal, Signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
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

  beforeEach(async () => {
    const visibleParkCount = signal<number | null>(47);
    const activatedRouteStub = {
      snapshot: { paramMap: convertToParamMap({}) },
      parent: {
        snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) },
        parent: null
      }
    } as unknown as ActivatedRoute;

    stateFacade = {
      visibleParkCount: visibleParkCount.asReadonly(),
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
          parks: '{{count}} parcs visibles'
        },
        highlights: {
          parks: '{{count}} parcs à explorer'
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
});
