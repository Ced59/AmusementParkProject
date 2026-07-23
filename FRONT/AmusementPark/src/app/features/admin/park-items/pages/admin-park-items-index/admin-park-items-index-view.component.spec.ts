import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { Select } from '@shared/ui/primitives/select';
import { AdminParkItemsIndexViewComponent } from './admin-park-items-index-view.component';

describe('AdminParkItemsIndexViewComponent', () => {
  it('enables local search on the park filter without changing the other filters', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkItemsIndexViewComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const fixture: ComponentFixture<AdminParkItemsIndexViewComponent> = TestBed.createComponent(AdminParkItemsIndexViewComponent);
    fixture.componentRef.setInput('state', signal({ kind: 'ready' }));
    fixture.componentRef.setInput('loading', signal(false));
    fixture.componentRef.setInput('rows', signal([]));
    fixture.componentRef.setInput('parkOptions', signal([
      { label: 'Tous les parcs', value: null },
      { label: 'Europa-Park', value: 'park-1' }
    ]));
    fixture.componentRef.setInput('totalRecords', signal(0));
    fixture.componentRef.setInput('selectedItemIds', signal([]));
    fixture.componentRef.setInput('selectedCount', signal(0));
    fixture.detectChanges();

    const selects = fixture.debugElement.queryAll(By.directive(Select));
    const parkSelect: Select = selects[0].componentInstance as Select;

    expect(parkSelect.filter).toBeTrue();
    const filterInput: HTMLInputElement = fixture.debugElement.query(By.css('.p-select-filter')).nativeElement;
    const nativeSelect: HTMLSelectElement = fixture.debugElement.query(By.css('.p-select-native')).nativeElement;

    expect(filterInput.getAttribute('aria-label')).toBeTruthy();
    expect(nativeSelect.getAttribute('aria-label')).toBeTruthy();
  });
});
