import { Injectable } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { isAllowedVideoEmbedUrl } from '@core/security/video-embed-policy';

@Injectable({
  providedIn: 'root'
})
export class SafeVideoEmbedUrlService {
  constructor(private readonly domSanitizer: DomSanitizer) {
  }

  resolve(value: string | null | undefined): SafeResourceUrl | null {
    const normalizedValue: string = value?.trim() ?? '';

    if (!normalizedValue) {
      return null;
    }

    try {
      const url: URL = new URL(normalizedValue);

      if (url.protocol !== 'https:' || !isAllowedVideoEmbedUrl(url)) {
        return null;
      }

      return this.domSanitizer.bypassSecurityTrustResourceUrl(url.href);
    } catch {
      return null;
    }
  }

}
