import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeafletMapComponent } from './leaflet-map.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('LeafletMapComponent', () => {
  let component: LeafletMapComponent;
  let fixture: ComponentFixture<LeafletMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, LeafletMapComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(LeafletMapComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
