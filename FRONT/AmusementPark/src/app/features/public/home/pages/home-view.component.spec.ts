import { Signal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { SearchResultItem } from '@app/models/search/search-result-item';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { HomeViewComponent } from './home-view.component';

describe('HomeViewComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, HomeViewComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();
  });

  it('limits mobile search suggestions to five current results', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent(createSearchResults(6));

    fixture.detectChanges();

    const suggestionElements: NodeListOf<Element> = fixture.nativeElement.querySelectorAll('.home-search-suggestion');

    expect(suggestionElements.length).toBe(5);
    expect(suggestionElements.item(0).textContent).toContain('Result 1');
    expect(suggestionElements.item(4).textContent).toContain('Result 5');
  });

  it('emits a suggestion title when a result has no direct public link', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent([
      createSearchResult('manufacturer_1', 'Manufacturer', 'Mack Rides')
    ]);
    let selectedTitle: string = '';

    fixture.componentInstance.suggestionSelected.subscribe((title: string) => {
      selectedTitle = title;
    });
    fixture.detectChanges();

    const suggestionButton: HTMLButtonElement = fixture.debugElement.query(By.css('button.home-search-suggestion')).nativeElement;
    suggestionButton.click();

    expect(selectedTitle).toBe('Mack Rides');
  });
});

function createComponent(results: SearchResultItem[]): ComponentFixture<HomeViewComponent> {
  const fixture: ComponentFixture<HomeViewComponent> = TestBed.createComponent(HomeViewComponent);
  const component: HomeViewComponent = fixture.componentInstance;
  const readyState: Signal<ScreenState<unknown, string>> = signal<ScreenState<unknown, string>>({ kind: 'ready' }).asReadonly();

  component.currentLang = signal('en').asReadonly();
  component.searchTerm = signal('parc').asReadonly();
  component.searchFilters = signal([]).asReadonly();
  component.statsState = readyState;
  component.homeStats = signal(null).asReadonly();
  component.featuredState = readyState;
  component.featuredParks = signal([]).asReadonly();
  component.heroFeaturedParks = signal([]).asReadonly();
  component.searchState = readyState;
  component.results = signal(results).asReadonly();
  component.pagination = signal(null).asReadonly();
  component.hasPerformedSearch = signal(true).asReadonly();
  component.searchResultsTotal = results.length;
  component.searchResultsHintKey = 'home.search.resultsSubtitle';

  return fixture;
}

function createSearchResults(count: number): SearchResultItem[] {
  return Array.from({ length: count }, (_: unknown, index: number) =>
    createSearchResult(`park_${index + 1}`, 'Park', `Result ${index + 1}`));
}

function createSearchResult(originalId: string, category: string, title: string): SearchResultItem {
  return {
    originalId,
    category,
    title,
    description: 'Description',
    city: 'Paris',
    countryCode: 'FR'
  };
}
