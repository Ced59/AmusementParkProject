const WebpFlagLanguages: ReadonlySet<string> = new Set(['en', 'es', 'it', 'nl', 'pl', 'pt']);

export function resolveFlagAssetPath(language: string): string {
  const normalizedLanguage: string = language.trim().toLowerCase();
  const extension: string = WebpFlagLanguages.has(normalizedLanguage) ? 'webp' : 'png';

  return `assets/flags/${normalizedLanguage}.${extension}`;
}
