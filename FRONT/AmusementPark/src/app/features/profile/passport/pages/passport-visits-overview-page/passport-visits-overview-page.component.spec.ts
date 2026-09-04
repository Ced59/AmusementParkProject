import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PASSPORT_VISITS_OVERVIEW_API_PORT } from '../../state/passport-visits-overview-state-data.ports';
import { PassportVisitsOverviewPageComponent } from './passport-visits-overview-page.component';

describe('PassportVisitsOverviewPageComponent', () => {
  let fixture: ComponentFixture<PassportVisitsOverviewPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, PassportVisitsOverviewPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: PASSPORT_VISITS_OVERVIEW_API_PORT,
          useValue: {
            listVisits: () => of({ items: [], nextCursor: null })
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(PassportVisitsOverviewPageComponent);
  });

  it('renders the private empty state when the user has no visits', () => {
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('.passport-overview__state')).not.toBeNull();
  });

  it('keeps the page width bounded and stacks content on narrow viewports', () => {
    const styles: string = (
      PassportVisitsOverviewPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 760px)');
    expect(styles).toContain('grid-template-columns: 1fr');
    expect(styles).toContain('@media (max-width: 520px)');
  });
});
