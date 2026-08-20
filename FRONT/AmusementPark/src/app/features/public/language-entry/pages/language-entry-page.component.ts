import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

import { LanguageChoiceService } from '@app/services/localization/language-choice.service';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink]
})
export class LanguageEntryPageComponent {
  protected readonly languages: readonly LanguageEntryOption[] = LANGUAGES.map(
    (language: LanguageOption): LanguageEntryOption => ({
      ...language,
      invitation: invitations[language.value] ?? language.label
    })
  );

  constructor(private readonly languageChoiceService: LanguageChoiceService) {
  }

  protected flagAssetPath(language: string): string {
    return resolveFlagAssetPath(language);
  }

  protected selectLanguage(language: string): void {
    this.languageChoiceService.chooseLanguage(language);
  }
}
