export interface LanguageOption {
  label: string;
  value: string;
  code: string;
}

export const LANGUAGES: readonly LanguageOption[] = [
  { label: 'English', value: 'en', code: 'en-US' },
  { label: 'Français', value: 'fr', code: 'fr-FR' },
  { label: 'Español', value: 'es', code: 'es-ES' },
  { label: 'Deutsch', value: 'de', code: 'de-DE' },
  { label: 'Italiano', value: 'it', code: 'it-IT' },
  { label: 'Polski', value: 'pl', code: 'pl-PL' },
  { label: 'Nederlands', value: 'nl', code: 'nl-NL' },
  { label: 'Português', value: 'pt', code: 'pt-PT' }
];
