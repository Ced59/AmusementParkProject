import { Injectable } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

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

      if (url.protocol !== 'https:' || !this.isAllowedEmbedUrl(url)) {
        return null;
      }

      return this.domSanitizer.bypassSecurityTrustResourceUrl(url.href);
    } catch {
      return null;
    }
  }

  private isAllowedEmbedUrl(url: URL): boolean {
    const hostname: string = url.hostname.toLowerCase();
    const pathname: string = url.pathname.toLowerCase();

    if ((hostname === 'www.youtube.com' || hostname === 'youtube.com' || hostname === 'www.youtube-nocookie.com') && pathname.startsWith('/embed/')) {
      return true;
    }

    if (hostname === 'www.dailymotion.com' && pathname.startsWith('/embed/video/')) {
      return true;
    }

    return hostname === 'player.vimeo.com' && pathname.startsWith('/video/');
  }
}
