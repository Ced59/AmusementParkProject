import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';

import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { ParkLifecycleNoticeComponent } from './park-lifecycle-notice.component';

interface ComponentDefinitionWithStyles {
  ɵcmp: {
    styles: readonly string[];
  };
}

describe('ParkLifecycleNoticeComponent', () => {
  let fixture: ComponentFixture<ParkLifecycleNoticeComponent>;
  let component: ParkLifecycleNoticeComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, ParkLifecycleNoticeComponent],
      providers: [
        ...provideCommonTestDependencies()
      ]
    }).compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setDefaultLang('fr');
    translateService.setTranslation('fr', {
      parks: {
        lifecycle: {
          closedDefinitively: {
            title: 'Parc fermé définitivement',
            body: 'Cette fiche conserve la mémoire de {{ name }}.'
          }
        }
      }
    });
    translateService.use('fr');

    fixture = TestBed.createComponent(ParkLifecycleNoticeComponent);
    component = fixture.componentInstance;
  });

  it('renders an accessible notice for a permanently closed park', () => {
    component.status = 'ClosedDefinitively';
    component.parkName = 'Test Park';

    fixture.detectChanges();

    const notice: HTMLElement | null = fixture.nativeElement.querySelector('[role="note"]');
    expect(notice?.textContent).toContain('Parc fermé définitivement');
    expect(notice?.textContent).toContain('Cette fiche conserve la mémoire de Test Park.');
  });

  it('does not render a notice for an operating park', () => {
    component.status = 'Operating';

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="note"]')).toBeNull();
  });

  it('uses the centralized semantic theme tokens for contrast', () => {
    const styles: string = readComponentStyles();

    expect(styles).toContain('var(--app-surface)');
    expect(styles).toContain('var(--app-text)');
    expect(styles).toContain('var(--app-text-muted)');
    expect(styles).toContain('var(--app-warning)');
    expect(styles).not.toContain('--surface-card');
    expect(styles).not.toContain('--text-color-secondary');
  });
});

function readComponentStyles(): string {
  const definition: ComponentDefinitionWithStyles = ParkLifecycleNoticeComponent as unknown as ComponentDefinitionWithStyles;
  return definition.ɵcmp.styles.join('\n');
}
