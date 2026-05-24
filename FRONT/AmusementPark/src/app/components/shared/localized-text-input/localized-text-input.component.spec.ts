import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LocalizedTextInputComponent } from './localized-text-input.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('LocalizedTextInputComponent', () => {
  let component: LocalizedTextInputComponent;
  let fixture: ComponentFixture<LocalizedTextInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, LocalizedTextInputComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(LocalizedTextInputComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
