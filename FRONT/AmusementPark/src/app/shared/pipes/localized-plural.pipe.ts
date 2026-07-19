import { Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

import { resolveLocalizedPlural } from '@shared/utils/localization/localized-plural.helpers';

@Pipe({
  name: 'localizedPlural',
  pure: false
})
export class LocalizedPluralPipe implements PipeTransform {
  constructor(private readonly translateService: TranslateService) {
  }

  transform(translationKey: string, count: number, params: Record<string, unknown> = {}, locale?: string): string {
    const resolvedLocale: string = locale?.trim() || this.translateService.currentLang || this.translateService.defaultLang || 'en';
    return resolveLocalizedPlural(this.translateService, translationKey, count, resolvedLocale, params);
  }
}
