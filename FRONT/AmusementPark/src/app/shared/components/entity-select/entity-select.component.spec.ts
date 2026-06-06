import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EntitySelectComponent } from './entity-select.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('EntitySelectComponent', () => {
  let component: EntitySelectComponent;
  let fixture: ComponentFixture<EntitySelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, EntitySelectComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(EntitySelectComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
