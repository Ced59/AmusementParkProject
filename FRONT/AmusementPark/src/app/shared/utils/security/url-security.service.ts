import { Injectable } from '@angular/core';

const SAFE_DATA_IMAGE_PATTERN: RegExp = /^data:image\/(png|jpeg|jpg|gif|webp);base64,[a-z0-9+/=\s]+$/i;

@Injectable({
  providedIn: 'root'
})
export class UrlSecurityService {
  sanitizeExternalUrl(value: string | null | undefined): string | null {
    const rawValue: string = value?.trim() ?? '';
    if (!rawValue) {
      return null;
    }

    const normalizedValue: string = this.normalizeUrlWithProtocol(rawValue);
    if (!/^[a-z][a-z0-9+.-]*:/i.test(normalizedValue) && !normalizedValue.startsWith('//')) {
      return null;
    }

    const urlValue: string = normalizedValue.startsWith('//') ? `https:${normalizedValue}` : normalizedValue;
    const parsedUrl: URL | null = this.tryParseUrl(urlValue);
    if (parsedUrl === null) {
      return null;
    }

    const protocol: string = parsedUrl.protocol.toLowerCase();
    if (protocol !== 'http:' && protocol !== 'https:' && protocol !== 'mailto:' && protocol !== 'tel:') {
      return null;
    }

    return urlValue;
  }

  sanitizeImageUrl(value: string | null | undefined): string | null {
    const rawValue: string = value?.trim() ?? '';
    if (!rawValue) {
      return null;
    }

    if (rawValue.startsWith('/')) {
      return rawValue;
    }

    if (/^blob:/i.test(rawValue)) {
      return rawValue;
    }

    if (SAFE_DATA_IMAGE_PATTERN.test(rawValue)) {
      return rawValue.replace(/\s+/g, '');
    }

    const parsedUrl: URL | null = this.tryParseUrl(rawValue);
    if (parsedUrl === null) {
      return null;
    }

    const protocol: string = parsedUrl.protocol.toLowerCase();
    if (protocol !== 'http:' && protocol !== 'https:') {
      return null;
    }

    return rawValue;
  }

  sanitizeRichHtmlUrl(value: string | null | undefined, allowImageDataUrl: boolean = false): string | null {
    const rawValue: string = value?.trim() ?? '';
    if (!rawValue) {
      return null;
    }

    if (rawValue.startsWith('#') || rawValue.startsWith('/') || rawValue.startsWith('./') || rawValue.startsWith('../')) {
      return rawValue;
    }

    if (allowImageDataUrl && SAFE_DATA_IMAGE_PATTERN.test(rawValue)) {
      return rawValue.replace(/\s+/g, '');
    }

    return this.sanitizeExternalUrl(rawValue);
  }

  private normalizeUrlWithProtocol(value: string): string {
    if (/^[a-z][a-z0-9+.-]*:/i.test(value) || value.startsWith('//')) {
      return value;
    }

    if (/^[\w.-]+\.[a-z]{2,}(\/.*)?$/i.test(value)) {
      return `https://${value}`;
    }

    return value;
  }

  private tryParseUrl(value: string): URL | null {
    try {
      return new URL(value, 'https://amusementpark.local');
    } catch {
      return null;
    }
  }
}
