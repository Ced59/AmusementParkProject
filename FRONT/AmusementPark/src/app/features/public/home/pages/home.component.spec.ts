import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HomeComponent } from './home.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

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
});
