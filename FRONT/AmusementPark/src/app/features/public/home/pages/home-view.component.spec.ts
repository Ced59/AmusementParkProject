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

  it('links standalone attraction suggestions to their public detail page', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent([
      createSearchResult('standaloneAttraction_standalone-1', 'Standalone Attraction', 'Bardonecchia Alpine Coaster')
    ]);

    fixture.detectChanges();

    const suggestionLink: HTMLAnchorElement = fixture.debugElement.query(By.css('a.home-search-suggestion')).nativeElement;

    expect(suggestionLink.getAttribute('href')).toContain('/en/attraction/standalone-1/bardonecchia-alpine-coaster');
  });

  it('emits the park autocomplete title when the autocomplete is clicked', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent([
      createSearchResult('park_1', 'Park', 'Boudewijn Seapark')
    ], 'Boud');
    let selectedTitle: string = '';

    fixture.componentInstance.autocompleteSelected.subscribe((title: string) => {
      selectedTitle = title;
    });
    fixture.detectChanges();

    const autocompleteButton: HTMLButtonElement = fixture.debugElement.query(By.css('button.home-search-autocomplete')).nativeElement;
    autocompleteButton.click();

    expect(selectedTitle).toBe('Boudewijn Seapark');
  });

  it('shows up to three park autocomplete names from current results', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent([
      createSearchResult('park_1', 'Park', 'Boudewijn Seapark'),
      createSearchResult('parkItem_1', 'Attraction', 'Boud Hotel', { parentParkName: 'Boud Parent Park' }),
      createSearchResult('park_2', 'Park', 'Boud Adventure World'),
      createSearchResult('park_3', 'Park', 'Boud Family Park'),
      createSearchResult('park_4', 'Park', 'Boud Park Four')
    ], 'Boud');

    fixture.detectChanges();

    const autocompleteButtons: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('button.home-search-autocomplete');
    const autocompleteTexts: string[] = Array.from(autocompleteButtons).map((button: HTMLButtonElement) => button.textContent ?? '');

    expect(autocompleteButtons.length).toBe(3);
    expect(autocompleteTexts.some((text: string) => text.includes('Boudewijn Seapark'))).toBeTrue();
    expect(autocompleteTexts.some((text: string) => text.includes('Boud Parent Park'))).toBeTrue();
    expect(autocompleteTexts.some((text: string) => text.includes('Boud Adventure World'))).toBeTrue();
    expect(autocompleteTexts.some((text: string) => text.includes('Boud Hotel'))).toBeFalse();
    expect(autocompleteTexts.some((text: string) => text.includes('Boud Park Four'))).toBeFalse();
  });

  it('accepts the park autocomplete from the search input with tab', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent([
      createSearchResult('park_1', 'Park', 'Boudewijn Seapark')
    ], 'Boud');
    let selectedTitle: string = '';

    fixture.componentInstance.autocompleteSelected.subscribe((title: string) => {
      selectedTitle = title;
    });
    fixture.detectChanges();

    const searchInput: HTMLInputElement = fixture.nativeElement.querySelector('#home-search-input') as HTMLInputElement;
    const tabEvent: KeyboardEvent = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    searchInput.dispatchEvent(tabEvent);

    expect(selectedTitle).toBe('Boudewijn Seapark');
    expect(tabEvent.defaultPrevented).toBeTrue();
  });

  it('uses parent park names as autocomplete candidates for broad search results', () => {
    const fixture: ComponentFixture<HomeViewComponent> = createComponent([
      createSearchResult('parkItem_1', 'Attraction', 'Taron', { parentParkName: 'Phantasialand' })
    ], 'Phan');

    fixture.detectChanges();

    const autocompleteButton: HTMLButtonElement = fixture.debugElement.query(By.css('button.home-search-autocomplete')).nativeElement;

    expect(autocompleteButton.textContent).toContain('Phantasialand');
  });
});

function createComponent(results: SearchResultItem[], searchTerm: string = 'parc'): ComponentFixture<HomeViewComponent> {
  const fixture: ComponentFixture<HomeViewComponent> = TestBed.createComponent(HomeViewComponent);
  const component: HomeViewComponent = fixture.componentInstance;
  const readyState: Signal<ScreenState<unknown, string>> = signal<ScreenState<unknown, string>>({ kind: 'ready' }).asReadonly();

  component.currentLang = signal('en').asReadonly();
  component.searchTerm = signal(searchTerm).asReadonly();
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

function createSearchResult(originalId: string, category: string, title: string, overrides: Partial<SearchResultItem> = {}): SearchResultItem {
  return {
    originalId,
    category,
    title,
    description: 'Description',
    city: 'Paris',
    countryCode: 'FR',
    ...overrides
  };
}
