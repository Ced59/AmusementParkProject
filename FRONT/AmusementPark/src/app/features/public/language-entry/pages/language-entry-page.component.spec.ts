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
});
