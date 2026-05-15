import { Pipe, PipeTransform } from '@angular/core';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { resolveLocalizedText } from '@shared/utils/localization';

@Pipe({
  name: 'localizedText'
})
export class LocalizedTextPipe implements PipeTransform {
  transform(
    items: readonly LocalizedItem<string>[] | null | undefined,
    currentLanguage: string | null | undefined,
    fallback: string = '—'
  ): string {
    return resolveLocalizedText(items, currentLanguage, fallback);
  }
}
