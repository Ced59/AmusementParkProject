import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { LanguageChoiceService } from '@app/services/localization/language-choice.service';
import { TranslationService } from '@app/services/translation.service';
import { LanguageEntryPageComponent } from './language-entry-page.component';

describe('LanguageEntryPageComponent', () => {
  let fixture: ComponentFixture<LanguageEntryPageComponent>;
  let languageChoiceService: MockedObject<LanguageChoiceService>;
  let translationService: MockedObject<TranslationService>;
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    languageChoiceService = {
      chooseLanguage: vi.fn(),
    } as unknown as MockedObject<LanguageChoiceService>;
    translationService = {
      useLang: vi.fn(),
    } as unknown as MockedObject<TranslationService>;
    router = {
      navigateByUrl: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [LanguageEntryPageComponent],
      providers: [
        { provide: LanguageChoiceService, useValue: languageChoiceService },
        { provide: TranslationService, useValue: translationService },
        { provide: Router, useValue: router },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LanguageEntryPageComponent);
    fixture.detectChanges();
  });

  it('renders all supported language choices', () => {
    const choices: NodeListOf<HTMLButtonElement> = fixture.nativeElement.querySelectorAll('.language-entry__choice');

    expect(choices).toHaveLength(8);
    expect(fixture.nativeElement.textContent).toContain('Français');
    expect(fixture.nativeElement.textContent).toContain('Português');
  });

  it('stores the explicit choice before opening the localized home', () => {
    languageChoiceService.chooseLanguage.mockReturnValue('fr');
    translationService.useLang.mockReturnValue(of(null));
    const component = fixture.componentInstance as unknown as {
      selectLanguage(language: string): void;
    };

    component.selectLanguage('fr');

    expect(languageChoiceService.chooseLanguage).toHaveBeenCalledWith('fr');
    expect(translationService.useLang).toHaveBeenCalledWith('fr');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/fr/home');
  });
});
