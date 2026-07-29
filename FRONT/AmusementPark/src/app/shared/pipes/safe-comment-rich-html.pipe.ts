import { DOCUMENT } from '@angular/common';
import { Inject, Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { HtmlSecurityService } from '@shared/utils/security/html-security.service';

const RESPONSIVE_IMAGE_WIDTHS: readonly number[] = [320, 480, 640, 960, 1280];

@Pipe({
  name: 'safeCommentRichHtml',
  standalone: true
})
export class SafeCommentRichHtmlPipe implements PipeTransform {
  constructor(
    @Inject(DOCUMENT) private readonly documentRef: Document,
    private readonly htmlSecurityService: HtmlSecurityService,
    private readonly imagesApiService: ImagesApiService,
    private readonly domSanitizer: DomSanitizer
  ) {
  }

  transform(value: string | null | undefined): SafeHtml {
    const sanitizedHtml: string = this.htmlSecurityService.sanitizeManagedImageRichHtml(value);
    if (!sanitizedHtml) {
      return this.domSanitizer.bypassSecurityTrustHtml('');
    }

    const workingDocument: Document = this.documentRef.implementation?.createHTMLDocument(
      'comment-rich-html-renderer'
    ) ?? this.documentRef;
    const template: HTMLTemplateElement = workingDocument.createElement('template');
    template.innerHTML = sanitizedHtml;

    const images: HTMLImageElement[] = Array.from(template.content.querySelectorAll('img'));
    for (const image of images) {
      const imageId: string | null = this.htmlSecurityService.extractManagedImageId(
        image.getAttribute('src')
      );
      if (imageId === null) {
        image.remove();
        continue;
      }

      image.setAttribute('src', this.imagesApiService.buildImageUrl(imageId, { width: 1280 }));
      const srcSet: string | null = this.imagesApiService.buildImageSrcSet(
        imageId,
        RESPONSIVE_IMAGE_WIDTHS
      );
      if (srcSet) {
        image.setAttribute('srcset', srcSet);
        image.setAttribute('sizes', this.resolveSizes(image));
      }
      image.setAttribute('loading', 'lazy');
      image.setAttribute('decoding', 'async');
    }

    return this.domSanitizer.bypassSecurityTrustHtml(template.innerHTML);
  }

  private resolveSizes(image: HTMLImageElement): string {
    if (image.classList.contains('rich-text__image--left')
      || image.classList.contains('rich-text__image--right')) {
      return '(max-width: 760px) calc(100vw - 2rem), (max-width: 1160px) 44vw, 512px';
    }

    if (image.classList.contains('rich-text__image--center')) {
      return '(max-width: 760px) calc(100vw - 2rem), (max-width: 1200px) 72vw, 864px';
    }

    return '(max-width: 760px) calc(100vw - 2rem), (max-width: 1180px) calc(100vw - 4rem), 1116px';
  }
}
