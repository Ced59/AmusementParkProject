import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';

import { ImagesApiService } from '@data-access/images/images-api.service';

export interface LcpImagePreloadRequest {
  readonly imageId: string | null | undefined;
  readonly fallbackWidth: number;
  readonly responsiveWidths: readonly number[];
  readonly sizes: string;
}

@Injectable({
  providedIn: 'root'
})
export class LcpImagePreloadService {
  private static readonly preloadSelector = 'link[data-app-lcp-image-preload="true"]';

  private currentPreloadKey: string | null = null;

  constructor(
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly imagesApiService: ImagesApiService
  ) {
  }

  preloadImage(request: LcpImagePreloadRequest): void {
    const imageId: string = request.imageId?.trim() ?? '';

    if (imageId.length === 0) {
      this.clearPreload();
      return;
    }

    const imageUrl: string | null = this.imagesApiService.resolveImageUrl(imageId, { width: request.fallbackWidth });

    if (!imageUrl) {
      this.clearPreload();
      return;
    }

    const imageSrcSet: string | null = this.imagesApiService.buildImageSrcSet(imageId, request.responsiveWidths);
    const preloadKey: string = `${imageUrl}|${imageSrcSet ?? ''}|${request.sizes}`;

    if (this.currentPreloadKey === preloadKey && this.findExistingPreload()) {
      return;
    }

    this.clearPreload();

    const linkElement: HTMLLinkElement = this.document.createElement('link');
    linkElement.setAttribute('rel', 'preload');
    linkElement.setAttribute('as', 'image');
    linkElement.setAttribute('href', imageUrl);
    linkElement.setAttribute('fetchpriority', 'high');
    linkElement.setAttribute('data-app-lcp-image-preload', 'true');

    if (imageSrcSet) {
      linkElement.setAttribute('imagesrcset', imageSrcSet);
      linkElement.setAttribute('imagesizes', request.sizes);
    }

    this.document.head.appendChild(linkElement);
    this.currentPreloadKey = preloadKey;
  }

  clearPreload(): void {
    const existingLinks: HTMLLinkElement[] = Array.from(
      this.document.head.querySelectorAll<HTMLLinkElement>(LcpImagePreloadService.preloadSelector)
    );

    existingLinks.forEach((linkElement: HTMLLinkElement): void => {
      linkElement.remove();
    });

    this.currentPreloadKey = null;
  }

  private findExistingPreload(): HTMLLinkElement | null {
    return this.document.head.querySelector<HTMLLinkElement>(LcpImagePreloadService.preloadSelector);
  }
}
