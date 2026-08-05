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

  it('balances the desktop title and restores the responsive card rails', () => {
    const styles: string = (
      HomeViewComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: var(--content-wide-max-width, 100rem)');
    expect(styles).toMatch(/\.home-hero__inner[\s\S]*align-items: start/);
    expect(styles).toMatch(/\.home-hero__title[\s\S]*width: 100%[\s\S]*max-width: none[\s\S]*font-size: clamp\(4rem, 6\.5vw, 6\.8rem\)/);
    expect(styles).toMatch(/\.home-hero__title-line[\s\S]*text-wrap: balance/);
    expect(styles).toMatch(/@media \(min-width: 1181px\)[\s\S]*\.home-hero__title-line[\s\S]*white-space: nowrap/);
    expect(styles).toMatch(/\.home-hero__spotlight[\s\S]*padding-top: clamp\(4\.5rem, 7vw, 7rem\)/);
    expect(styles).toMatch(/\.home-floating-stack[\s\S]*max-width: 34rem[\s\S]*min-height: 460px[\s\S]*margin: 0 auto 0 0/);
    expect(styles).toMatch(/@media \(max-width: 960px\)[\s\S]*\.home-hero__spotlight[\s\S]*padding-top: 0/);
    expect(styles).toMatch(/@media \(max-width: 960px\)[\s\S]*\.home-floating-stack[\s\S]*max-width: none/);
    expect(styles).toMatch(/@media \(max-width: 680px\)[\s\S]*\.home-hero__title[\s\S]*font-size: clamp\(3\.2rem, 14vw, 4\.6rem\)/);
    expect(styles).toMatch(/@media \(min-width: 961px\) and \(max-width: 1120px\)[\s\S]*\.home-hero__spotlight[\s\S]*padding-top: 0/);
    expect(styles).toMatch(/@media \(min-width: 961px\) and \(max-width: 1120px\)[\s\S]*\.home-floating-stack[\s\S]*max-width: none/);
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
