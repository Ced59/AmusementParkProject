import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeComponent } from './home.component';
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
