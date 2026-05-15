import { Pipe, PipeTransform } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { HtmlSecurityService } from '@shared/utils/security/html-security.service';

@Pipe({
  name: 'safeRichHtml',
  standalone: true
})
export class SafeRichHtmlPipe implements PipeTransform {
  constructor(
    private readonly htmlSecurityService: HtmlSecurityService,
    private readonly domSanitizer: DomSanitizer
  ) {
  }

  transform(value: string | null | undefined): SafeHtml {
    const sanitizedHtml: string = this.htmlSecurityService.sanitizeRichHtml(value);
    return this.domSanitizer.bypassSecurityTrustHtml(sanitizedHtml);
  }
}
