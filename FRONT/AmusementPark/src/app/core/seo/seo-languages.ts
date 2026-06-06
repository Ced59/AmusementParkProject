import { LANGUAGES } from '@shared/models/localization';

export interface SeoLanguageDefinition {
  language: string;
  hreflang: string;
}

export const SEO_LANGUAGES: readonly SeoLanguageDefinition[] = LANGUAGES.map((language) => ({
  language: language.value,
  hreflang: language.value,
}));

export const SEO_DEFAULT_LANGUAGE: string = 'en';
