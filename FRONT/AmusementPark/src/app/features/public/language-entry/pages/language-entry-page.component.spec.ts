import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { LanguageChoiceService } from '@app/services/localization/language-choice.service';
import { LanguageEntryPageComponent } from './language-entry-page.component';

describe('LanguageEntryPageComponent', () => {
  let fixture: ComponentFixture<LanguageEntryPageComponent>;
  let languageChoiceService: MockedObject<LanguageChoiceService>;

  beforeEach(async () => {
    languageChoiceService = {
      chooseLanguage: vi.fn(),
    } as unknown as MockedObject<LanguageChoiceService>;
    await TestBed.configureTestingModule({
      imports: [LanguageEntryPageComponent],
      providers: [
        { provide: LanguageChoiceService, useValue: languageChoiceService },
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LanguageEntryPageComponent);
    fixture.detectChanges();
  });

  it('renders all supported language choices', () => {
    const choices: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('.language-entry__choice');

    expect(choices).toHaveLength(8);
    expect(fixture.nativeElement.textContent).toContain('Français');
    expect(fixture.nativeElement.textContent).toContain('Português');
    expect(choices[1]?.getAttribute('href')).toBe('/fr/home');
  });

  it('stores the explicit choice while the real link remains the navigation fallback', () => {
    languageChoiceService.chooseLanguage.mockReturnValue('fr');
    const component = fixture.componentInstance as unknown as {
      selectLanguage(language: string): void;
    };

    component.selectLanguage('fr');

    expect(languageChoiceService.chooseLanguage).toHaveBeenCalledWith('fr');
  });

  it('preserves native link semantics for every language choice', () => {
    const choices: NodeListOf<HTMLAnchorElement> = fixture.nativeElement.querySelectorAll('.language-entry__choice');

    for (const choice of Array.from(choices)) {
      expect(choice.tagName).toBe('A');
      expect(choice.hasAttribute('role')).toBe(false);
      expect(choice.getAttribute('href')).toMatch(/^\/(en|fr|es|de|it|pl|nl|pt)\/home$/);
      expect(choice.getAttribute('aria-label')?.trim()).toBeTruthy();
    }
    expect(fixture.nativeElement.querySelector('[role="list"]')).toBeNull();
  });

  it('provides a high density logo without changing its reserved aspect ratio', () => {
    const logo: HTMLImageElement = fixture.nativeElement.querySelector('.language-entry__logo');

    expect(logo.getAttribute('src')).toBe('/assets/general-icon/logo-amusementpark-language-136.webp');
    expect(logo.getAttribute('srcset')).toBe(
      '/assets/general-icon/logo-amusementpark-language-136.webp 1x, /assets/general-icon/logo-amusementpark-language-272.webp 2x, /assets/general-icon/logo-amusementpark-language-408.webp 3x',
    );
    expect(logo.getAttribute('width')).toBe('184');
    expect(logo.getAttribute('height')).toBe('184');
  });
});
