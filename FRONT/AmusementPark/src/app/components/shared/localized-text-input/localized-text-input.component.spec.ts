import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LocalizedTextInputComponent } from './localized-text-input.component';

describe('LocalizedTextInputComponent', () => {
  let component: LocalizedTextInputComponent;
  let fixture: ComponentFixture<LocalizedTextInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
    imports: [LocalizedTextInputComponent]
})
    .compileComponents();

    fixture = TestBed.createComponent(LocalizedTextInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
