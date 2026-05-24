import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminSiteComponent } from './admin-site.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('AdminSiteComponent', () => {
  let component: AdminSiteComponent;
  let fixture: ComponentFixture<AdminSiteComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminSiteComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AdminSiteComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
