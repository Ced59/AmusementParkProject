import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AboutComponent } from './about.component';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';

describe('AboutComponent', () => {
  let component: AboutComponent;
  let fixture: ComponentFixture<AboutComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, AboutComponent],
      providers: provideCommonTestDependencies(),
    }).compileComponents();

    fixture = TestBed.createComponent(AboutComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should present the discovery journey in three concise steps', () => {
    fixture.detectChanges();

    const stepCards: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.about-step-card');

    expect(stepCards).toHaveLength(3);
  });

  it('should link both main actions to the localized parks route', () => {
    fixture.detectChanges();

    const parksLinks: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('a[appUiButton="primary"]');

    expect(parksLinks).toHaveLength(2);
    expect(Array.from(parksLinks).map((link: HTMLAnchorElement): string | null => link.getAttribute('href')))
      .toEqual(['/parks', '/parks']);
  });
});
