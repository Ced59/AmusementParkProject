import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import type { MockedObject } from 'vitest';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { HtmlSecurityService } from '@shared/utils/security/html-security.service';
import { SafeCommentRichHtmlPipe } from './safe-comment-rich-html.pipe';

class DomSanitizerStub {
  bypassSecurityTrustHtml(value: string): SafeHtml {
    return value as unknown as SafeHtml;
  }
}

describe('SafeCommentRichHtmlPipe', () => {
  it('renders managed ids through the responsive image pipeline', () => {
    const htmlSecurityService: MockedObject<HtmlSecurityService> = {
      sanitizeManagedImageRichHtml: vi.fn().mockReturnValue(
        '<p>Text</p><img src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--left" alt="Park">'
      ),
      extractManagedImageId: vi.fn().mockReturnValue('0123456789abcdef0123456789abcdef')
    } as unknown as MockedObject<HtmlSecurityService>;
    const imagesApiService: MockedObject<ImagesApiService> = {
      buildImageUrl: vi.fn().mockReturnValue('/pipeline/images/image-1?width=1280'),
      buildImageSrcSet: vi.fn().mockReturnValue(
        '/pipeline/images/image-1?width=320 320w, /pipeline/images/image-1?width=1280 1280w'
      )
    } as unknown as MockedObject<ImagesApiService>;
    const pipe: SafeCommentRichHtmlPipe = new SafeCommentRichHtmlPipe(
      document,
      htmlSecurityService,
      imagesApiService,
      new DomSanitizerStub() as unknown as DomSanitizer
    );

    const html: string = pipe.transform('<img>') as unknown as string;

    expect(html).toContain('src="/pipeline/images/image-1?width=1280"');
    expect(html).toContain('srcset="/pipeline/images/image-1?width=320 320w, /pipeline/images/image-1?width=1280 1280w"');
    expect(html).toContain('sizes="(max-width: 760px)');
    expect(html).toContain('loading="lazy"');
    expect(html).toContain('decoding="async"');
  });

  it('returns empty server-renderable html without accessing browser globals', () => {
    const workingDocument: Document = document.implementation.createHTMLDocument('server');
    const htmlSecurityService: MockedObject<HtmlSecurityService> = {
      sanitizeManagedImageRichHtml: vi.fn().mockReturnValue('')
    } as unknown as MockedObject<HtmlSecurityService>;
    const pipe: SafeCommentRichHtmlPipe = new SafeCommentRichHtmlPipe(
      workingDocument,
      htmlSecurityService,
      {} as ImagesApiService,
      new DomSanitizerStub() as unknown as DomSanitizer
    );

    expect(pipe.transform(null) as unknown as string).toBe('');
  });
});
