import { TranslateService } from '@ngx-translate/core';

export function resolveLocalizedPlural(
  translateService: TranslateService,
  translationKey: string,
  count: number,
  locale: string,
  params: Record<string, unknown> = {}
): string {
  const pluralCategory: Intl.LDMLPluralRule = new Intl.PluralRules(locale).select(count);
  const categoryKey: string = `${translationKey}.${pluralCategory}`;
  const interpolationParams: Record<string, unknown> = { ...params, count: params['count'] ?? count };
  const categoryTranslation: string = translateService.instant(categoryKey, interpolationParams) as string;

  if (categoryTranslation !== categoryKey) {
    return categoryTranslation;
  }

  const fallbackKey: string = `${translationKey}.other`;
  return translateService.instant(fallbackKey, interpolationParams) as string;
}
