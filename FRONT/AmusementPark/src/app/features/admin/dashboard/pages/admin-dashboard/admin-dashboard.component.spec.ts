import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminDashboardComponent } from './admin-dashboard.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ADMIN_NAVIGATION_ITEMS } from '@shared/models/admin/admin-navigation.models';

describe('AdminDashboardComponent', () => {
  let component: AdminDashboardComponent;
  let fixture: ComponentFixture<AdminDashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminDashboardComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders every shared admin navigation destination', () => {
    fixture.detectChanges();

    const shortcuts: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('.admin-dashboard__shortcut');

    expect(shortcuts.length).toBe(ADMIN_NAVIGATION_ITEMS.length);
  });
});
