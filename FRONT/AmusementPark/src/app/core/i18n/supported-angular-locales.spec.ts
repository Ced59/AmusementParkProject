import { formatDate } from '@angular/common';

import { LANGUAGES } from '@shared/models/localization';
import { registerSupportedAngularLocales } from './supported-angular-locales';

describe('registerSupportedAngularLocales', () => {
  it('registers every supported language under its route and regional aliases', () => {
    registerSupportedAngularLocales();

    for (const language of LANGUAGES) {
      expect(() => formatDate('2026-07-01T10:00:00Z', 'longDate', language.value))
        .not.toThrow();
      expect(() => formatDate('2026-07-01T10:00:00Z', 'longDate', language.code))
        .not.toThrow();
    }
  });
});
