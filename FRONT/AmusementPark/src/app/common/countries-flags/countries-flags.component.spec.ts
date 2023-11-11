import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CountriesFlagsComponent } from './countries-flags.component';

describe('CountriesFlagsComponent', () => {
  let component: CountriesFlagsComponent;
  let fixture: ComponentFixture<CountriesFlagsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CountriesFlagsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CountriesFlagsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
