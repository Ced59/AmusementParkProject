import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';

import { AdminSiteComponent } from './admin-site.component';

describe('AdminSiteComponent', () => {
  let component: AdminSiteComponent;
  let fixture: ComponentFixture<AdminSiteComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminSiteComponent, RouterTestingModule]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminSiteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
