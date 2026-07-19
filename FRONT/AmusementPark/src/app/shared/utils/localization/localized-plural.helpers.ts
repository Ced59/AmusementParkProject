import { TranslateService } from '@ngx-translate/core';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';

export function resolveLocalizedPlural(
  translateService: TranslateService,
  translationKey: string,
  count: number,
  locale: string,
  params: Record<string, unknown> = {}
): string {
  const pluralCategory: Intl.LDMLPluralRule = new Intl.PluralRules(resolveConfiguredLocale(locale)).select(count);
  const categoryKey: string = `${translationKey}.${pluralCategory}`;
  const interpolationParams: Record<string, unknown> = { ...params, count: params['count'] ?? count };
  const categoryTranslation: string = translateService.instant(categoryKey, interpolationParams) as string;

  if (categoryTranslation !== categoryKey) {
    return categoryTranslation;
  }

  const fallbackKey: string = `${translationKey}.other`;
  return translateService.instant(fallbackKey, interpolationParams) as string;
}

function resolveConfiguredLocale(locale: string): string {
  const normalizedLocale: string = locale.trim().toLowerCase();
  const configuredLanguage: LanguageOption | undefined = LANGUAGES.find((language: LanguageOption) =>
    language.value.toLowerCase() === normalizedLocale || language.code.toLowerCase() === normalizedLocale);
  return configuredLanguage?.code ?? locale;
}
