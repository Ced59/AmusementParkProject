import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminParkEditComponent } from './admin-park-edit.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('AdminParkEditComponent', () => {
  let component: AdminParkEditComponent;
  let fixture: ComponentFixture<AdminParkEditComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParkEditComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AdminParkEditComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
