import { Pipe, PipeTransform } from '@angular/core';
import { LocalizedItem } from '../models/shared/localized-item';

@Pipe({
  name: 'localizedText'
})
export class LocalizedTextPipe implements PipeTransform {
  transform(
    items: LocalizedItem<string>[] | null | undefined,
    currentLanguage: string | null | undefined,
    fallback: string = '—'
  ): string {
    if (!items || items.length === 0) {
      return fallback;
    }

    const normalizedLanguage: string = (currentLanguage ?? '').trim().toLowerCase();

    const exactMatch: LocalizedItem<string> | undefined = items.find(
      (item: LocalizedItem<string>) => item.languageCode?.trim().toLowerCase() === normalizedLanguage
    );

    if (exactMatch?.value?.trim()) {
      return exactMatch.value;
    }

    const englishMatch: LocalizedItem<string> | undefined = items.find(
      (item: LocalizedItem<string>) => item.languageCode?.trim().toLowerCase() === 'en'
    );

    if (englishMatch?.value?.trim()) {
      return englishMatch.value;
    }

    const firstNonEmpty: LocalizedItem<string> | undefined = items.find(
      (item: LocalizedItem<string>) => !!item.value?.trim()
    );

    return firstNonEmpty?.value ?? fallback;
  }
}
