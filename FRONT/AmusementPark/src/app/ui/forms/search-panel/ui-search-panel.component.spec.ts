import { ComponentFixture, TestBed } from '@angular/core/testing';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { UiSearchPanelComponent } from './ui-search-panel.component';

describe('UiSearchPanelComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, UiSearchPanelComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();
  });

  it('emits an explicit search request from the search action', () => {
    const fixture: ComponentFixture<UiSearchPanelComponent> = TestBed.createComponent(UiSearchPanelComponent);
    const component: UiSearchPanelComponent = fixture.componentInstance;
    let searchCount: number = 0;

    component.showSearchAction = true;
    component.searchClicked.subscribe(() => {
      searchCount += 1;
    });
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.app-search-panel__submit button');
    button.click();

    expect(searchCount).toBe(1);
  });

  it('disables the search action and displays a spinner while searching', () => {
    const fixture: ComponentFixture<UiSearchPanelComponent> = TestBed.createComponent(UiSearchPanelComponent);
    const component: UiSearchPanelComponent = fixture.componentInstance;

    component.showSearchAction = true;
    component.searchInProgress = true;
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('.app-search-panel__submit button');

    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-busy')).toBe('true');
    expect(button.querySelector('.pi-spinner.pi-spin')).not.toBeNull();
    expect(button.textContent).toContain('actions.searching');
  });
});
