import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { EMPTY } from 'rxjs';

import { TranslationService } from '../../../../../services/translation.service';
import { GlobalRatingSuggestionsStateFacade } from '../../state/global-rating-suggestions-state.facade';
import { GlobalRatingSuggestionsComponent } from './global-rating-suggestions.component';

describe('GlobalRatingSuggestionsComponent', () => {
  it('keeps every nested grid shrinkable and stacks metrics on mobile', () => {
    const styles: string = (
      GlobalRatingSuggestionsComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('max-width: 100%');
    expect(styles).toContain('overflow-x: clip');
    expect(styles).toContain('@media (max-width: 520px)');
    expect(styles).toContain('grid-template-columns: 1fr');
  });

  it('renders an initial loading failure even before availability is known', async () => {
    const fakeFacade = {
      suggestions: signal([]),
      available: signal(false),
      enabled: signal(true),
      loading: signal(false),
      saving: signal(false),
      error: signal(true),
      load: vi.fn(),
      changeLanguage: vi.fn(),
      setEnabled: vi.fn(),
      dismiss: vi.fn(),
      accept: vi.fn()
    };
    await TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot(), GlobalRatingSuggestionsComponent],
      providers: [{
        provide: TranslationService,
        useValue: { getCurrentLang: (): string => 'fr', languageChanged: EMPTY }
      }]
    })
      .overrideComponent(GlobalRatingSuggestionsComponent, {
        set: {
          providers: [{ provide: GlobalRatingSuggestionsStateFacade, useValue: fakeFacade }]
        }
      })
      .compileComponents();
    const fixture: ComponentFixture<GlobalRatingSuggestionsComponent> =
      TestBed.createComponent(GlobalRatingSuggestionsComponent);

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.rating-suggestions')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });
});
