interface RegionDisplayNames {
  of(code: string): string | undefined;
}

interface RegionDisplayNamesConstructor {
  new(locales: readonly string[] | string, options: { type: 'region' }): RegionDisplayNames;
}

export function resolveLocalizedCountryName(countryCode: string | null | undefined, currentLanguage: string): string | null {
  const normalizedCode: string = countryCode?.trim().toUpperCase() ?? '';
  if (!normalizedCode) {
    return null;
  }

  const intlWithDisplayNames: { DisplayNames?: RegionDisplayNamesConstructor } = Intl as unknown as { DisplayNames?: RegionDisplayNamesConstructor };
  if (!intlWithDisplayNames.DisplayNames) {
    return normalizedCode;
  }

  try {
    const displayNames: RegionDisplayNames = new intlWithDisplayNames.DisplayNames([currentLanguage || 'en'], { type: 'region' });
    return displayNames.of(normalizedCode) ?? normalizedCode;
  } catch {
    return normalizedCode;
  }
}
