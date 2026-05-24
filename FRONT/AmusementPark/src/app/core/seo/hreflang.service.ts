import { Injectable } from '@angular/core';

import { CanonicalUrlService } from './canonical-url.service';
import { SEO_DEFAULT_LANGUAGE, SEO_LANGUAGES, SeoLanguageDefinition } from './seo-languages';
import { SeoAlternateLink } from './models/seo-route-data.model';

@Injectable({
  providedIn: 'root'
})
export class HreflangService {
  constructor(private readonly canonicalUrlService: CanonicalUrlService) {
  }

  buildAlternates(currentUrl: string): SeoAlternateLink[] {
    const alternates: SeoAlternateLink[] = SEO_LANGUAGES.map((language: SeoLanguageDefinition): SeoAlternateLink => ({
      hreflang: language.hreflang,
      href: this.canonicalUrlService.buildAbsoluteUrl(this.canonicalUrlService.replaceLanguage(currentUrl, language.language))
    }));

    alternates.push({
      hreflang: 'x-default',
      href: this.canonicalUrlService.buildAbsoluteUrl(this.canonicalUrlService.replaceLanguage(currentUrl, SEO_DEFAULT_LANGUAGE))
    });

    return alternates;
  }
}
