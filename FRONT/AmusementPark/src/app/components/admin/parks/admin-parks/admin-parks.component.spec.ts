import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminParksComponent } from './admin-parks.component';

describe('AdminParksComponent', () => {
  let component: AdminParksComponent;
  let fixture: ComponentFixture<AdminParksComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdminParksComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminParksComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
