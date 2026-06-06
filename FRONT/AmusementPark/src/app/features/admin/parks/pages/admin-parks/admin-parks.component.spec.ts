import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminParksComponent } from './admin-parks.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('AdminParksComponent', () => {
  let component: AdminParksComponent;
  let fixture: ComponentFixture<AdminParksComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AdminParksComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AdminParksComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
