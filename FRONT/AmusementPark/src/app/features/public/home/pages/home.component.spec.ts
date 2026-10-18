import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeComponent } from './home.component';
import { HomeViewComponent } from './home-view.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { HomeStateFacade } from '../state/home-state.facade';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, HomeComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('exposes standalone attraction search options', () => {
    const filters = (component as unknown as {
      searchFilters: () => Array<{ options: Array<{ value: string | null }> }>;
    }).searchFilters();
    const values: Array<string | null> = filters[0].options.map((option: { value: string | null }) => option.value);

    expect(values).toContain('attractionsWithStandalone');
    expect(values).toContain('standaloneAttractions');
  });

  it('keeps the Playground editorial split and responsive content rails', () => {
    const styles: string = (
      HomeViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: var(--content-max-width, 1160px)');
    expect(styles).toMatch(/\.home-hero__inner[\s\S]*grid-template-columns: minmax\(0, 1\.08fr\) minmax\(0, 0\.92fr\)[\s\S]*align-items: start/);
    expect(styles).toMatch(/\.home-hero__title[\s\S]*width: 100%[\s\S]*max-width: none[\s\S]*font-size: clamp\(39px, 5\.3vw, 74px\)/);
    expect(styles).toMatch(/\.home-hero__title-line[\s\S]*text-wrap: balance/);
    expect(styles).toMatch(/\.home-hero__spotlight[\s\S]*border-left: 3px solid var\(--app-heading\)/);
    expect(styles).toMatch(/\.home-hero-card--1[\s\S]*border-top: 5px solid var\(--c-orange\)/);
    expect(styles).toMatch(/@media \(max-width: 960px\)[\s\S]*\.home-hero__inner[\s\S]*grid-template-columns: 1fr/);
    expect(styles).toMatch(/@media \(max-width: 960px\)[\s\S]*\.home-hero__spotlight[\s\S]*border-left: 0/);
    expect(styles).toMatch(/@media \(max-width: 680px\)[\s\S]*\.home-hero__title[\s\S]*font-size: clamp\(38px, 9vw, 53px\)/);
    expect(styles).toMatch(/\.home-hero-card--1[^}]*\.home-hero-card__media[^}]*height: 205px/);
    expect(styles).toMatch(/@media \(max-width: 680px\)[\s\S]*\.home-featured__grid[\s\S]*display: flex[\s\S]*overflow-x: auto/);
    expect(styles).toMatch(/\.home-featured[^{]*>\s*app-page-state[^{]*\{[^}]*min-width: 0;[^}]*width: 100%/);
    expect(styles).toContain('scroll-snap-type: x mandatory');
    expect(styles).not.toMatch(/(?:linear|radial|conic)-gradient\(/);
    expect(styles).not.toMatch(/animation\s*:/);
  });

  it('runs an explicit search immediately without keeping the pending live search', () => {
    vi.useFakeTimers();

    try {
      fixture.detectChanges();
      const stateFacade: HomeStateFacade = fixture.debugElement.injector.get(HomeStateFacade);
      const searchSpy = vi.spyOn(stateFacade, 'search').mockImplementation(() => undefined);

      component.onSearchInput('  Europa-Park  ');
      component.onSearchSubmit();

      expect(searchSpy).toHaveBeenCalledTimes(1);
      expect(searchSpy).toHaveBeenCalledWith('Europa-Park', '', 1, stateFacade.pageSize());

      vi.advanceTimersByTime(300);

      expect(searchSpy).toHaveBeenCalledTimes(1);
    } finally {
      fixture.destroy();
      vi.useRealTimers();
    }
  });
});
