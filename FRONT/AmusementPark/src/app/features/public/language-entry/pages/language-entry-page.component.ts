import { ChangeDetectionStrategy, Component, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { LanguageChoiceService } from '@app/services/localization/language-choice.service';
import { TranslationService } from '@app/services/translation.service';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { resolveFlagAssetPath } from '@shared/utils/assets/flag-assets';

interface LanguageEntryOption extends LanguageOption {
  invitation: string;
}

const invitations: Readonly<Record<string, string>> = {
  en: 'Explore amusement parks in English',
  fr: 'Découvre les parcs d’attractions en français',
  es: 'Descubre parques de atracciones en español',
  de: 'Entdecke Freizeitparks auf Deutsch',
  it: 'Scopri i parchi divertimento in italiano',
  pl: 'Odkrywaj parki rozrywki po polsku',
  nl: 'Ontdek pretparken in het Nederlands',
  pt: 'Explora parques de diversão em português'
};

@Component({
  selector: 'app-language-entry-page',
  templateUrl: './language-entry-page.component.html',
  styleUrl: './language-entry-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LanguageEntryPageComponent {
  protected readonly languages: readonly LanguageEntryOption[] = LANGUAGES.map(
    (language: LanguageOption): LanguageEntryOption => ({
      ...language,
      invitation: invitations[language.value] ?? language.label
    })
  );

  constructor(
    private readonly languageChoiceService: LanguageChoiceService,
    private readonly translationService: TranslationService,
    private readonly router: Router,
    private readonly destroyRef: DestroyRef
  ) {
  }

  protected flagAssetPath(language: string): string {
    return resolveFlagAssetPath(language);
  }

  protected selectLanguage(language: string): void {
    const selectedLanguage: string | null = this.languageChoiceService.chooseLanguage(language);
    if (selectedLanguage === null) {
      return;
    }

    this.translationService.useLang(selectedLanguage)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.router.navigateByUrl(`/${selectedLanguage}/home`)
            .catch((error: unknown): void => console.error('Failed to open the localized home page.', error));
        },
        error: (error: unknown): void => console.error('Error changing language:', error)
      });
  }
}
