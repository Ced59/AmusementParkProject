import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { Select, Tab, TabList, TabPanel, TabPanels, Tabs } from './primitives';

@Component({
  standalone: true,
  imports: [Select],
  template: '<app-ui-select [options]="options" optionValue="value"></app-ui-select>'
})
class SelectHostComponent {
  readonly options: Array<{ labelKey: string; value: string }> = [
    { labelKey: 'admin.parks.types.themePark', value: 'ThemePark' }
  ];
}

@Component({
  standalone: true,
  imports: [Tabs, TabList, Tab, TabPanels, TabPanel],
  template: `
    <app-ui-tabs [value]="activeTab">
      <app-ui-tab-list>
        <app-ui-tab value="1">Location</app-ui-tab>
      </app-ui-tab-list>
      <app-ui-tab-panels>
        <app-ui-tab-panel value="1">
          <span class="active-panel">Location panel</span>
        </app-ui-tab-panel>
      </app-ui-tab-panels>
    </app-ui-tabs>
  `
})
class NumericTabHostComponent {
  activeTab: number = 1;
}

describe('Select primitive', () => {
  it('uses labelKey as a safe label fallback for translated option objects', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, SelectHostComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const fixture: ComponentFixture<SelectHostComponent> = TestBed.createComponent(SelectHostComponent);
    fixture.detectChanges();

    const option: HTMLOptionElement = fixture.debugElement.query(By.css('option')).nativeElement;

    expect(option.textContent?.trim()).toBe('admin.parks.types.themePark');
    expect(option.textContent).not.toContain('[object Object]');
  });
});

describe('Tabs primitive', () => {
  it('keeps numeric active tab inputs compatible with string tab values', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, NumericTabHostComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const fixture: ComponentFixture<NumericTabHostComponent> = TestBed.createComponent(NumericTabHostComponent);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.css('.active-panel'))).not.toBeNull();
    expect(fixture.debugElement.query(By.css('app-ui-tab')).nativeElement.classList).toContain('p-tab-active');
  });
});
