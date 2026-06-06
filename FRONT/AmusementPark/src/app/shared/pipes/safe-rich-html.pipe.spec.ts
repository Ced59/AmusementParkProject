import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { HtmlSecurityService } from '@shared/utils/security/html-security.service';
import { SafeRichHtmlPipe } from './safe-rich-html.pipe';

class DomSanitizerStub {
  public bypassSecurityTrustHtml(value: string): SafeHtml {
    return `SAFE:${value}` as unknown as SafeHtml;
  }
}

describe('SafeRichHtmlPipe', () => {
  it('sanitizes html before marking it as trusted', () => {
    const htmlSecurityService: jasmine.SpyObj<HtmlSecurityService> = jasmine.createSpyObj<HtmlSecurityService>('HtmlSecurityService', ['sanitizeRichHtml']);
    const domSanitizer: DomSanitizerStub = new DomSanitizerStub();
    const pipe: SafeRichHtmlPipe = new SafeRichHtmlPipe(htmlSecurityService, domSanitizer as unknown as DomSanitizer);
    htmlSecurityService.sanitizeRichHtml.and.returnValue('<p>safe</p>');

    const result: SafeHtml = pipe.transform('<script>bad</script><p>safe</p>');

    expect(htmlSecurityService.sanitizeRichHtml).toHaveBeenCalledWith('<script>bad</script><p>safe</p>');
    expect(result as unknown as string).toBe('SAFE:<p>safe</p>');
  });
});
