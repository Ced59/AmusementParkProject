import { Injectable } from '@angular/core';
import { CountryDto } from '@app/models/countries/country-dto';

interface RegionDisplayNames {
  of(code: string): string | undefined;
}

interface RegionDisplayNamesConstructor {
  new(locales: readonly string[] | string, options: { type: 'region' }): RegionDisplayNames;
}

@Injectable({ providedIn: 'root' })
export class CountryDisplayService {
  private readonly displayNamesCache: Map<string, RegionDisplayNames | null> = new Map<string, RegionDisplayNames | null>();

  resolveLocalizedCountryName(countryCode: string | null | undefined, currentLanguage: string): string | null {
    const normalizedCode: string | null = this.normalizeCountryCode(countryCode);
    if (!normalizedCode) {
      return null;
    }

    const displayNames: RegionDisplayNames | null = this.getDisplayNames(currentLanguage);
    if (!displayNames) {
      return normalizedCode;
    }

    try {
      return displayNames.of(normalizedCode) ?? normalizedCode;
    }
    catch {
      return normalizedCode;
    }
  }

  resolveCountryCodeFromLocalizedName(value: string | null | undefined, countries: readonly CountryDto[]): string | null {
    const normalizedValue: string = this.normalizeSearchValue(value);
    if (!normalizedValue) {
      return null;
    }

    const directCode: string | null = this.normalizeCountryCode(normalizedValue);
    if (directCode && countries.some((country: CountryDto) => this.normalizeCountryCode(country.isoCode) === directCode)) {
      return directCode;
    }

    const matchingCountry: CountryDto | undefined = countries.find((country: CountryDto) => {
      const normalizedName: string = this.normalizeSearchValue(country.name);
      return normalizedName === normalizedValue;
    });

    return this.normalizeCountryCode(matchingCountry?.isoCode);
  }

  countryNameMatches(value: string | null | undefined, searchTerm: string | null | undefined): boolean {
    const normalizedValue: string = this.normalizeSearchValue(value);
    const normalizedSearchTerm: string = this.normalizeSearchValue(searchTerm);

    return normalizedValue.length > 0
      && normalizedSearchTerm.length > 0
      && normalizedValue.includes(normalizedSearchTerm);
  }

  private getDisplayNames(currentLanguage: string): RegionDisplayNames | null {
    const normalizedLanguage: string = (currentLanguage || 'en').trim().toLowerCase() || 'en';
    if (this.displayNamesCache.has(normalizedLanguage)) {
      return this.displayNamesCache.get(normalizedLanguage) ?? null;
    }

    const intlWithDisplayNames: { DisplayNames?: RegionDisplayNamesConstructor } = Intl as unknown as { DisplayNames?: RegionDisplayNamesConstructor };
    if (!intlWithDisplayNames.DisplayNames) {
      this.displayNamesCache.set(normalizedLanguage, null);
      return null;
    }

    try {
      const displayNames: RegionDisplayNames = new intlWithDisplayNames.DisplayNames([normalizedLanguage], { type: 'region' });
      this.displayNamesCache.set(normalizedLanguage, displayNames);
      return displayNames;
    }
    catch {
      this.displayNamesCache.set(normalizedLanguage, null);
      return null;
    }
  }

  private normalizeCountryCode(value: string | null | undefined): string | null {
    const normalizedValue: string = (value ?? '').trim().toUpperCase();
    return normalizedValue.length === 2 ? normalizedValue : null;
  }

  private normalizeSearchValue(value: string | null | undefined): string {
    return (value ?? '')
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }
}
