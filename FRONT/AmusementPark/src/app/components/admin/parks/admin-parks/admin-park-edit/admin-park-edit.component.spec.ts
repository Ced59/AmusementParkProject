import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminParkEditComponent } from './admin-park-edit.component';

describe('AdminParkEditComponent', () => {
  let component: AdminParkEditComponent;
  let fixture: ComponentFixture<AdminParkEditComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [AdminParkEditComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminParkEditComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
