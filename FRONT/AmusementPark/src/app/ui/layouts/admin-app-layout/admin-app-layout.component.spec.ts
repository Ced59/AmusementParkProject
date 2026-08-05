import { ViewEncapsulation } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminAppLayoutComponent } from './admin-app-layout.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ADMIN_NAVIGATION_ITEMS } from '@shared/models/admin/admin-navigation.models';

describe('AdminAppLayoutComponent', () => {
  it('uses unscoped component styles so admin CSS stays out of the public initial bundle', () => {
    expect((AdminAppLayoutComponent as unknown as { ɵcmp: { encapsulation: ViewEncapsulation } }).ɵcmp.encapsulation)
      .toBe(ViewEncapsulation.None);
  });

  it('renders every shared admin navigation destination after the dashboard link', async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminAppLayoutComponent],
      providers: provideCommonTestDependencies()
    }).compileComponents();

    const fixture: ComponentFixture<AdminAppLayoutComponent> = TestBed.createComponent(AdminAppLayoutComponent);
    fixture.detectChanges();

    const links: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('.app-admin-layout__nav a');

    expect(links.length).toBe(ADMIN_NAVIGATION_ITEMS.length + 1);
  });
});
