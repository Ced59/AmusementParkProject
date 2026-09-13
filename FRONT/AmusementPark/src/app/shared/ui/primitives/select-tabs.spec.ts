import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { Select, Tab, TabList, TabPanel, TabPanels, Tabs } from './primitives';
import { SelectHostComponent } from './test-helpers/select-tabs/select-host-component';
import { NumericTabHostComponent } from './test-helpers/select-tabs/numeric-tab-host-component';
import { ClickableNumericTabHostComponent } from './test-helpers/select-tabs/clickable-numeric-tab-host-component';

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

  it('emits numeric tab values when the controlled input is numeric', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ClickableNumericTabHostComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const fixture: ComponentFixture<ClickableNumericTabHostComponent> = TestBed.createComponent(ClickableNumericTabHostComponent);
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.p-tab-button')).nativeElement.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.activeTab).toBe(1);
    expect(fixture.debugElement.query(By.css('.active-panel'))).not.toBeNull();
  });

  it('selects a tab when the visual tab host is clicked', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ClickableNumericTabHostComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const fixture: ComponentFixture<ClickableNumericTabHostComponent> = TestBed.createComponent(ClickableNumericTabHostComponent);
    fixture.detectChanges();

    fixture.debugElement.query(By.css('app-ui-tab')).nativeElement.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.activeTab).toBe(1);
    expect(fixture.debugElement.query(By.css('.active-panel'))).not.toBeNull();
  });
});
